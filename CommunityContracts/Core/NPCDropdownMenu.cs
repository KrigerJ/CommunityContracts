using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using SDV_NPC = StardewValley.NPC;
using static ModEntry;

namespace CommunityContracts.Core
{
    public class NPCDropdownMenu : IClickableMenu
    {
        private List<string> npcNames = new List<string> { "Abigail", "Alex", "Caroline", "Demetrius", "Elliott", "Emily", "Evelyn", "George", "Haley", "Jas", "Jodi", "Leah", "Leo", "Linus", "Maru", "Pam", "Penny", "Sam", "Sandy", "Sebastian", "Shane", "Vincent", "Wizard" };
        private List<NPCMenuOption> options = new List<NPCMenuOption>();
        private Dictionary<string, Texture2D> npcPortraits = new();
        private int selectedIndex = -1;
        private const int ButtonWidth = 160;
        private const int ButtonHeight = 70;
        private const int Columns = 4;
        private const int HSpacing = 10;
        private const int WSpacing = 120;
        private static string DropLocationTooltip => T("DropLocationTooltip");
        private ClickableComponent setDropLocationButton;
        public class NPCMenuOption
        {
            public string name;
            public ClickableComponent nameButton;
            public Rectangle portraitRect;
        }
        public NPCDropdownMenu()
        {
            int startX = Game1.viewport.Width / 2 - ((ButtonWidth + WSpacing) * Columns / 2);
            int startY = Game1.viewport.Height / 2 - 210;


            foreach (var name in npcNames)
            {
                try
                {
                    npcPortraits[name] = Game1.content.Load<Texture2D>($"Portraits/{name}");
                }
                catch
                {
                    // Monitor.Log($"Portrait not found for {name}", LogLevel.Trace);
                }
            }

            for (int i = 0; i < npcNames.Count; i++)
            {
                string name = npcNames[i];
                SDV_NPC npc = Game1.getCharacterFromName(name, mustBeVillager: false);

                if (npc == null || npc.currentLocation == null || npc.Position == Vector2.Zero)
                {
                    Instance.Monitor.Log(T("SkippingNPC", new { npc = name }), LogLevel.Trace);
                    continue;
                }

                int col = options.Count % Columns;
                int row = options.Count / Columns;

                int x = startX + col * (ButtonWidth + WSpacing);
                int y = startY + row * (ButtonHeight + HSpacing);

                var nameButton = new ClickableComponent(new Rectangle(x, y, ButtonWidth, ButtonHeight), name);
                var portraitRect = new Rectangle(x - 70, y, 64, 64); // Same as your draw logic

                options.Add(new NPCMenuOption
                {
                    name = name,
                    nameButton = nameButton,
                    portraitRect = portraitRect
                });
            }

            int frameX = Game1.viewport.Width / 2 - ((ButtonWidth + WSpacing) * Columns / 2) - 72;
            int frameY = Game1.viewport.Height / 2 - 320;

            // Position Drop Location button
            int buttonX = frameX + 346; // adjust as needed for spacing
            int buttonY = frameY + 20;

            setDropLocationButton = new ClickableComponent(
                new Rectangle(buttonX, buttonY, 440, 60),
                "SetDropLocation"
            );
        }
        public override void draw(SpriteBatch b)
        {
            int framePadding = 20;
            int frameWidth = (ButtonWidth + WSpacing) * Columns + framePadding * 6 - 80;
            int frameHeight = ((npcNames.Count + Columns) / Columns) * (ButtonHeight + HSpacing * 2) + framePadding * 2 + 40;
            int frameX = Game1.viewport.Width / 2 - frameWidth / 2 - 72;
            int frameY = Game1.viewport.Height / 2 - 320;

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                frameX,
                frameY,
                frameWidth,
                frameHeight,
                Color.White,
                drawShadow: false
            );

            // Draw the "Set Drop Location" button
            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                setDropLocationButton.bounds.X,
                setDropLocationButton.bounds.Y,
                setDropLocationButton.bounds.Width,
                setDropLocationButton.bounds.Height,
                Color.White,
                1f,
                false
            );

            // Measure the text size
            string text = T("SetDropLocation");
            Vector2 textSize = Game1.smallFont.MeasureString(text);

            // Compute centered position
            float textX = setDropLocationButton.bounds.X + (setDropLocationButton.bounds.Width / 2f) - (textSize.X / 2f);
            float textY = setDropLocationButton.bounds.Y + (setDropLocationButton.bounds.Height / 2f) - (textSize.Y / 2f);

            // Draw centered text
            Utility.drawTextWithShadow(
                b,
                text,
                Game1.smallFont,
                new Vector2(textX, textY),
                Game1.textColor
            );

            base.draw(b);

            foreach (var option in options)
            {
                Color boxColor = option.nameButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ? Color.Gold : Color.White;

                drawTextureBox(
                    b,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    option.nameButton.bounds.X,
                    option.nameButton.bounds.Y,
                    option.nameButton.bounds.Width,
                    option.nameButton.bounds.Height,
                    boxColor,
                    1f,
                    false
                );

                if (npcPortraits.TryGetValue(option.name, out var portrait))
                {
                    Rectangle sourceRect = new Rectangle(0, 0, 64, 64);
                    b.Draw(portrait, option.portraitRect, sourceRect, Color.White);
                }

                // Measure text size
                Vector2 NametextSize = Game1.smallFont.MeasureString(option.name);

                // Compute centered position
                float NametextX = option.nameButton.bounds.X + (option.nameButton.bounds.Width / 2f) - (NametextSize.X / 2f);
                float NametextY = option.nameButton.bounds.Y + (option.nameButton.bounds.Height / 2f) - (NametextSize.Y / 2f);

                // Draw centered text
                Utility.drawTextWithShadow(
                    b,
                    option.name,
                    Game1.smallFont,
                    new Vector2(NametextX, NametextY),
                    Game1.textColor
                );
            }

            drawMouse(b);

            foreach (var option in options)
            {
                if (option.portraitRect.Contains(Game1.getMouseX(), Game1.getMouseY()))
                {
                    string tooltip = T("WarpToLocation", new { npc = option.name });

                    drawHoverText(
                        b,
                        tooltip,
                        Game1.smallFont
                    );
                    break;
                }
            }

            foreach (var option in options)
            {
                if (option.nameButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    string tooltip = T("GoToMenu", new { npc = option.name });

                    drawHoverText(
                        b,
                        tooltip,
                        Game1.smallFont,
                        xOffset: -300
                    );
                    break;
                }
            }

            if (setDropLocationButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    DropLocationTooltip,
                    Game1.smallFont,
                    xOffset: -300 // shift left
                );
            }   
        }
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            foreach (var option in options)
            {
                string npcName = option.name;
                SDV_NPC npc = Game1.getCharacterFromName(npcName, mustBeVillager: false);

                // Check if the portrait was clicked
                if (option.portraitRect.Contains(x, y))
                {
                    Vector2 tile = npc.Position / Game1.tileSize;
                    Game1.warpFarmer(npc.currentLocation.Name, (int)tile.X, (int)tile.Y, false);
                    Game1.exitActiveMenu();
                    Game1.playSound("wand");
                    return; // Skip contract logic
                }

                // Check if the name button was clicked
                if (option.nameButton.containsPoint(x, y))
                {
                    Game1.exitActiveMenu();
                    Game1.activeClickableMenu = new NPCServiceMenu(npcName);
                    return;
                }

                if (setDropLocationButton.containsPoint(x, y))
                {
                    Vector2 tile = new Vector2((int)(Game1.player.Position.X / Game1.tileSize), (int)(Game1.player.Position.Y / Game1.tileSize));
                    string locationName = Game1.player.currentLocation.Name;

                    Game1.exitActiveMenu();

                    Config.DropLocationName = locationName;
                    Config.PresetLocations[locationName] = tile;
                    Instance.Helper.WriteConfig(Config);

                    Game1.playSound("coin");
                    Game1.showGlobalMessage(T("DropLocationSet", new { location = locationName, x = (int)tile.X, y = (int)tile.Y }));

                    HighlightedDropTile = new Vector2(
                        (int)(Game1.player.Position.X / Game1.tileSize),
                        (int)(Game1.player.Position.Y / Game1.tileSize)
                    );

                    return;
                } 
            }

            base.receiveLeftClick(x, y, playSound);
        }
    }
}
