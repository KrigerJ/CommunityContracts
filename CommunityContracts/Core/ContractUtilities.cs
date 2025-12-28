using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using xTile.Dimensions;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;


namespace CommunityContracts.Core
{
    public static class ContractUtilities
    {
        public class ContractsDelivery
        {
            public List<Item> Items { get; set; } = new();
            public long RecipientID { get; set; } // New field

        }
        public static int SafeMultiplier(int value)
        {
            return Math.Max(1, value);
        }
        public static int GetContractPercent(string ProductType)
        {
            return Config.NPCContractPercents.TryGetValue(ProductType, out var value)
                ? value
                : Config.NPCContractPercents["Basic"];
        }
        public static int GetSeviceContractPercents(ServiceId ProductType)
        {
            return Config.SeviceContractPercents.TryGetValue(ProductType, out var value)
                ? value
                : Config.SeviceContractPercents[ServiceId.CrabPots];
        }
        public static string GetItemTypeLabel(string ItemType)
        {
            return ItemTypeLabels.TryGetValue(ItemType, out var value)
                ? value
                : ItemTypeLabels["Weeds"];
        }
        public static string GetServiceTypeLabel(ServiceId ItemType)
        {
            return ServiceTypeLabels.TryGetValue(ItemType, out var value)
                ? value
                : ServiceTypeLabels[ServiceId.Weeds];
        }
        public static int GetQuality(int NPCLevel)
        {
            return NPCLevel switch
            {
                >= 10 => 4, // Iridium
                >= 7 => 2,  // Gold
                >= 3 => 1,  // Silver
                _ => 0      // Normal
            };
        }
        public static float GetQualityMultiplier(int ProductQuality)
        {
            return ProductQuality switch
            {
                0 => 1.0f,  // Normal
                1 => 1.25f, // Silver
                2 => 1.5f,  // Gold
                4 => 2.0f,  // Iridium
                _ => 1.0f
            };
        }
        public static float GetHoneyMultiplier(int Flower)
        {
            return Flower switch
            {
                376 => 3.8f,  // Poppy
                418 => 1.0f,  // Crocus
                421 => 2.6f,  // Sunflower
                591 => 1.6f,  // Tulip
                593 => 2.8f, // Summer Spangle
                595 => 6.8f,  // Fairy Rose
                597 => 2.0f,  // Blue Jazz
                _ => 1.0f
            };
        }
        public static float GetCut(float contractorCut)
        {
            if (contractorCut < 1000)
                contractorCut = (contractorCut / 10) * 10;       // Round to nearest 10
            else if (contractorCut < 5000)
                contractorCut = (contractorCut / 50) * 50;       // Round to nearest 50
            else if (contractorCut < 20000)
                contractorCut = (contractorCut / 100) * 100;     // Round to nearest 100
            else if (contractorCut < 100000)
                contractorCut = (contractorCut / 500) * 500;     // Round to nearest 500
            else
                contractorCut = (contractorCut / 1000) * 1000;    // Round to nearest 1000

            return contractorCut;
        }
        public static int GetTotalValue(int contractorCut)
        {
            if (contractorCut < 1000)
                contractorCut = (contractorCut / 10) * 10;       // Round to nearest 10
            else if (contractorCut < 5000)
                contractorCut = (contractorCut / 50) * 50;       // Round to nearest 50
            else if (contractorCut < 20000)
                contractorCut = (contractorCut / 100) * 100;     // Round to nearest 100
            else if (contractorCut < 100000)
                contractorCut = (contractorCut / 500) * 500;     // Round to nearest 500
            else
                contractorCut = (contractorCut / 1000) * 1000;    // Round to nearest 1000

            return contractorCut;
        }
        public static string GetQualityName(int Quality)
        {
            return Quality switch
            {
                >= 4 => T("QualityIridium"),
                >= 2 => T("QualityGold"),
                >= 1 => T("QualitySilver"),
                _ => T("QualityNormal")
            };
        }
        public static int GetSeasonIndex(string Season)
        {
            return Season switch
            {
                "spring" => 0,
                "summer" => 1,
                "fall" => 2,
                "winter" => 3,
                _ => 0
            };
        }
        public static int CountProcessors(string processorName)
        {
            var allLocations = new List<GameLocation>(Game1.locations);

            // Add interiors of farm buildings (Coops, Barns, Sheds, etc.)
            if (Game1.getLocationFromName("Farm") is Farm farm)
            {
                foreach (var building in farm.buildings)
                {
                    if (building.indoors?.Value != null)
                        allLocations.Add(building.indoors.Value);
                }
            }

            return allLocations
                .SelectMany(loc => loc.objects.Values)
                .Count(obj => obj != null && obj.Name == processorName);
        }
        public static string GetItemName(int id)
        {
            return (ItemRegistry.Create($"(O){id}") as SObject)?.DisplayName ?? T("UnknownItem", new { id });
        }
        public static void DeliverContractsItems(List<ContractsDelivery> deliveries, ModConfig config)
        {
            if (deliveries.Count == 0)
                return;

            foreach (var delivery in deliveries)
            {
                var farmer = Game1.getAllFarmers().FirstOrDefault(f => f.UniqueMultiplayerID == delivery.RecipientID);
                if (farmer == null)
                    continue;

                GameLocation location = Game1.getLocationFromName(config.DropLocationName);

                if (location == null)
                {
                    location = Game1.getLocationFromName("Farm");
                }

                Vector2 dropTile;

                if (!config.PresetLocations.TryGetValue(config.DropLocationName, out dropTile))
                {
                    Instance.Monitor.Log( T("DropLocationNotFound", new { name = config.DropLocationName }), LogLevel.Warn);

                    dropTile = new Vector2(68, 15);
                }
                // Validate drop tile before placing chest
                if (location is Farm farmLocation &&
                    !farmLocation.isTileLocationOpen(new Location((int)dropTile.X, (int)dropTile.Y)))
                {
                    Instance.Monitor.Log( T("DropTileBlocked", new { tile = dropTile }), LogLevel.Trace);
                    dropTile = new Vector2(59, 15); // Safe fallback near farmhouse
                }
                // Place or reuse chest
                if (!location.objects.TryGetValue(dropTile, out var obj) || obj is not Chest chest)
                {
                    chest = new Chest(true);
                    location.objects[dropTile] = chest;
                    chest.name = T("ContractDeliveryChest");

                    if (config.ChestColors.TryGetValue(config.DeliveryChestColor, out var tint))
                    {
                        chest.playerChoiceColor.Value = tint;
                        chest.modData["CommunityContracts/DeliveryColor"] = config.DeliveryChestColor;
                    }
                }
                else if (obj is Chest existingChest)
                {
                    chest = existingChest;

                    if (chest.modData.TryGetValue("CommunityContracts/DeliveryColor", out var savedColor) &&
                        config.ChestColors.TryGetValue(savedColor, out var tint))
                    {
                        chest.playerChoiceColor.Value = tint;
                    }
                }

                var chestItems = chest.GetItemsForPlayer(farmer.UniqueMultiplayerID);
                foreach (var item in delivery.Items)
                    chestItems.Add(item);

                if (farmer.IsLocalPlayer)
                {
                    string summary = string.Join(", ", delivery.Items.Select(i => $"{i.Stack} {i.DisplayName}"));
                    Game1.showGlobalMessage( T("ShipmentDelivered", new { summary }));
                }
            }
            deliveries.Clear();
        }
        public static int GetRecycleQuantity(int itemID) => itemID switch
        {
            93 => 3,
            380 => 3,
            382 => 5,
            388 => 15,
            390 => 10,
            _ => 1
        };
        public static int UpdateNPCLevel(string NPCName)
        {
            return (int)((Game1.player.friendshipData.TryGetValue(NPCName, out var data) ? data.Points : 0) / 250);
        }

        public static Dictionary<string, string> ItemTypeLabels = new()
        {
            { "Animals", T("ItemTypeAnimals") },
            { "Crab Pots", T("ItemTypeCrabPots") },
            { "Crops", T("ItemTypeCrops") },
            { "Forageables", T("ItemTypeForageables") },
            { "Hardwood", T("ItemTypeHardwood") },
            { "Honey", T("ItemTypeHoney") },
            { "Stone", T("ItemTypeStone") },
            { "Weeds", T("ItemTypeWeeds") },
            { "Wood", T("ItemTypeWood") },
            { "Tappers", T("ItemTypeTappers") }
        };

        public static Dictionary<ServiceId, string> ServiceTypeLabels = new()
        {
            { ServiceId.Animals, T("ItemTypeAnimals") },
            { ServiceId.CrabPots, T("ItemTypeCrabPots") },
            { ServiceId.Crops, T("ItemTypeCrops") },
            { ServiceId.Forageables, T("ItemTypeForageables") },
            { ServiceId.Hardwood, T("ItemTypeHardwood") },
            { ServiceId.Honey, T("ItemTypeHoney") },
            { ServiceId.Stone, T("ItemTypeStone") },
            { ServiceId.Weeds, T("ItemTypeWeeds") },
            { ServiceId.Wood, T("ItemTypeWood") },
            { ServiceId.Tappers, T("ItemTypeTappers") }
        };
    }
}
