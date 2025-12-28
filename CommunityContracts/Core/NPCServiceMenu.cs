using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using static CommunityContracts.Core.ContractUtilities;
using static ModEntry;
using SDV_NPC = StardewValley.NPC;
using SObject = StardewValley.Object;


namespace CommunityContracts.Core
{
    public class NPCServiceMenu : IClickableMenu
    {
        public static Dictionary<string, string> Specialties = new()
        {
            { "Abigail", T("SpecialtyAbigail") },
            { "Alex", T("SpecialtyAlex") },
            { "Caroline", T("SpecialtyCaroline") },
            { "Demetrius", T("SpecialtyDemetrius") },
            { "Elliott", T("SpecialtyElliott") },
            { "Emily", T("SpecialtyEmily") },
            { "Evelyn", T("SpecialtyEvelyn") },
            { "George", T("SpecialtyGeorge") },
            { "Haley", T("SpecialtyHaley") },
            { "Jas", T("SpecialtyJas") },
            { "Jodi", T("SpecialtyJodi") },
            { "Leah", T("SpecialtyLeah") },
            { "Leo", T("SpecialtyLeo") },
            { "Linus", T("SpecialtyLinus") },
            { "Maru", T("SpecialtyMaru") },
            { "Pam", T("SpecialtyPam") },
            { "Penny", T("SpecialtyPenny") },
            { "Sam", T("SpecialtySam") },
            { "Sandy", T("SpecialtySandy") },
            { "Sebastian", T("SpecialtySebastian") },
            { "Shane", T("SpecialtyShane") },
            { "Vincent", T("SpecialtyVincent") },
            { "Wizard", T("SpecialtyWizard") },
            { "General", T("SpecialtyGeneral") }
        };
        public enum ServiceId
        {
            CrabPots,
            SetCrabPots,
            BaitCrabPots,
            Forageables,
            Hardwood,
            Honey,
            Stone,
            Weeds,
            Wood,
            Animals,
            Crops,
            Tappers,
            Producers
        }
        
        private List<(ServiceId id, string label)> GetEnabledServices()
        {
            var services = new List<(ServiceId, string)>
            {

            };

            if (Config.EnableAnimalsService)
                services.Add((ServiceId.Animals, T("ServiceAnimals")));

            if (ButtCount("Pots") > 0)
                services.Add((ServiceId.SetCrabPots, T("ServiceSetCrabPots")));

            if (ButtCount("Bait") > 0 && !Game1.player.professions.Contains(11))
                services.Add((ServiceId.BaitCrabPots, T("ServiceBaitCrabPots")));

            if (ButtCount("Crab") > 0)
                services.Add((ServiceId.CrabPots, T("ServiceCrabPots")));

            if (Config.EnableCropService && ButtCount("Crop") > 0)
                services.Add((ServiceId.Crops, T("ServiceCrops")));

            if (ButtCount("Forge") > 0)
                services.Add((ServiceId.Forageables, T("ServiceForageables")));

            if (ButtCount("Hard") > 0)
                services.Add((ServiceId.Hardwood, T("ServiceHardwood")));

            if (ButtCount("Bee") > 0)
                services.Add((ServiceId.Honey, T("ServiceHoney")));

            if (ButtCount("Stone") > 0)
                services.Add((ServiceId.Stone, T("ServiceStone")));

            if (ButtCount("Tapper") > 0)
                services.Add((ServiceId.Tappers, T("ServiceTappers")));

            if (ButtCount("Weed") > 0)
                services.Add((ServiceId.Weeds, T("ServiceWeeds")));

            if (ButtCount("Wood") > 0)
                services.Add((ServiceId.Wood, T("ServiceWood")));

            if (Config.EnableProducersService)
                services.Add((ServiceId.Producers, T("ServiceProducers")));

            return services;
        }

        public static Dictionary<ServiceId, string> SpecialtyNames = new()
        {
            { ServiceId.CrabPots, T("ServiceCrabPots") },
            { ServiceId.SetCrabPots, T("ServiceSetCrabPots") },
            { ServiceId.BaitCrabPots, T("ServiceBaitCrabPots") },
            { ServiceId.Forageables, T("ServiceForageables") },
            { ServiceId.Hardwood, T("ServiceHardwood") },
            { ServiceId.Honey, T("ServiceHoney") },
            { ServiceId.Stone, T("ServiceStone") },
            { ServiceId.Weeds, T("ServiceWeeds") },
            { ServiceId.Wood, T("ServiceWood") },
            { ServiceId.Animals, T("ServiceAnimals") },
            { ServiceId.Crops, T("ServiceCrops") },
            { ServiceId.Tappers, T("ServiceTappers") },
            { ServiceId.Producers, T("ServiceProducers") },
        };

        private List<NPCMenuOption> options = new List<NPCMenuOption>();
        private Dictionary<string, Texture2D> npcPortraits = new();
        private const int ButtonWidth = 220;
        private const int ButtonHeight = 60;
        private const int Columns = 4;
        private const int HSpacing = 10;
        private const int WSpacing = 20;
        private string Specialty = T("SpecialtyGeneral");
        private string NPCName = "";
        public static string ItemTypeLabel { get; set; }
        private ClickableComponent setSpecialtyContractButton;
        private string SpecialtyContractTooltip = T("SpecialtyContractTooltip");
        private readonly IMonitor Monitor;
        private ClickableComponent npcPortraitButton;
        public static int NPCLevel { get; set; } = 0; // Default level
        public static int Quality { get; set; } = 0; // Normal = 0, Silver = 1, Gold = 2, Iridium = 4
        public static int CurrentFriendship { get; set; } = 0; // Default level
        public static string QualityName { get; set; }
        public class NPCMenuOption
        {
            public ServiceId ServiceId { get; set; }   // stable internal ID
            public string name { get; set; }           // localized label for display
            public ClickableComponent nameButton { get; set; }

        }
        public NPCServiceMenu(string NewNPCName)
        {
            int startX = Game1.viewport.Width / 2 - ((ButtonWidth + WSpacing) * Columns / 2);
            int startY = Game1.viewport.Height / 2 - 180;

            NPCName = NewNPCName;
            NPCLevel = UpdateNPCLevel(NPCName);
            Quality = GetQuality(NPCLevel);
            QualityName = GetQualityName(Quality);
            CurrentFriendship = Game1.player.friendshipData.TryGetValue(NPCName, out var data) ? data.Points : 0;

            Specialty = Specialties.ContainsKey(NPCName)
                ? Specialties[NPCName]
                : T("SpecialtyGeneral");

            try
            {
                npcPortraits[NPCName] = Game1.content.Load<Texture2D>($"Portraits/{NPCName}");
            }
            
            catch (Exception ex)
            {
                //Monitor.Log($"Portrait for '{NPCName}' not found. Using fallback. Error: {ex.Message}", LogLevel.Warn);
            }

            for (int i = 0; i < GetEnabledServices().Count; i++)
            {
                var (id, label) = GetEnabledServices()[i];

                // Temporary bounds — will be replaced in draw()
                var nameButton = new ClickableComponent(new Rectangle(0, 0, ButtonWidth, ButtonHeight), label);

                options.Add(new NPCMenuOption
                {
                    ServiceId = id,
                    name = label,
                    nameButton = nameButton,
                });
            }

            int frameX = Game1.viewport.Width / 2 - ((ButtonWidth + WSpacing) * Columns / 2) - 1;
            int frameY = Game1.viewport.Height / 2 - 320;

            // Position Portrait button
            int PortraitX = startX - 180; // adjust as needed for spacing
            int PortraitY = startY - 80;

            npcPortraitButton = new ClickableComponent(new Rectangle(PortraitX, PortraitY, 200, 200), "NPCPortraits");

            // Position Specialty Contract button
            int buttonX = frameX + 200; // adjust as needed for spacing
            int buttonY = frameY + 20;

            SpecialtyContractTooltip = T("SpecialtyContractTooltipDynamic", new { specialty = Specialty, npc = NPCName });

            setSpecialtyContractButton = new ClickableComponent(
                new Rectangle(buttonX, buttonY, 510, 60),
                "SetSpecialtyContract"
            );
        }
        public override void draw(SpriteBatch b)
        {
            int framePadding = 20;
            int frameWidth = (ButtonWidth + WSpacing) * Columns + framePadding * 6 + 140;
            int frameHeight = (ButtonHeight + HSpacing * 2) + framePadding * (5 * 5) + 20;
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

            if (npcPortraits.TryGetValue(NPCName, out var portrait))
            {
                Rectangle sourceRect = new Rectangle(0, 0, 64, 64); // top-left portrait
                Rectangle destRect = new Rectangle(npcPortraitButton.bounds.X, npcPortraitButton.bounds.Y, npcPortraitButton.bounds.Width, npcPortraitButton.bounds.Height);

                b.Draw(
                    portrait,
                    destRect,
                    sourceRect,
                    Color.White
                );
            }

            // Measure text size
            string text = T("NewSpecialtyContract", new { specialty = Specialty });

            Vector2 SpecialtytextSize = Game1.smallFont.MeasureString(text);

            // Adjust button width to include 20px buffer on each side
            int SpecialtyadjustedWidth = (int)SpecialtytextSize.X + 40; // 20px left + 20px right

            // Update button bounds with new width
            setSpecialtyContractButton.bounds = new Rectangle(
                setSpecialtyContractButton.bounds.X,
                setSpecialtyContractButton.bounds.Y,
                SpecialtyadjustedWidth,
                setSpecialtyContractButton.bounds.Height
            );

            // Draw the "Specialty Contract" button background
            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                setSpecialtyContractButton.bounds.X,
                setSpecialtyContractButton.bounds.Y,
                setSpecialtyContractButton.bounds.Width,
                setSpecialtyContractButton.bounds.Height,
                Color.White,
                1f,
                false
            );

            // Compute centered text position
            float SpecialtytextX = setSpecialtyContractButton.bounds.X + (setSpecialtyContractButton.bounds.Width / 2f) - (SpecialtytextSize.X / 2f);
            float SpecialtytextY = setSpecialtyContractButton.bounds.Y + (setSpecialtyContractButton.bounds.Height / 2f) - (SpecialtytextSize.Y / 2f);

            // Draw centered text
            Utility.drawTextWithShadow(
                b,
                text,
                Game1.smallFont,
                new Vector2(SpecialtytextX, SpecialtytextY),
                Game1.textColor
            );

            base.draw(b);

            // Config
            int spacing = 20;
            int rowSpacing = 20;
            int maxWidth = (ButtonWidth + WSpacing) * Columns - 20;
            int startY = setSpecialtyContractButton.bounds.Bottom + 40; // place below specialty button

            List<List<(NPCMenuOption option, int width)>> rows = new();
            List<(NPCMenuOption, int)> currentRow = new();
            int currentRowWidth = 0;

            foreach (var option in options)
            {
                Vector2 ButtontextSize = Game1.smallFont.MeasureString(option.name);
                int adjustedWidth = (int)ButtontextSize.X + 40;

                int extraSpacing = currentRow.Count > 0 ? spacing : 0;

                if (currentRowWidth + adjustedWidth + extraSpacing > maxWidth)
                {
                    rows.Add(currentRow);
                    currentRow = new List<(NPCMenuOption, int)>();
                    currentRowWidth = 0;
                    extraSpacing = 0;
                }

                currentRow.Add((option, adjustedWidth));
                currentRowWidth += adjustedWidth + extraSpacing;
            }

            if (currentRow.Count > 0)
                rows.Add(currentRow);

            int currentY = startY;

            foreach (var row in rows)
            {
                int rowWidth = row.Sum(r => r.width) + spacing * (row.Count - 1);
                int rowStartX = Game1.viewport.Width / 2 - ((ButtonWidth + WSpacing) * Columns / 2) +40;
                int currentX = rowStartX;

                foreach (var (option, adjustedWidth) in row)
                {
                    option.nameButton.bounds = new Rectangle(currentX, currentY, adjustedWidth, ButtonHeight);

                    // Hover highlight
                    Color boxColor = option.nameButton.containsPoint(Game1.getMouseX(), Game1.getMouseY())
                                        ? Color.Gold
                                        : Color.White;

                    // Draw button background
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

                    // Center text inside button
                    Vector2 ButtontextSize = Game1.smallFont.MeasureString(option.name);
                    float ButtontextX = option.nameButton.bounds.X + (option.nameButton.bounds.Width / 2f) - (ButtontextSize.X / 2f);
                    float ButtontextY = option.nameButton.bounds.Y + (option.nameButton.bounds.Height / 2f) - (ButtontextSize.Y / 2f);

                    Utility.drawTextWithShadow(
                        b,
                        option.name,
                        Game1.smallFont,
                        new Vector2(ButtontextX, ButtontextY),
                        Game1.textColor
                    );

                    currentX += adjustedWidth + spacing;
                }

                currentY += ButtonHeight + rowSpacing;
            }

            drawMouse(b);

            foreach (var option in options)
            {
                if (option.nameButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    ItemTypeLabel = GetItemTypeLabel(option.name);
                    int feeRate = GetSeviceContractPercents(option.ServiceId);

                    string tooltip = T("NPCCollectsForFee", new { npc = NPCName, item = option.name, rate = feeRate });

                    drawHoverText(
                        b,
                        tooltip,
                        Game1.smallFont,
                        xOffset: -300
                    );
                    break;
                }
            }

            if (npcPortraitButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    T("HoverAboutNPC", new { npc = NPCName }) + "\n" +
                    T("HoverNPCContractorInfo", new { npc = NPCName, level = NPCLevel, quality = QualityName, specialty = Specialty }) + "\n" +
                    T("HoverNPCFriendship", new { npc = NPCName, points = CurrentFriendship }),
                    Game1.smallFont
                );
            }

            if (setSpecialtyContractButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    SpecialtyContractTooltip,
                    Game1.smallFont,
                    xOffset: -400 // shift left
                );
            }
        }
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            foreach (var option in options)
            {
                string npcName = option.name;
                SDV_NPC npc = Game1.getCharacterFromName(npcName, mustBeVillager: false);

                base.receiveLeftClick(x, y, playSound);

                if (npcPortraitButton.containsPoint(x, y))
                {
                    Game1.playSound("smallSelect");
                }

                // Check if the name button was clicked
                if (option.nameButton.containsPoint(x, y))
                {
                    ServiceId npcServiceId = option.ServiceId; // store ID in option
                    Game1.exitActiveMenu();
                    CollectionUtilities.NPCService(NPCName, npcServiceId, Instance.Monitor);
                    return;
                }

                if (setSpecialtyContractButton.containsPoint(x, y))
                {
                    Game1.exitActiveMenu();
                    ContractDispatcher.TryRunIntro(NPCName);
                    return;
                }
            }
            base.receiveLeftClick(x, y, playSound);
        }
        public static int ButtCount(string Butt)
        {
            int Count = 0;

            int[] weedIndices = new int[]
            {
                0, 313, 314, 315, 316, 317, 318,
                674, 675, 676, 677, 678, 679,
                784, 785, 786,
                792, 793, 794,
                882, 883, 884
            };

            if (Butt == "Bee")
            {
                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.Objects.Pairs.ToList())
                    {
                        if (pair.Value is SObject obj &&
                            obj.bigCraftable.Value &&
                            obj.ParentSheetIndex == 10 && // Bee House
                            obj.readyForHarvest.Value &&
                            obj.heldObject.Value is SObject honey)
                        {
                            Count += 1;
                            if (Count > 0)
                                return Count;
                        }
                    
                    }
                }
            }

            if (Butt == "Crab")
            {
                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.Objects.Pairs.ToList())
                    {
                        if (pair.Value is CrabPot pot &&
                            pot.readyForHarvest.Value &&
                            pot.heldObject.Value is SObject catchObj)
                        {
                            Count += 1;
                            if (Count > 0)
                                return Count;
                        }
                    }
                }
            }

            if (Butt == "Weed")
            {
                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.Objects.Pairs.ToList())
                    {
                        if (pair.Value is SObject weeed &&
                            weedIndices.Contains(weeed.ParentSheetIndex) &&
                            !weeed.bigCraftable.Value)
                        {
                            Count += 1;
                            if (Count > 0)
                                return Count;
                        }
                    }
                }
            }

            if (Butt == "Wood")
            {
                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.Objects.Pairs.ToList())
                    {
                        if (pair.Value is SObject woody &&
                            (woody.ParentSheetIndex == 30 || woody.ParentSheetIndex == 294 || woody.ParentSheetIndex == 295 || woody.ParentSheetIndex == 388) &&
                            !woody.bigCraftable.Value)
                        {
                            Count += 1;
                            if (Count > 0)
                                return Count;
                        }
                    }
                }
            }

            if (Butt == "Stone")
            {
                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.Objects.Pairs.ToList())
                    {
                        if (pair.Value is SObject sto &&
                            (sto.ParentSheetIndex == 343 || sto.ParentSheetIndex == 450 || sto.ParentSheetIndex == 668 || sto.ParentSheetIndex == 670) &&
                            !sto.bigCraftable.Value &&
                            sto.canBeGrabbed.Value)
                        {
                            Count += 1;
                            if (Count > 0)
                                return Count;
                        }
                    }
                }
            }

            if (Butt == "Forge")
            {
                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.Objects.Pairs.ToList())
                    {
                        if (pair.Value is SObject forg)
                        {
                            if (!forg.bigCraftable.Value && forg.canBeGrabbed.Value && forg.IsSpawnedObject)
                            {
                                Count += 1;
                                if (Count > 0)
                                    return Count;
                            }
                            else if (forg.bigCraftable.Value &&
                                     (forg.Name == "Mushroom Box" || forg.Name == "Mushroom Log") &&
                                     forg.heldObject.Value is SObject held)
                            {
                                Count += 1;
                                if (Count > 0)
                                    return Count;
                            }
                        }
                    }
                }
            }

            if (Butt == "Tapper")
            {
                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.Objects.Pairs.ToList())
                    {
                        if (pair.Value is SObject tapp &&
                            tapp.bigCraftable.Value &&
                            (tapp.ParentSheetIndex == 105 || tapp.ParentSheetIndex == 264) && // Tapper or Heavy Tapper
                            tapp.readyForHarvest.Value &&
                            tapp.heldObject.Value is SObject tappedProduct)
                        {
                            Count += 1;
                            if (Count > 0)
                                return Count;
                        }
                    }
                }
            }

            if (Butt == "Crop")
            {
                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.terrainFeatures.Pairs.ToList())
                    {
                        if (pair.Value is HoeDirt dirt &&
                            dirt.crop is Crop crop &&
                            crop.fullyGrown.Value)
                        {
                            Count += 1;
                            if (Count > 0)
                                return Count;
                        }
                    }
                }
            }

            if (Butt == "Bait")
            {
                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var kvp in location.Objects.Pairs)
                    {
                        if (kvp.Value is CrabPot cp)
                        {
                            bool hasCatch = cp.heldObject.Value != null;
                            bool isBaited = cp.bait != null && cp.bait.Value != null;

                            if (!isBaited && !hasCatch)
                                Count += 1;
                            if (Count > 0)
                                return Count;
                        }
                    }
                }
            }

            if (Butt == "Hard")
            {
                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var clump in location.resourceClumps.ToList())
                    {
                        if (clump.parentSheetIndex.Value == ResourceClump.stumpIndex ||
                            clump.parentSheetIndex.Value == ResourceClump.hollowLogIndex)
                        {
                            Count += 1;
                            if (Count > 0)
                                return Count;
                        }
                    }
                }
            }

            if (Butt == "Stone")
            {
                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var clump in location.resourceClumps.ToList())
                    {
                        if (clump.parentSheetIndex.Value == ResourceClump.boulderIndex)
                        {
                            Count += 1;
                            if (Count > 0)
                                return Count;
                        }
                    }
                }
            }

            if (Butt == "Pots")
            {
                GameLocation currentLocation = Game1.player.currentLocation;
                var candidateTiles = new List<Vector2>();

                for (int x = 0; x < currentLocation.Map.DisplayWidth / Game1.tileSize; x++)
                {
                    for (int y = 0; y < currentLocation.Map.DisplayHeight / Game1.tileSize; y++)
                    {
                        var tile = new Vector2(x, y);
                        if (currentLocation.doesTileHaveProperty(x, y, "Water", "Back") != null &&
                            !currentLocation.Objects.ContainsKey(tile))
                        {
                            Count += 1;
                            if (Count > 0)
                                return Count;
                        }
                    }
                }
            }
            return Count;
        } 
    }
}
