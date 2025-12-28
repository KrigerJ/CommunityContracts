using CommunityContracts.Core;
using CommunityContracts.Core.NPC;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using SObject = StardewValley.Object;
using static CommunityContracts.Core.NPCServiceMenu;

public class ModEntry : Mod
{
    public static ModEntry Instance { get; private set; }

    public static bool IsSelectingTile = false;

    internal static ModConfig Config;
    public static IMonitor ModMonitor { get; private set; }

    public static Dictionary<string, bool> IsNPCCollecting = new();

    public static Vector2? HighlightedDropTile;

    public static Dictionary<string, int> npcCooldowns = new();
    public static string T(string key, object tokens = null)
    {
        var translation = Instance.Helper.Translation.Get(key);
        if (tokens != null)
            translation = translation.Tokens(tokens);
        return translation.ToString();
    }
    public static Dictionary<string, object> NPCProfiles = new()
    {
        { "Abigail", new AbigailProfile() },
        { "Alex", new AlexProfile() },
        { "Caroline", new CarolineProfile() },
        { "Demetrius", new DemetriusProfile() },
        { "Elliott", new ElliottProfile() },
        { "Emily", new EmilyProfile() },
        { "Evelyn", new EvelynProfile() },
        { "George", new GeorgeProfile() },
        { "Haley", new HaleyProfile() },
        { "Jas", new JasProfile() },
        { "Jodi", new JodiProfile() },
        { "Leah", new LeahProfile() },
        { "Leo", new LeoProfile() },
        { "Linus", new LinusProfile() },
        { "Maru", new MaruProfile() },
        { "Pam", new PamProfile() },
        { "Penny", new PennyProfile() },
        { "Sam", new SamProfile() },
        { "Sandy", new SandyProfile() },
        { "Sebastian", new SebastianProfile() },
        { "Shane", new ShaneProfile() },
        { "Vincent", new VincentProfile() },
        { "Wizard", new WizardProfile() },
    };
    public class ContractDelivery
    {
        public List<Item> Items { get; set; }
        public string Source { get; set; }
        public long RecipientID { get; set; } // New field
    }
    private readonly List<ContractDelivery> ContractsDeliveries = new();
    public void QueueContractsDelivery(ContractDelivery delivery)
    {
        if (delivery.Items.Count > 0)
        {
            ContractsDeliveries.Add(delivery);
            Monitor.Log(T("QueuedDelivery", new { source = delivery.Source }), LogLevel.Info);
        }
    }
    private void OnRenderedHud(object sender, RenderedHudEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu != null || !Config.EnableContractTooltip)
            return;

        Vector2 cursorPos = new Vector2(Game1.getMouseX(), Game1.getMouseY());

        foreach (NPC npc in Game1.currentLocation.characters)
        {
            if (IsNPCCollecting.TryGetValue(npc.Name, out bool isBusy) && isBusy)
                continue;

            Rectangle npcBox = new Rectangle(
                (int)(npc.Position.X - Game1.viewport.X),
                (int)(npc.Position.Y - Game1.viewport.Y - 64),
                npc.Sprite.SpriteWidth * 4,
                npc.Sprite.SpriteHeight * 4 + 64
            );

            if (npcBox.Contains((int)cursorPos.X, (int)cursorPos.Y) && CanOpenMenuFor(npc))
            {
                string specialty = Specialties.GetValueOrDefault(npc.Name) ?? "General";
                string tooltip = T("ContractTooltip", new { npc = npc.Name });

                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    tooltip,
                    Game1.smallFont
                );
                break;
            }
        }
    }
    private bool CanOpenMenuFor(NPC npc)
    {
        if (npc == null || npc is Pet || npc is Monster || npc is Horse || npc is Child)
            return false;

        return !npc.IsInvisible &&
               !npc.isMoving() &&
               (!npcCooldowns.TryGetValue(npc.Name, out int ticks) || ticks == 0);
    }
    private void OnRenderingHud(object sender, RenderingHudEventArgs e)
    {
        if (Config.EnableProcessTimeReduction)
        {
            // Tile under the cursor (Vector2), matches Objects dictionary keys
            Vector2 cursorTile = Game1.currentCursorTile;

            if (Game1.currentLocation != null &&
                Game1.currentLocation.Objects.TryGetValue(cursorTile, out SObject obj) &&
                obj is not null &&
                obj.bigCraftable.Value &&
                obj.minutesUntilReady.Value > 0)
            {
                int minutesLeft = obj.minutesUntilReady.Value;

                // Convert minutes to hours:minutes
                int hours = minutesLeft / 60;
                int minutes = minutesLeft % 60;

                string tooltip = T("DisplayTime", new { DisName = obj.DisplayName, Hrs = hours, Min = minutes });

                IClickableMenu.drawHoverText(
                    e.SpriteBatch,
                    tooltip,
                    Game1.smallFont
                );
            }
        }
    }
    public override void Entry(IModHelper helper)
    {
        Instance = this;

        ModMonitor = this.Monitor;

        Config = helper.ReadConfig<ModConfig>();

        Helper.Events.Display.RenderingHud += OnRenderingHud;

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;

        helper.Events.GameLoop.DayStarted += OnDayStarted;

        helper.Events.Input.ButtonPressed += OnButtonPressed;

        helper.Events.Display.MenuChanged += OnMenuChanged;

        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        helper.Events.Display.RenderedHud += OnRenderedHud;

        helper.Events.GameLoop.TimeChanged += OnTimeChanged;

        Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

        Helper.Events.Display.RenderedWorld += OnRenderedWorld;

        helper.Events.GameLoop.SaveLoaded += (_, _) =>
        {
            foreach (var location in Game1.locations)
            {
                foreach (var pair in location.objects.Pairs)
                {
                    if (pair.Value is Chest chest &&
                        chest.modData.TryGetValue("CommunityContracts/DeliveryColor", out var savedColor) &&
                        Config.ChestColors.TryGetValue(savedColor, out var tint))
                    {
                        chest.playerChoiceColor.Value = tint;
                    }
                }
            }
        };

        var QualityHarmony = new Harmony(this.ModManifest.UniqueID);

        Helper.Events.GameLoop.Saving += OnSaving;

        var harmony = new Harmony(ModManifest.UniqueID);

        Helper.Events.GameLoop.DayEnding += (s, e) =>
        {
            var convertedDeliveries = ContractsDeliveries
                .Select(w => new ContractUtilities.ContractsDelivery
                {
                    Items = w.Items,
                    RecipientID = w.RecipientID
                })
                .ToList();

            ContractUtilities.DeliverContractsItems(convertedDeliveries, Config);
            ContractsDeliveries.Clear(); // Clear original list after delivery
        };
    }
    private void OnGameLaunched(object sender, EventArgs e)
    {
        GMCMContent.Register(Helper, ModManifest, Monitor, Config);
    }
    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
        if(Config.EnableProcessTimeReduction)
        { 
        // Calculate progression factor
        int friendshipPoints = Game1.player.friendshipData.Values.Sum(f => f.Points);
        int totalSkillLevels = Game1.player.FarmingLevel + Game1.player.ForagingLevel +
                               Game1.player.FishingLevel + Game1.player.MiningLevel +
                               Game1.player.CombatLevel;

        const int MaxFriendship = 85000; // 34 NPCs * 2500
        const int MaxSkills = 50;        // 5 skills * 10 levels
        const int MaxTime = 8640;        // longest vanilla process
        const int MinTime = 10;          // floor in minutes

        float pFriend = Math.Clamp((float)friendshipPoints / MaxFriendship, 0f, 1f);
        float pSkill = Math.Clamp((float)totalSkillLevels / MaxSkills, 0f, 1f);
        float pRaw = (pFriend + pSkill) * 0.5f;

        // Apply S-curve for faster early progression, taper late
        float pPrime = (float)Math.Pow(pRaw, 0.85f);

        // Compute effective time scaling factor
        int effectiveTime = (int)(MinTime + (MaxTime - MinTime) * (1f - pPrime));
        float scale = (float)effectiveTime / MaxTime;

        Monitor.Log(T("ProductionAccelerator", new { FriendPoints = friendshipPoints, TotSkillLev = totalSkillLevels, Progress = pPrime, Scale = scale }), LogLevel.Info);

        // Apply to all machines globally
        foreach (GameLocation location in Game1.locations)
        {
            foreach (var pair in location.Objects.Pairs)
            {
                if (pair.Value is SObject obj && obj.bigCraftable.Value && obj.minutesUntilReady.Value > 0)
                {
                    int currentTime = obj.minutesUntilReady.Value;
                    int newTime = Math.Max(MinTime, (int)(currentTime * scale));
                    obj.minutesUntilReady.Value = newTime;

                    Monitor.Log($"[Production Accelerator] {obj.Name} at {location.Name} tile {pair.Key}: {currentTime} → {newTime} minutes", LogLevel.Trace);
                }
            }
        }

        Monitor.Log($"Global production speed adjusted. Effective time scale: {scale:F2}", LogLevel.Info);
    }
    }
    private void OnSaving(object sender, SavingEventArgs e)
    {

    }
    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {

    }
    private void OnMenuChanged(object sender, MenuChangedEventArgs e)
    {

    }
    private void OnTimeChanged(object sender, TimeChangedEventArgs e)
    {
        foreach (var key in npcCooldowns.Keys.ToList())
        {
            if (npcCooldowns[key] > 0)
                npcCooldowns[key] -= 10;

            if (npcCooldowns[key] <= 0)
                npcCooldowns.Remove(key); // Clean up expired entries
        }
    }
    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {

    }
    private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
    {
        if (HighlightedDropTile.HasValue &&
            Game1.currentLocation.Name == Config.DropLocationName)
        {
            Vector2 screenPos = Game1.GlobalToLocal(
                Game1.viewport,
                HighlightedDropTile.Value * Game1.tileSize
            );

            // Get highlight color from config
            Color highlightColor = Config.HighlightColors.TryGetValue(
                Config.HighlightColor,
                out var c
            ) ? c : Color.Yellow * 0.75f;

            // Draw highlight
            e.SpriteBatch.Draw(
                Game1.mouseCursors,
                screenPos,
                new Rectangle(194, 388, 16, 16), // yellow tile highlight
                highlightColor,
                0f,
                Vector2.Zero,
                Game1.pixelZoom,
                SpriteEffects.None,
                1f
            );

            // Optional label
            SpriteFont font = Game1.smallFont;
            string[] lines = { T("DeliveryLabel"), T("LocationLabel") };

            Color fontColor = Config.FontColors.TryGetValue(
                Config.FontColor,
                out var colr
            ) ? colr : Color.Black;

            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 lineSize = font.MeasureString(lines[i]);
                Vector2 linePos = screenPos - new Vector2(0, lineSize.Y * (lines.Length - i) + 8);
                e.SpriteBatch.DrawString(font, lines[i], linePos, fontColor);
            }
        }
    }
    private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (Config.CheatMenuHotkey != SButton.None && e.Button == Config.CheatMenuHotkey)
        {
            Game1.activeClickableMenu = new NPCDropdownMenu();
        }

        if (Config.DeliveryLocationHotkey != SButton.None && e.Button == Config.DeliveryLocationHotkey)
        {
            Vector2 tile = new Vector2((int)(Game1.player.Position.X / Game1.tileSize), (int)(Game1.player.Position.Y / Game1.tileSize));
            string CharlocationName = Game1.player.currentLocation.Name;

            Config.DropLocationName = CharlocationName;
            Config.PresetLocations[CharlocationName] = tile;
            Instance.Helper.WriteConfig(Config);

            Game1.playSound("coin");

        Game1.showGlobalMessage(T("DropLocationSet", new { location = CharlocationName, x = (int)tile.X, y = (int)tile.Y }));

        HighlightedDropTile = new Vector2(
                (int)(Game1.player.Position.X / Game1.tileSize),
                (int)(Game1.player.Position.Y / Game1.tileSize)
            );
            return;
        }
        // Left-click to open NPC menu from world
        if (e.Button == SButton.MouseLeft)
        {
            Vector2 cursorPos = new Vector2(Game1.getMouseX(), Game1.getMouseY());

            foreach (NPC npc in Game1.currentLocation.characters)
            {
                if (IsNPCCollecting.TryGetValue(npc.Name, out bool isBusy) && isBusy)
                    continue;

                Rectangle npcBox = new Rectangle(
                    (int)(npc.Position.X - Game1.viewport.X),
                    (int)(npc.Position.Y - Game1.viewport.Y - 64),
                    npc.Sprite.SpriteWidth * 4,
                    npc.Sprite.SpriteHeight * 4 + 64
                );

                if (npcBox.Contains((int)cursorPos.X, (int)cursorPos.Y) && CanOpenMenuFor(npc))
                {
                    if (!npcCooldowns.TryGetValue(npc.Name, out int minutesRemaining))
                    {
                        Game1.playSound("smallSelect");

                        DelayedAction.functionAfterDelay(() =>
                        {
                            try
                            {
                                Monitor.Log(T("OpeningMenu", new { npc = npc.Name }), LogLevel.Info);
                                Game1.activeClickableMenu = new NPCServiceMenu(npc.Name);
                            }
                            catch (Exception ex)
                            {
                                Monitor.Log(T("FailedMenu", new { npc = npc.Name, error = ex }), LogLevel.Error);
                            }
                        }, 150);
                    }
                    else if (minutesRemaining <= 0)
                    {
                        npcCooldowns.Remove(npc.Name); // Optional cleanup
                        Game1.playSound("smallSelect");

                        DelayedAction.functionAfterDelay(() =>
                        {
                            try
                            {
                                Monitor.Log(T("OpeningMenuCooldown", new { npc = npc.Name }), LogLevel.Info);
                                Game1.activeClickableMenu = new NPCServiceMenu(npc.Name);
                            }
                            catch (Exception ex)
                            {
                                Monitor.Log(T("FailedMenuCooldown", new { npc = npc.Name, error = ex }), LogLevel.Error);
                            }
                        }, 150);
                    }
                    else
                    {
                        Monitor.Log(T("CooldownActive", new { npc = npc.Name, minutes = minutesRemaining }), LogLevel.Trace);
                    }

                    break;
                }
            }
        }

        if (!Context.IsWorldReady || !IsSelectingTile || e.Button != SButton.MouseLeft)
            return;

        var cursorTile = e.Cursor.Tile;
        var locationName = Game1.currentLocation.Name;

        // Save to config
        Config.DropLocationName = locationName;
        Config.DropTileX = (int)cursorTile.X;
        Config.DropTileY = (int)cursorTile.Y;
        Helper.WriteConfig(Config);

        Game1.addHUDMessage(new HUDMessage(T("DeliveryLocationSet", new { location = locationName, x = cursorTile.X, y = cursorTile.Y }), HUDMessage.newQuest_type));
        IsSelectingTile = false;
    }
}