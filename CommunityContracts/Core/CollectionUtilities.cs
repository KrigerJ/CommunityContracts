using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core
{
    public static class CollectionUtilities
    {
        public static ModConfig config;
        public static Dictionary<ServiceId, bool> IsCollectingService = new();
        private static void AddToItemMap(Dictionary<(int index, int quality), SObject> itemMap, int index, int quality)
        {
            var key = (index, quality);

            if (!itemMap.TryGetValue(key, out var stacked))
            {
                stacked = new SObject($"{index}", 1);
                stacked.Quality = quality;
                itemMap[key] = stacked;
            }
            else
            {
                stacked.Stack += 1;
            }
        }
        private static void AddToItemMap(Dictionary<int, SObject> itemMap, int index, int quality)
        {
            if (!itemMap.TryGetValue(index, out var stacked))
            {
                stacked = new SObject($"{index}", 1);
                stacked.Quality = quality;
                itemMap[index] = stacked;
            }
            else
            {
                stacked.Stack += 1;
                stacked.Quality = quality;
            }
        }
        public static List<Item> ScanCrabPotItems(IMonitor monitor, string npcName)
        {
            var itemMap = new Dictionary<(int index, int quality), SObject>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is CrabPot pot &&
                        pot.readyForHarvest.Value &&
                        pot.heldObject.Value is SObject catchObj)
                    {
                        int index = catchObj.ParentSheetIndex;
                        var key = (index, quality);

                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject($"{index}", 1);
                            stacked.Quality = quality;
                            itemMap[key] = stacked;
                        }
                        else
                        {
                            stacked.Stack += 1;
                        }
                    }
                }
            }
            return itemMap.Values.Cast<Item>().ToList();
        }
        public static async void GatherCrabPotItemsWithDelay(IMonitor monitor, string npcName, Action<List<Item>> onComplete)
        {
            var itemMap = new Dictionary<(int index, int quality), SObject>();
            var potQueue = new Queue<(GameLocation location, Vector2 tile, SObject catchObj)>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);
            int FarmerSkillLevel = Game1.player.fishingLevel.Value;

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is CrabPot pot &&
                        pot.readyForHarvest.Value &&
                        pot.heldObject.Value is SObject catchObj)
                    {
                        potQueue.Enqueue((location, pair.Key, catchObj));
                    }
                }
            }

            IsNPCCollecting[npcName] = true;

            while (potQueue.Count > 0)
            {
                var (location, tile, catchObj) = potQueue.Dequeue();
                int index = catchObj.ParentSheetIndex;
                var key = (index, quality);

                if (!itemMap.TryGetValue(key, out var stacked))
                {
                    stacked = new SObject($"{index}", 1);
                    stacked.Quality = quality;
                    itemMap[key] = stacked;
                }
                else
                {
                    stacked.Stack += 1;
                }

                if (location.Objects[tile] is CrabPot pot)
                {
                    pot.heldObject.Value = null;
                    pot.readyForHarvest.Value = false;
                }

                // Deliver if any stack reaches 999
                if (stacked.Stack >= 999)
                {
                    var deliveryChunk = new List<Item> { stacked.getOne() };
                    deliveryChunk[0].Stack = 999;

                    stacked.Stack -= 999;

                    DeliverContractsItems(new List<ContractsDelivery>
                    {
                        new ContractsDelivery
                        {
                            Items = deliveryChunk,
                            RecipientID = Game1.player.UniqueMultiplayerID
                        }
                    }, Config);

                    Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                }

                // Skip delay if day is ending
                if (!Game1.newDay)
                {
                    await Task.Delay(Config.CollectionDelay / SafeMultiplier(npcLevel + FarmerSkillLevel));
                }
                else
                {
                    //monitor.Log(T("DayEndedBypassDelay"), LogLevel.Trace);
                }
            }

            IsNPCCollecting[npcName] = false;

            // Final delivery of remaining items
            var finalItems = itemMap.Values.Where(i => i.Stack > 0).Cast<Item>().ToList();
            onComplete(finalItems);
        }
        public static List<Item> ScanHoneyItems(IMonitor monitor, string npcName)
        {
            var itemMap = new Dictionary<(int index, int quality), SObject>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);

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
                        int index = honey.ParentSheetIndex;
                        var key = (index, quality);

                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject($"{index}", 1);
                            stacked.Quality = quality;
                            itemMap[key] = stacked;
                        }
                        else
                        {
                            stacked.Stack += 1;
                        }
                    }
                }
            }
            return itemMap.Values.Cast<Item>().ToList();
        }
        public static async void GatherBeeHouseHoneyWithRealTimeDelay(IMonitor monitor, string npcName, Action<List<Item>> onComplete)
        {
            var itemMap = new Dictionary<int, SObject>();
            var honeyQueue = new Queue<(GameLocation location, Vector2 tile, SObject honey)>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);
            int FarmerSkillLevel = Game1.player.farmingLevel.Value;

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
                        honeyQueue.Enqueue((location, pair.Key, honey));
                    }
                }
            }

            IsNPCCollecting[npcName] = true;

            while (honeyQueue.Count > 0)
            {
                var (location, tile, honey) = honeyQueue.Dequeue();
                int index = honey.ParentSheetIndex;
                var key = (index, quality);

                if (!itemMap.TryGetValue(index, out var stacked))
                {
                    stacked = new SObject($"{index}", 1);
                    stacked.Quality = quality;
                    itemMap[index] = stacked;
                }
                else
                {
                    stacked.Stack += 1;
                }

                if (location.Objects[tile] is SObject beeHouse)
                {
                    beeHouse.heldObject.Value = null;
                    beeHouse.readyForHarvest.Value = false;
                }

                // Deliver if stack reaches 999
                if (stacked.Stack >= 999)
                {
                    var deliveryChunk = new List<Item> { stacked.getOne() };
                    deliveryChunk[0].Stack = 999;

                    stacked.Stack -= 999;

                    DeliverContractsItems(new List<ContractsDelivery>
                    {
                        new ContractsDelivery
                        {
                            Items = deliveryChunk,
                            RecipientID = Game1.player.UniqueMultiplayerID
                        }
                    }, Config);

                    Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                }

                // Skip delay if day is ending
                if (!Game1.newDay)
                {
                    await Task.Delay(Config.CollectionDelay / SafeMultiplier(npcLevel + FarmerSkillLevel));
                }
                else
                {
                    //monitor.Log(T("DayEndedBypassDelay"), LogLevel.Trace);
                }
            }

            IsNPCCollecting[npcName] = false;

            // Final delivery of remaining items
            var finalItems = itemMap.Values.Where(i => i.Stack > 0).Cast<Item>().ToList();
            onComplete(finalItems);
        }
        public static List<Item> ScanFarmCrops(IMonitor monitor, string npcName)
        {
            var itemMap = new Dictionary<(int index, int quality), SObject>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.terrainFeatures.Pairs.ToList())
                {
                    if (pair.Value is HoeDirt dirt &&
                        dirt.crop is Crop crop &&
                        crop.fullyGrown.Value)
                    {

                        string Sindex = crop.indexOfHarvest.Value;
                        if (int.TryParse(Sindex, out int index))
                        {
                            var key = (index, quality);
                            if (!itemMap.TryGetValue(key, out var stacked))
                            {
                                stacked = new SObject($"{index}", 1);
                                stacked.Quality = quality;
                                itemMap[key] = stacked;
                            }
                            else
                            {
                                stacked.Stack += 1;
                            }
                        }
                    }

                }
            }
            return itemMap.Values.Cast<Item>().ToList();
        }
        public static async void GatherFarmCropsWithRealTimeDelay(IMonitor monitor, string npcName, Action<List<Item>> onComplete)
        {
            var itemMap = new Dictionary<int, SObject>();
            var cropQueue = new Queue<(GameLocation location, Vector2 tile, Crop crop)>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);
            int FarmerSkillLevel = Game1.player.farmingLevel.Value;

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.terrainFeatures.Pairs.ToList())
                {
                    if (pair.Value is HoeDirt dirt &&
                        dirt.crop is Crop crop &&
                        crop.fullyGrown.Value)
                    {
                        cropQueue.Enqueue((location, pair.Key, crop));
                    }
                }
            }

            IsNPCCollecting[npcName] = true;

            while (cropQueue.Count > 0)
            {
                var (location, tile, crop) = cropQueue.Dequeue();
                string Sindex = crop.indexOfHarvest.Value;

                if (int.TryParse(Sindex, out int index))
                {
                    // Stack logic unchanged...
                    if (!itemMap.TryGetValue(index, out var stacked))
                    {
                        stacked = new SObject($"{index}", 1);
                        stacked.Quality = quality;
                        itemMap[index] = stacked;
                    }
                    else
                    {
                        stacked.Stack += 1;
                        stacked.Quality = quality;
                    }

                    // Handle regrowable vs one-time crops
                    if (crop.dayOfCurrentPhase.Value > 0)
                    {
                        // Reset the existing crop to one day before harvest
                        var hoe = location.terrainFeatures[tile] as HoeDirt;
                        var c = hoe?.crop;
                        if (c != null)
                        {
                            // Ensure it's at the last phase
                            c.currentPhase.Value = c.phaseDays.Count - 1;

                            // One day before being ready again
                            c.dayOfCurrentPhase.Value = Math.Max(0, c.dayOfCurrentPhase.Value - 1);

                            // Keep it as a mature plant that is not yet harvestable
                            c.fullyGrown.Value = true;
                            //c.readyForHarvest.Value = false;

                            // Preserve harvest index just in case
                            c.indexOfHarvest.Value = crop.indexOfHarvest.Value;
                        }
                    }
                    else
                    {
                        // One-time crops: clear the tile
                        (location.terrainFeatures[tile] as HoeDirt).crop = null;
                    }

                    // Deliver if stack reaches 999
                    if (stacked.Stack >= 999)
                    {
                        var deliveryChunk = new List<Item> { stacked.getOne() };
                        deliveryChunk[0].Stack = 999;

                        stacked.Stack -= 999;

                        DeliverContractsItems(new List<ContractsDelivery>
                        {
                            new ContractsDelivery
                            {
                                Items = deliveryChunk,
                                RecipientID = Game1.player.UniqueMultiplayerID
                            }
                        }, Config);

                        Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                    }
                    crop.dayOfCurrentPhase.Value = 2; // Simulate regrowth
                }

                // Skip delay if day is ending
                if (!Game1.newDay)
                {
                    await Task.Delay(Config.CollectionDelay / SafeMultiplier(npcLevel + FarmerSkillLevel));
                }
                else
                {
                    //monitor.Log(T("DayEndedBypassDelay"), LogLevel.Trace);
                }
            }

            IsNPCCollecting[npcName] = false;

            // Final delivery of remaining items
            var finalItems = itemMap.Values.Where(i => i.Stack > 0).Cast<Item>().ToList();
            onComplete(finalItems);
        }
        public static List<Item> ScanHardwoodItems(IMonitor monitor, string npcName)
        {
            var itemMap = new Dictionary<(int index, int quality), SObject>();
            int hardwoodIndex = 709;
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var clump in location.resourceClumps.ToList())
                {
                    if (clump.parentSheetIndex.Value == ResourceClump.stumpIndex ||
                        clump.parentSheetIndex.Value == ResourceClump.hollowLogIndex)
                    {
                        int amount = quality + (clump.parentSheetIndex.Value == ResourceClump.stumpIndex ? 2 : 8);
                        var key = (hardwoodIndex, quality);

                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject($"{hardwoodIndex}", amount);
                            stacked.Quality = quality;
                            itemMap[key] = stacked;
                        }
                        else
                        {
                            stacked.Stack += amount;
                        }
                    }
                }
            }
            return itemMap.Values.Cast<Item>().ToList();
        }
        public static async void GatherHardwoodWithRealTimeDelay(IMonitor monitor, string npcName, Action<List<Item>> onComplete)
        {
            var itemMap = new Dictionary<int, SObject>();
            var clumpQueue = new Queue<(GameLocation location, ResourceClump clump)>();
            int hardwoodIndex = 709;
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);
            int FarmerSkillLevel = Game1.player.foragingLevel.Value;

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var clump in location.resourceClumps.ToList())
                {
                    if (clump.parentSheetIndex.Value == ResourceClump.stumpIndex ||
                        clump.parentSheetIndex.Value == ResourceClump.hollowLogIndex)
                    {
                        clumpQueue.Enqueue((location, clump));
                    }
                }
            }

            IsNPCCollecting[npcName] = true;

            while (clumpQueue.Count > 0)
            {
                var (location, clump) = clumpQueue.Dequeue();
                int amount = quality + (clump.parentSheetIndex.Value == ResourceClump.stumpIndex ? 2 : 8);

                if (!itemMap.TryGetValue(hardwoodIndex, out var stacked))
                {
                    stacked = new SObject($"{hardwoodIndex}", amount);
                    itemMap[hardwoodIndex] = stacked;
                }
                else
                {
                    stacked.Stack += amount;
                }

                location.resourceClumps.Remove(clump);

                // Deliver if stack reaches 999
                if (itemMap[hardwoodIndex].Stack >= 999)
                {
                    var deliveryChunk = new List<Item> { itemMap[hardwoodIndex].getOne() };
                    deliveryChunk[0].Stack = 999;

                    itemMap[hardwoodIndex].Stack -= 999;

                    DeliverContractsItems(new List<ContractsDelivery>
                    {
                        new ContractsDelivery
                        {
                            Items = deliveryChunk,
                            RecipientID = Game1.player.UniqueMultiplayerID
                        }
                    }, Config);

                    Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                }

                // Skip delay if day is ending
                if (!Game1.newDay)
                {
                    await Task.Delay(Config.CollectionDelay / SafeMultiplier(npcLevel + FarmerSkillLevel));
                }
                else
                {
                    //monitor.Log(T("DayEndedBypassDelay"), LogLevel.Trace);
                }
            }

            IsNPCCollecting[npcName] = false;

            // Final delivery of remaining items
            var finalItems = itemMap.Values.Where(i => i.Stack > 0).Cast<Item>().ToList();
            onComplete(finalItems);
        }
        public static List<Item> ScanWeedsItems(IMonitor monitor, string npcName)
        {
            var itemMap = new Dictionary<int, SObject>();
            int fiberIndex = 771;
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);

            int[] weedIndices = new int[]
            {
                0, 313, 314, 315, 316, 317, 318,
                674, 675, 676, 677, 678, 679,
                784, 785, 786,
                792, 793, 794,
                882, 883, 884
            };

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        weedIndices.Contains(obj.ParentSheetIndex) &&
                        !obj.bigCraftable.Value)
                    {
                        int amount = 1;

                        if (!itemMap.TryGetValue(fiberIndex, out var stacked))
                        {
                            stacked = new SObject($"{fiberIndex}", amount);
                            stacked.Quality = quality;
                            itemMap[fiberIndex] = stacked;
                        }
                        else
                        {
                            stacked.Stack += amount;
                        }

                        // End contract if stack reaches 999
                        if (itemMap[fiberIndex].Stack >= 999)
                        {
                            return itemMap.Values.Cast<Item>().ToList();
                        }
                    }
                }
            }

            return itemMap.Values.Cast<Item>().ToList();
        }
        public static async void GatherWeedsWithRealTimeDelay(IMonitor monitor, string npcName, Action<List<Item>> onComplete)
        {
            var itemMap = new Dictionary<int, SObject>();
            var debrisQueue = new Queue<(GameLocation location, Vector2 tile, int amount)>();
            int fiberIndex = 771;
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);
            int FarmerSkillLevel = Game1.player.farmingLevel.Value;

            int[] weedIndices = new int[]
            {
                0, 313, 314, 315, 316, 317, 318,
                674, 675, 676, 677, 678, 679,
                784, 785, 786,
                792, 793, 794,
                882, 883, 884
            };

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        weedIndices.Contains(obj.ParentSheetIndex) &&
                        !obj.bigCraftable.Value)
                    {
                        int amount = 1;
                        debrisQueue.Enqueue((location, pair.Key, amount));
                    }
                }
            }

            IsNPCCollecting[npcName] = true;

            while (debrisQueue.Count > 0 && (!itemMap.TryGetValue(fiberIndex, out var current) || current.Stack < 999))
            {
                var (location, tile, amount) = debrisQueue.Dequeue();

                if (!itemMap.TryGetValue(fiberIndex, out var stacked))
                {
                    stacked = new SObject($"{fiberIndex}", amount);
                    itemMap[fiberIndex] = stacked;
                }
                else
                {
                    stacked.Stack += amount;
                }

                location.Objects.Remove(tile);

                // Skip delay if day is ending
                if (!Game1.newDay)
                {
                    await Task.Delay(Config.CollectionDelay / SafeMultiplier(npcLevel + FarmerSkillLevel));
                }
                else
                {
                    //monitor.Log(T("DayEndedBypassDelay"), LogLevel.Trace);
                }
            }

            if (itemMap.TryGetValue(fiberIndex, out var finalStack) && finalStack.Stack > 0)
            {
                int deliverCount = Math.Min(finalStack.Stack, 999);

                var deliveryChunk = new List<Item> { finalStack.getOne() };
                deliveryChunk[0].Stack = deliverCount;

                DeliverContractsItems(new List<ContractsDelivery>
                {
                    new ContractsDelivery
                    {
                        Items = deliveryChunk,
                        RecipientID = Game1.player.UniqueMultiplayerID
                    }
                }, Config);

                Game1.showGlobalMessage(T("ShipmentDelivered", new { summary = deliveryChunk[0].DisplayName }));
            }
            else
            {
                onComplete(new List<Item>());
            }

            IsNPCCollecting[npcName] = false;
        }

        public static List<Item> ScanWoodItems(IMonitor monitor, string npcName)
        {
            var itemMap = new Dictionary<(int index, int quality), SObject>();
            int woodIndex = 388;
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        (obj.ParentSheetIndex == 30 || obj.ParentSheetIndex == 294 || obj.ParentSheetIndex == 295 || obj.ParentSheetIndex == 388) &&
                        !obj.bigCraftable.Value)
                    {
                        int amount = 1 + quality;
                        var key = (woodIndex, quality);

                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject($"{woodIndex}", amount);
                            stacked.Quality = quality;
                            itemMap[key] = stacked;
                        }
                        else
                        {
                            stacked.Stack += amount;
                        }
                    }
                }
            }
            return itemMap.Values.Cast<Item>().ToList();
        }
        public static async void GatherWoodWithRealTimeDelay(IMonitor monitor, string npcName, Action<List<Item>> onComplete)
        {
            var itemMap = new Dictionary<int, SObject>();
            var debrisQueue = new Queue<(GameLocation location, Vector2 tile, int amount)>();
            int woodIndex = 388;
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);
            int FarmerSkillLevel = Game1.player.foragingLevel.Value;

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        (obj.ParentSheetIndex == 30 || obj.ParentSheetIndex == 294 || obj.ParentSheetIndex == 295 || obj.ParentSheetIndex == 388) &&
                        !obj.bigCraftable.Value)
                    {
                        int amount = 1 + quality;
                        debrisQueue.Enqueue((location, pair.Key, amount));
                    }
                }
            }

            IsNPCCollecting[npcName] = true;

            while (debrisQueue.Count > 0)
            {
                var (location, tile, amount) = debrisQueue.Dequeue();

                if (!itemMap.TryGetValue(woodIndex, out var stacked))
                {
                    stacked = new SObject($"{woodIndex}", amount);
                    itemMap[woodIndex] = stacked;
                }
                else
                {
                    stacked.Stack += amount;
                }

                location.Objects.Remove(tile);

                // Deliver if stack reaches 999
                if (itemMap[woodIndex].Stack >= 999)
                {
                    var deliveryChunk = new List<Item> { itemMap[woodIndex].getOne() };
                    deliveryChunk[0].Stack = 999;

                    itemMap[woodIndex].Stack -= 999;

                    DeliverContractsItems(new List<ContractsDelivery>
                    {
                        new ContractsDelivery
                        {
                            Items = deliveryChunk,
                            RecipientID = Game1.player.UniqueMultiplayerID
                        }
                    }, Config);

                    Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                }

                // Skip delay if day is ending
                if (!Game1.newDay)
                {
                    await Task.Delay(Config.CollectionDelay / SafeMultiplier(npcLevel + FarmerSkillLevel));
                }
                else
                {
                    //monitor.Log(T("DayEndedBypassDelay"), LogLevel.Trace);
                }
            }

            IsNPCCollecting[npcName] = false;

            // Final delivery of remaining items
            var finalItems = itemMap.Values.Where(i => i.Stack > 0).Cast<Item>().ToList();
            onComplete(finalItems);
        }
        public static List<Item> ScanStoneItems(IMonitor monitor, string npcName)
        {
            var itemMap = new Dictionary<int, SObject>();
            int stoneIndex = 390;
            int coalIndex = 382;
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);
            int totalStone = 0;

            foreach (GameLocation location in Game1.locations)
            {
                // Large boulders
                foreach (var clump in location.resourceClumps.ToList())
                {
                    if (clump.parentSheetIndex.Value == ResourceClump.boulderIndex)
                    {
                        int amount = 15 + quality;
                        totalStone += amount;

                        if (!itemMap.TryGetValue(stoneIndex, out var stacked))
                        {
                            stacked = new SObject($"{stoneIndex}", amount);
                            stacked.Quality = quality;
                            itemMap[stoneIndex] = stacked;
                        }
                        else
                        {
                            stacked.Stack += amount;
                        }
                    }
                }

                // Small debris stones
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        (obj.ParentSheetIndex == 343 || obj.ParentSheetIndex == 450 || obj.ParentSheetIndex == 668 || obj.ParentSheetIndex == 670) &&
                        !obj.bigCraftable.Value &&
                        obj.canBeGrabbed.Value)
                    {
                        int amount = 1 + quality;
                        totalStone += amount;

                        if (!itemMap.TryGetValue(stoneIndex, out var stacked))
                        {
                            stacked = new SObject($"{stoneIndex}", amount);
                            stacked.Quality = quality;
                            itemMap[stoneIndex] = stacked;
                        }
                        else
                        {
                            stacked.Stack += amount;
                        }
                    }
                }
            }

            // Simulate coal yield (~15%)
            int estimatedCoal = (int)Math.Round(totalStone * 0.15);
            if (estimatedCoal > 0)
            {
                itemMap[coalIndex] = new SObject($"{coalIndex}", estimatedCoal);
            }
            return itemMap.Values.Cast<Item>().ToList();
        }
        public static async void GatherStoneWithRealTimeDelay(IMonitor monitor, string npcName, Action<List<Item>> onComplete)
        {
            var itemMap = new Dictionary<int, SObject>();
            var stoneQueue = new Queue<(GameLocation location, Vector2 tile, int amount, bool isClump)>();
            int stoneIndex = 390;
            int coalIndex = 382;
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);
            int totalStone = 0;
            int FarmerSkillLevel = Game1.player.miningLevel.Value;

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var clump in location.resourceClumps.ToList())
                {
                    if (clump.parentSheetIndex.Value == ResourceClump.boulderIndex)
                    {
                        int amount = 15 + quality;
                        totalStone += amount;
                        stoneQueue.Enqueue((location, clump.Tile, amount, true));
                    }
                }

                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        (obj.ParentSheetIndex == 343 || obj.ParentSheetIndex == 450 || obj.ParentSheetIndex == 668 || obj.ParentSheetIndex == 670) &&
                        !obj.bigCraftable.Value &&
                        obj.canBeGrabbed.Value)
                    {
                        int amount = 1 + quality;
                        totalStone += amount;
                        stoneQueue.Enqueue((location, pair.Key, amount, false));
                    }
                }
            }

            IsNPCCollecting[npcName] = true;

            while (stoneQueue.Count > 0)
            {
                var (location, tile, amount, isClump) = stoneQueue.Dequeue();

                if (!itemMap.TryGetValue(stoneIndex, out var stacked))
                {
                    stacked = new SObject($"{stoneIndex}", amount);
                    itemMap[stoneIndex] = stacked;
                }
                else
                {
                    stacked.Stack += amount;
                }

                if (isClump)
                {
                    var clump = location.resourceClumps.FirstOrDefault(c => c.Tile == tile && c.parentSheetIndex.Value == ResourceClump.boulderIndex);
                    if (clump != null)
                        location.resourceClumps.Remove(clump);
                }
                else
                {
                    location.Objects.Remove(tile);
                }

                // Deliver if stone stack reaches 999
                if (itemMap[stoneIndex].Stack >= 999)
                {
                    var deliveryChunk = new List<Item> { itemMap[stoneIndex].getOne() };
                    deliveryChunk[0].Stack = 999;

                    itemMap[stoneIndex].Stack -= 999;

                    DeliverContractsItems(new List<ContractsDelivery>
                    {
                        new ContractsDelivery
                        {
                            Items = deliveryChunk,
                            RecipientID = Game1.player.UniqueMultiplayerID
                        }
                    }, Config);

                    Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                }

                // Skip delay if day is ending
                if (!Game1.newDay)
                {
                    await Task.Delay(Config.CollectionDelay / SafeMultiplier(npcLevel + FarmerSkillLevel));
                }
                else
                {
                    //monitor.Log(T("DayEndedBypassDelay"), LogLevel.Trace);
                }
            }

            // Simulate coal yield (~15%)
            int estimatedCoal = (int)Math.Round(totalStone * 0.15);

            if (estimatedCoal > 0)
            {
                itemMap[coalIndex] = new SObject($"{coalIndex}", estimatedCoal);
            }

            IsNPCCollecting[npcName] = false;

            // Final delivery of remaining items
            var finalItems = itemMap.Values.Where(i => i.Stack > 0).Cast<Item>().ToList();
            onComplete(finalItems);
        }
        public static List<Item> ScanForageablesItems(IMonitor monitor, string npcName)
        {
            var itemMap = new Dictionary<(int index, int quality), SObject>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);

            foreach (GameLocation location in Game1.locations)
            {

                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj)
                    {
                        // Regular forageables
                        if (!obj.bigCraftable.Value && obj.canBeGrabbed.Value && obj.IsSpawnedObject)
                        {
                            AddToItemMap(itemMap, obj.ParentSheetIndex, quality);
                        }
                        // Mushroom Box / Mushroom Log producers
                        else if (obj.bigCraftable.Value &&
                                 (obj.Name == "Mushroom Box" || obj.Name == "Mushroom Log") &&
                                 obj.heldObject.Value is SObject held)
                        {
                            AddToItemMap(itemMap, held.ParentSheetIndex, quality);
                        }
                    }
                }
            }
            return itemMap.Values.Cast<Item>().ToList();
        }
        public static async void GatherForageablesWithRealTimeDelay(IMonitor monitor, string npcName, Action<List<Item>> onComplete)
        {
            var itemMap = new Dictionary<int, SObject>();
            var forageQueue = new Queue<(GameLocation location, Vector2 tile, SObject obj)>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);
            int FarmerSkillLevel = Game1.player.foragingLevel.Value;

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj)
                    {
                        // Regular forageables
                        if (!obj.bigCraftable.Value && obj.canBeGrabbed.Value && obj.IsSpawnedObject)
                        {
                            forageQueue.Enqueue((location, pair.Key, obj));
                        }

                        // Mushroom Box / Mushroom Log
                        else if (obj.bigCraftable.Value &&
                                 (obj.Name == "Mushroom Box" || obj.Name == "Mushroom Log") &&
                                 obj.heldObject.Value is SObject held)
                        {
                            // Add directly to itemMap instead of enqueueing tile
                            AddToItemMap(itemMap, held.ParentSheetIndex, quality);

                            // Clear the box/log after collection
                            obj.heldObject.Value = null;
                        }
                        await Task.Delay(Config.CollectionDelay / SafeMultiplier(npcLevel + FarmerSkillLevel));
                    }
                }
            }

            IsNPCCollecting[npcName] = true;

            while (forageQueue.Count > 0)
            {
                // Apply delay BEFORE processing each item
                if (!Game1.newDay)
                {
                    await Task.Delay(Config.CollectionDelay / SafeMultiplier(npcLevel + FarmerSkillLevel));
                }
                else
                {
                    //monitor.Log(T("DayEndedBypassDelay"), LogLevel.Trace);
                }


                var (location, tile, obj) = forageQueue.Dequeue();
                int index = obj.ParentSheetIndex;

                if (!itemMap.TryGetValue(index, out var stacked))
                {
                    stacked = new SObject($"{index}", 1);
                    stacked.Quality = quality;
                    itemMap[index] = stacked;
                }
                else
                {
                    stacked.Stack += 1;
                    stacked.Quality = quality;
                }

                // Only remove if it was a ground forageable
                if (tile != Vector2.Zero)
                    location.Objects.Remove(tile);

                // Deliver if stack reaches 999
                if (itemMap[index].Stack >= 999)
                {
                    var deliveryChunk = new List<Item> { itemMap[index].getOne() };
                    deliveryChunk[0].Stack = 999;

                    itemMap[index].Stack -= 999;

                    DeliverContractsItems(new List<ContractsDelivery>
                    {
                        new ContractsDelivery
                        {
                            Items = deliveryChunk,
                            RecipientID = Game1.player.UniqueMultiplayerID
                        }
                    }, Config);

                    Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                }
            }

            IsNPCCollecting[npcName] = false;

            // Final delivery of remaining items
            var finalItems = itemMap.Values.Where(i => i.Stack > 0).Cast<Item>().ToList();
            onComplete(finalItems);
        }
        private static bool IsForageable(SObject obj)
        {
            return obj.Category == -27 || obj.Category == -79 || obj.Category == -80 || obj.Category == -81;
        }
        public static List<Item> ScanAnimalProductsItems(IMonitor monitor, string npcName)
        {
            var itemMap = new Dictionary<(int index, int quality), SObject>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);

            int[] animalProductIndices = new int[]
            {
                176, 180, 182, 174, 184, 186, 440, 442, 436, 438, 444, 430
            };

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        !obj.bigCraftable.Value &&
                        obj.canBeGrabbed.Value &&
                        animalProductIndices.Contains(obj.ParentSheetIndex))
                    {
                        int index = obj.ParentSheetIndex;
                        var key = (index, quality);

                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject($"{index}", 1);
                            stacked.Quality = quality;
                            itemMap[key] = stacked;
                        }
                        else
                        {
                            stacked.Stack += 1;
                        }
                    }
                }
            }
            monitor.Log(T("ScannedAnimalProducts", itemMap.Values.Sum(i => i.Stack)), LogLevel.Trace);
            return itemMap.Values.Cast<Item>().ToList();
        }
        public static async void GatherAnimalProductsWithRealTimeDelay(IMonitor monitor, string npcName, Action<List<Item>> onComplete)
        {
            var itemMap = new Dictionary<int, SObject>();
            var productQueue = new Queue<(GameLocation location, Vector2 tile, SObject obj)>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);
            int FarmerSkillLevel = Game1.player.farmingLevel.Value;

            int[] animalProductIndices = new int[]
            {
                176, 180, 182, 174, 184, 186, 440, 442, 436, 438, 444, 430
            };

            var allLocations = new List<GameLocation>(Game1.locations);

            var farm = Game1.getLocationFromName("Farm");
            if (farm != null)
            {
                try
                {
                    foreach (var building in ((dynamic)farm).buildings)
                    {
                        if (building.indoors.Value is GameLocation indoor)
                            allLocations.Add(indoor);
                    }
                }
                catch (RuntimeBinderException ex)
                {
                    //monitor.Log(T("FailedFarmBuildings", ex.Message), LogLevel.Warn);
                }
            }

            foreach (GameLocation location in allLocations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        !obj.bigCraftable.Value &&
                        obj.canBeGrabbed.Value &&
                        animalProductIndices.Contains(obj.ParentSheetIndex))
                    {
                        productQueue.Enqueue((location, pair.Key, obj));
                    }
                }
            }

            monitor.Log(T("QueuedAnimalProducts", productQueue.Count), LogLevel.Info);
            IsNPCCollecting[npcName] = true;

            while (productQueue.Count > 0)
            {
                var (location, tile, obj) = productQueue.Dequeue();
                int index = obj.ParentSheetIndex;

                if (!itemMap.TryGetValue(index, out var stacked))
                {
                    stacked = new SObject($"{index}", 1);
                    stacked.Quality = quality;
                    itemMap[index] = stacked;
                }
                else
                {
                    stacked.Stack += 1;
                }

                location.Objects.Remove(tile);

                // Deliver if stack reaches 999
                if (itemMap[index].Stack >= 999)
                {
                    var deliveryChunk = new List<Item> { itemMap[index].getOne() };
                    deliveryChunk[0].Stack = 999;

                    itemMap[index].Stack -= 999;

                    DeliverContractsItems(new List<ContractsDelivery>
                    {
                        new ContractsDelivery
                        {
                            Items = deliveryChunk,
                            RecipientID = Game1.player.UniqueMultiplayerID
                        }
                    }, Config);

                    Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                }

                // Skip delay if day is ending
                if (!Game1.newDay)
                {
                    await Task.Delay(Config.CollectionDelay / SafeMultiplier(npcLevel + FarmerSkillLevel));
                }
                else
                {
                    //monitor.Log(T("DayEndedBypassDelay"), LogLevel.Trace);
                }
            }

            IsNPCCollecting[npcName] = false;

            // Final delivery of remaining items
            var finalItems = itemMap.Values.Where(i => i.Stack > 0).Cast<Item>().ToList();
            onComplete(finalItems);
        }
        public static List<Item> ScanTapperItems(IMonitor monitor, string npcName)
        {
            var itemMap = new Dictionary<(int index, int quality), SObject>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        obj.bigCraftable.Value &&
                        (obj.ParentSheetIndex == 105 || obj.ParentSheetIndex == 264) && // Tapper or Heavy Tapper
                        obj.readyForHarvest.Value &&
                        obj.heldObject.Value is SObject tappedProduct)
                    {
                        int index = tappedProduct.ParentSheetIndex;
                        var key = (index, quality);

                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject($"{index}", tappedProduct.Stack);
                            stacked.Quality = quality;
                            itemMap[key] = stacked;
                        }
                        else
                        {
                            stacked.Stack += tappedProduct.Stack;
                        }
                    }
                }
            }
            return itemMap.Values.Cast<Item>().ToList();
        }
        public static async void GatherTapperItemsWithRealTimeDelay(IMonitor monitor, string npcName, Action<List<Item>> onComplete)
        {
            var itemMap = new Dictionary<int, SObject>();
            var tapperQueue = new Queue<(GameLocation location, Vector2 tile, SObject tappedProduct)>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);
            int FarmerSkillLevel = Game1.player.foragingLevel.Value;

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        obj.bigCraftable.Value &&
                        (obj.ParentSheetIndex == 105 || obj.ParentSheetIndex == 264) && // Tapper or Heavy Tapper
                        obj.readyForHarvest.Value &&
                        obj.heldObject.Value is SObject tappedProduct)
                    {
                        tapperQueue.Enqueue((location, pair.Key, tappedProduct));
                    }
                }
            }

            IsNPCCollecting[npcName] = true;

            while (tapperQueue.Count > 0)
            {
                var (location, tile, tappedProduct) = tapperQueue.Dequeue();
                int index = tappedProduct.ParentSheetIndex;

                if (!itemMap.TryGetValue(index, out var stacked))
                {
                    stacked = new SObject($"{index}", tappedProduct.Stack);
                    stacked.Quality = quality;
                    itemMap[index] = stacked;
                }
                else
                {
                    stacked.Stack += tappedProduct.Stack;
                }

                if (location.Objects[tile] is SObject tapper)
                {
                    tapper.heldObject.Value = null;
                    tapper.readyForHarvest.Value = false;
                }

                // Deliver if stack reaches 999
                if (stacked.Stack >= 999)
                {
                    var deliveryChunk = new List<Item> { stacked.getOne() };
                    deliveryChunk[0].Stack = 999;

                    stacked.Stack -= 999;

                    DeliverContractsItems(new List<ContractsDelivery>
                    {
                        new ContractsDelivery
                        {
                            Items = deliveryChunk,
                            RecipientID = Game1.player.UniqueMultiplayerID
                        }
                    }, Config);

                    Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                }

                // Skip delay if day is ending
                if (!Game1.newDay)
                {
                    await Task.Delay(Config.CollectionDelay / SafeMultiplier(npcLevel + FarmerSkillLevel));
                }
                else
                {
                    //monitor.Log(T("DayEndedBypassDelay"), LogLevel.Trace);
                }
            }

            IsNPCCollecting[npcName] = false;

            // Final delivery of remaining items
            var finalItems = itemMap.Values.Where(i => i.Stack > 0).Cast<Item>().ToList();
            onComplete(finalItems);
        }
        public static async void OfferCrabPotContract(IMonitor monitor, string npcName)
        {
            // Step 1: Scan candidate water tiles (closest first)
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
                        candidateTiles.Add(tile);
                    }
                }
            }

            // Farmer’s tile
            Vector2 farmerTile = new Vector2((int)(Game1.player.Position.X / Game1.tileSize), (int)(Game1.player.Position.Y / Game1.tileSize));

            // Sort by distance from farmer
            candidateTiles = candidateTiles
                .OrderBy(t => Vector2.Distance(farmerTile, t))
                .ToList();

            // Step 2: Count crab pots in inventory
            int potsInInventory = Game1.player.Items
                .OfType<SObject>()
                .Where(i => i.ParentSheetIndex == 710)
                .Sum(i => i.Stack);

            // Step 3: Limit by gold
            int feePerPot = Config.PotSetFee;
            int maxAffordable = Game1.player.Money / feePerPot;

            // Step 4: Final number of pots
            int potsToPlace = Math.Min(candidateTiles.Count, Math.Min(potsInInventory, maxAffordable));

            if (potsToPlace <= 0)
            {
                Game1.showGlobalMessage(T("CheckInventoryCrabPots"));
                return;
            }

            int totalFee = potsToPlace * feePerPot;

            // Step 5: Friendship points (1 per 5 pots, minimum 1)
            int friendshipPoints = Math.Max(1, potsToPlace * feePerPot / 250);

            // Step 6: Build dialogue text

            string dialogText =
                T("PotPlaceOffer", new { npc = npcName, quantity = potsToPlace }) + "\n\n" +
                T("PotPlaceValue", new { Fee = totalFee, PerPot = feePerPot }) + "\n\n" +
                T("ServiceFriendshipReward", new { FriendPoints = friendshipPoints, npc = npcName }) + "\n\n" +
                T("ContractAcceptPrompt");

            Game1.currentLocation.createQuestionDialogue(
                dialogText,
                new Response[]
                {
                    new Response("Yes", T("ResponseYes")),
                    new Response("No", T("ResponseNo"))
                },
                async (farmer, answer) =>
                {
                    if (answer == "Yes")
                    {
                        Game1.player.Money -= totalFee;

                        monitor.Log(T("PotPlaceContractAccept", new { pots = potsToPlace, fee = totalFee }), LogLevel.Info);

                        // Delay scaling by NPC + Farmer skill
                        int npcLevel = UpdateNPCLevel(npcName);
                        int farmerSkill = Game1.player.fishingLevel.Value;
                        int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);

                        IsNPCCollecting[npcName] = true;

                        for (int i = 0; i < potsToPlace; i++)
                        {
                            var tile = candidateTiles[i];

                            var crabPot = new CrabPot();

                            currentLocation.Objects[tile] = crabPot;

                            // Remove one from inventory
                            var item = Game1.player.Items.FirstOrDefault(it => it is SObject obj && obj.ParentSheetIndex == 710);
                            if (item is SObject potItem)
                            {
                                potItem.Stack--;
                                if (potItem.Stack <= 0)
                                    Game1.player.removeItemFromInventory(potItem);
                            }

                            if (!Game1.newDay)
                                await Task.Delay(delay);
                        }

                        IsNPCCollecting[npcName] = false;

                        // Apply friendship reward
                        var npc = Game1.getCharacterFromName(npcName);
                        if (npc != null)
                        {
                            Game1.player.changeFriendship(friendshipPoints, npc);
                            Game1.showGlobalMessage(T("FriendshipIncreased", new { npc = npcName, points = friendshipPoints }));
                        }

                        Game1.showGlobalMessage(T("GlobalPotsPlaced", new { npc = npcName, numberofpots = potsToPlace, fee = totalFee }));
                    }
                    else if (answer == "No")
                    {
                        Game1.showGlobalMessage(T("MaybeLater"));
                    }
            });
        }
        public static async void OfferCrabPotBaitContract(IMonitor monitor, string npcName)
        {
            // 1) Gather all unbaited crab pots globally
            var unbaitedPots = new List<(GameLocation location, Vector2 tile, CrabPot pot)>();

            foreach (GameLocation loc in Game1.locations)
            {
                foreach (var kvp in loc.Objects.Pairs)
                {
                    if (kvp.Value is CrabPot cp)
                    {
                        bool hasCatch = cp.heldObject.Value != null;
                        bool isBaited = cp.bait != null && cp.bait.Value != null;

                        if (!isBaited && !hasCatch)
                            unbaitedPots.Add((loc, kvp.Key, cp));
                    }
                }
            }

            // 2) Count available bait in inventory
            int baitRegular = Game1.player.Items
                .OfType<SObject>()
                .Where(i => i.ParentSheetIndex == 685)
                .Sum(i => i.Stack);

            int feePerPot = Config.BaitFee; // service fee per baited pot (tune as desired)
            int maxAffordable = Game1.player.Money / feePerPot;

            // 4) Final number to bait
            int potsToBait = Math.Min(unbaitedPots.Count, Math.Min(baitRegular, maxAffordable));

            if (potsToBait <= 0)
            {
                Game1.showGlobalMessage(T("NoBaitablePotsOrBaitOrGold"));
                return;
            }

            int totalFee = potsToBait * feePerPot;

            // 5) Friendship: 1 point per 10 baited, min 1
            int friendshipPoints = Math.Max(1, potsToBait * feePerPot / 200);

            // 6) Dialogue
            string dialogText =
                T("BaitContractOffer", new { npc = npcName, quantity = potsToBait }) + "\n\n" +
                T("BaitValue", new { Fee = totalFee, PerPot = feePerPot }) + "\n\n" +
                T("ServiceFriendshipReward", new { FriendPoints = friendshipPoints, npc = npcName }) + "\n\n" +
                T("ContractAcceptPrompt");

            Game1.currentLocation.createQuestionDialogue(
                dialogText,
                new Response[]
                {
                    new Response("Yes", T("ResponseYes")),
                    new Response("No", T("ResponseNo"))
                },
                async (farmer, answer) =>
                {
                    if (answer != "Yes")
                    {
                        Game1.showGlobalMessage(T("MaybeLater"));
                        return;
                    }

                    // Charge fee
                    Game1.player.Money -= totalFee;

                    int npcLevel = UpdateNPCLevel(npcName);
                    int farmerSkill = Game1.player.fishingLevel.Value;
                    int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);

                    // 7) Baiting loop: consume bait and apply to pots
                    IsNPCCollecting[npcName] = true;

                    int baitedCount = 0;

                    for (int i = 0; i < unbaitedPots.Count && baitedCount < potsToBait; i++)
                    {
                        var (location, tile, pot) = unbaitedPots[i];

                        SObject? baitItem = TakeBaitFromInventory();
                        if (baitItem == null)
                            break; // ran out mid-process

                        // Use the machine's drop-in path to ensure correctness
                        bool accepted = pot.performObjectDropInAction(baitItem, false, Game1.player);
                        if (!accepted)
                        {
                            // Fallback: directly assign bait if drop-in failed
                            pot.bait.Value = baitItem;
                        }

                        baitedCount++;

                        if (!Game1.newDay && delay > 0)
                            await Task.Delay(delay);
                    }

                    IsNPCCollecting[npcName] = false;

                    // Friendship reward
                    var npc = Game1.getCharacterFromName(npcName);
                    if (npc != null)
                    {
                        Game1.player.changeFriendship(friendshipPoints, npc);
                        Game1.showGlobalMessage(T("FriendshipIncreased", new { npc = npcName, points = friendshipPoints }));
                    }

                    Game1.showGlobalMessage(T("BaitingServiceComplete", new { npc = npcName, count = baitedCount,  fee = totalFee }));
                }
            );

            // Local helper: take one bait item from inventory)
            SObject? TakeBaitFromInventory()
            {
                var reg = Game1.player.Items.FirstOrDefault(it => it is SObject o && o.ParentSheetIndex == 685) as SObject;
                if (reg != null)
                {
                    reg.Stack--;
                    if (reg.Stack <= 0)
                        Game1.player.removeItemFromInventory(reg);
                    return new SObject("685", 1);
                }

                return null;
            }
        }
        public static List<Item> ScanProducerItems(IMonitor monitor, string npcName)
        {
            var itemMap = new Dictionary<(int index, int quality), SObject>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        obj.bigCraftable.Value &&
                        obj.readyForHarvest.Value &&
                        obj.heldObject.Value is SObject product)
                    {
                        int index = product.ParentSheetIndex;
                        var key = (index, quality);

                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject($"{index}", product.Stack);
                            stacked.Quality = quality;
                            itemMap[key] = stacked;
                        }
                        else
                        {
                            stacked.Stack += product.Stack;
                        }
                    }
                }
            }

            return itemMap.Values.Cast<Item>().ToList();
        }
        public static async void GatherProducerItemsWithRealTimeDelay(
    IMonitor monitor, string npcName, Action<List<Item>> onComplete)
        {
            var itemMap = new Dictionary<string, SObject>();
            var producerQueue = new Queue<(GameLocation location, Vector2 tile, SObject product)>();
            int npcLevel = UpdateNPCLevel(npcName);
            int quality = GetQuality(npcLevel);
            int FarmerSkillLevel = Game1.player.foragingLevel.Value;

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        obj.bigCraftable.Value &&
                        obj.readyForHarvest.Value &&
                        obj.heldObject.Value is SObject product)
                    {
                        producerQueue.Enqueue((location, pair.Key, product));
                    }
                }
            }

            IsNPCCollecting[npcName] = true;

            while (producerQueue.Count > 0)
            {
                var (location, tile, product) = producerQueue.Dequeue();

                // Use string ID if available, else fall back to numeric
                string id = !string.IsNullOrEmpty(product.ItemId)
                    ? product.ItemId
                    : product.ParentSheetIndex.ToString();

                if (!itemMap.TryGetValue(id, out var stacked))
                {
                    stacked = new SObject(id, product.Stack); // constructor can take string ID
                    stacked.Quality = quality;
                    itemMap[id] = stacked;
                }
                else
                {
                    stacked.Stack += product.Stack;
                }

                // Clear producer so it can start again
                if (location.Objects[tile] is SObject producer)
                {
                    producer.heldObject.Value = null;
                    producer.readyForHarvest.Value = false;
                }

                // Deliver in chunks
                if (stacked.Stack >= 999)
                {
                    var deliveryChunk = new List<Item> { stacked.getOne() };
                    deliveryChunk[0].Stack = 999;
                    stacked.Stack -= 999;

                    DeliverContractsItems(new List<ContractsDelivery>
                    {
                        new ContractsDelivery
                        {
                            Items = deliveryChunk,
                            RecipientID = Game1.player.UniqueMultiplayerID
                        }
                    }, Config);

                    Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                }

                // Delay pacing
                if (!Game1.newDay)
                {
                    await Task.Delay(Config.CollectionDelay / SafeMultiplier(npcLevel + FarmerSkillLevel));
                }
                else
                {
                    //monitor.Log(T("DayEndedBypassDelay"), LogLevel.Trace);
                }
            }

            IsNPCCollecting[npcName] = false;

            var finalItems = itemMap.Values.Where(i => i.Stack > 0).Cast<Item>().ToList();
            onComplete(finalItems);
        }
        private static void ProcessNPCCollection(
            IMonitor monitor,
            List<Item> items,
            ServiceId ItemTypeName,
            string NewNPCName,
            Action<List<Item>> onConfirmed)
        {
            int totalValue = items.Sum(i => Math.Max(1, i.sellToStorePrice()) * i.Stack);
            int totalItemCount = items.Sum(i => i.Stack);
            float feeRate = GetSeviceContractPercents(ItemTypeName);
            int totalFee = (int)Math.Round(totalValue * feeRate / 100);
            int friendshipAdd = (int)Math.Round(2f + 18f * Math.Pow(feeRate / 100f, 0.75));
            int npcLevel = UpdateNPCLevel(NewNPCName);
            int quality = GetQuality(npcLevel);
            string qualityName = GetQualityName(quality);
            var serviceLabels = SpecialtyNames.ContainsKey(ItemTypeName)
                ? SpecialtyNames[ItemTypeName]
                : T("ServiceWeeds");

            if (items.Count > 0)
            {
                if (Game1.player.Money >= totalFee)
                {
                    string dialogText =
                        T("ServiceOffer", new { npc = NewNPCName, quantity = totalItemCount, item = serviceLabels }) + "\n\n" +
                        T("ServiceFee", new { Fee = totalFee, feepercent = feeRate }) + "\n\n" +
                        T("ServiceFriendshipReward", new { FriendPoints = friendshipAdd, npc = NewNPCName }) + "\n\n" +
                        T("ContractAcceptPrompt");

                    Game1.currentLocation.createQuestionDialogue(
                        dialogText,
                        new Response[]
                        {
                            new Response("Yes", T("ResponseYes")),
                            new Response("No", T("ResponseNo"))
                        },
                        async (farmer, answer) =>
                        {
                            if (answer == "Yes")
                            {
                                Game1.player.Money -= totalFee;
                                // Collecting summary
                                Game1.showGlobalMessage(T("NPCCollectingItems", new
                                {
                                    npc = NewNPCName,
                                    count = totalItemCount,
                                    item = GetServiceTypeLabel(ItemTypeName),
                                    value = totalValue,
                                    fee = totalFee,
                                    rate = feeRate
                                }
                                ));

                                var npc = Game1.getCharacterFromName(NewNPCName);
                                if (npc != null)
                                {
                                    Game1.player.changeFriendship((int)friendshipAdd, npc);
                                    // Friendship increase
                                    Game1.showGlobalMessage(T("FriendshipIncreased", new
                                    {
                                        npc = NewNPCName,
                                        points = (int)friendshipAdd
                                    }
                                    ));
                                }

                                onConfirmed(items); // Proceed to actual collection
                            }
                            else if (answer == "No")
                            {
                                Game1.showGlobalMessage(T("MaybeLater"));
                            }
                        }
                    );
                }
                else
                {
                    Game1.showRedMessage(T("NotEnoughGold", totalFee));
                    IsCollectingService[ItemTypeName] = false;
                }
            }
            else
            {
                // Not enough items found
                Game1.showGlobalMessage(T("NPCFoundNoItems", new
                {
                    npc = NewNPCName,
                    item = GetServiceTypeLabel(ItemTypeName)
                }
                ));

                IsCollectingService[ItemTypeName] = false;
            }
            
        }
        public static void NPCService(string NewNPCName, ServiceId serviceId, IMonitor monitor)
        {
            if (serviceId == ServiceId.CrabPots)
            {
                if (IsCollectingService.TryGetValue(serviceId, out bool isRunning) && isRunning)
                {
                    Game1.showRedMessage(T("ServiceInProgress", new { service = serviceId.ToString() }));

                    return;
                }

                IsCollectingService[serviceId] = true;

                string CrabNPCName = NewNPCName;
                var previewItems = ScanCrabPotItems(monitor, CrabNPCName);

                ProcessNPCCollection(monitor, previewItems, serviceId, CrabNPCName, confirmed =>
                {
                    Game1.delayedActions.Add(new DelayedAction(100, () =>
                    {
                        GatherCrabPotItemsWithDelay(monitor, CrabNPCName, collected =>
                        {
                            DeliverContractsItems(new List<ContractsDelivery>
                            {
                                new ContractsDelivery
                                {
                                    Items = collected,
                                    RecipientID = Game1.player.UniqueMultiplayerID
                                }
                            }, Config);

                            string summary = string.Join(", ", collected.Select(i => $"{i.Stack} {i.DisplayName}"));

                            IsCollectingService[serviceId] = false; // Reset flag
                        });
                    }));
                });
            }

            if (serviceId == ServiceId.SetCrabPots)
            {
                if (IsCollectingService.TryGetValue(serviceId, out bool isRunning) && isRunning)
                {
                    Game1.showRedMessage(T("ServiceInProgress", new { service = serviceId.ToString() }));

                    return;
                }

                string SetCrabNPCName = NewNPCName;

                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    OfferCrabPotContract(monitor, NewNPCName);
                }));
        
                IsCollectingService[serviceId] = false; // Reset flag
            }

            if (serviceId == ServiceId.BaitCrabPots)
            {
                // Prevent overlap if a baiting run is already in progress
                if (IsCollectingService.TryGetValue(serviceId, out bool isRunning) && isRunning)
                {
                    Game1.showRedMessage(T("ServiceInProgress", new { service = serviceId.ToString() }));
                    return;
                }

                string npcName = NewNPCName;

                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    OfferCrabPotBaitContract(Instance.Monitor, npcName);
                }));

                IsCollectingService[serviceId] = false; // reset flag
            }

            if (serviceId == ServiceId.Honey)
            {
                if (IsCollectingService.TryGetValue(serviceId, out bool isRunning) && isRunning)
                {
                    Game1.showRedMessage(T("ServiceInProgress", new { service = serviceId.ToString() }));

                    return;
                }

                IsCollectingService[serviceId] = true;

                string HoneyNPCName = NewNPCName;
                var previewItems = ScanHoneyItems(monitor, HoneyNPCName);

                ProcessNPCCollection(monitor, previewItems, serviceId, HoneyNPCName, confirmed =>
                {
                    Game1.delayedActions.Add(new DelayedAction(100, () =>
                    {
                        GatherBeeHouseHoneyWithRealTimeDelay(monitor, HoneyNPCName, collected =>
                        {
                            DeliverContractsItems(new List<ContractsDelivery>
                            {
                                new ContractsDelivery
                                {
                                    Items = collected,
                                    RecipientID = Game1.player.UniqueMultiplayerID
                                }
                            }, Config);

                            string summary = string.Join(", ", collected.Select(i => $"{i.Stack} {i.DisplayName}"));

                            IsCollectingService[serviceId] = false; // Reset flag
                        });
                    }));
                });
            }

            if (serviceId == ServiceId.Crops)
            {
                if (IsCollectingService.TryGetValue(serviceId, out bool isRunning) && isRunning)
                {
                    Game1.showRedMessage(T("ServiceInProgress", new { service = serviceId.ToString() }));

                    return;
                }

                IsCollectingService[serviceId] = true;

                string CropsNPCName = NewNPCName;
                var previewItems = ScanFarmCrops(monitor, CropsNPCName);

                ProcessNPCCollection(monitor, previewItems, serviceId, CropsNPCName, confirmed =>
                {
                    Game1.delayedActions.Add(new DelayedAction(100, () =>
                    {
                        GatherFarmCropsWithRealTimeDelay(monitor, CropsNPCName, collected =>
                        {
                            DeliverContractsItems(new List<ContractsDelivery>
                {
                    new ContractsDelivery
                    {
                        Items = collected,
                        RecipientID = Game1.player.UniqueMultiplayerID
                    }
                }, Config);

                            string summary = string.Join(", ", collected.Select(i => $"{i.Stack} {i.DisplayName}"));

                            IsCollectingService[serviceId] = false; // Reset flag
                        });
                    }));
                });
            }

            if (serviceId == ServiceId.Hardwood)
            {
                if (IsCollectingService.TryGetValue(serviceId, out bool isRunning) && isRunning)
                {
                    Game1.showRedMessage(T("ServiceInProgress", new { service = serviceId.ToString() }));

                    return;
                }

                IsCollectingService[serviceId] = true;

                string HardwoodNPCName = NewNPCName;
                var previewItems = ScanHardwoodItems(monitor, HardwoodNPCName);

                ProcessNPCCollection(monitor, previewItems, serviceId, HardwoodNPCName, confirmed =>
                {
                    Game1.delayedActions.Add(new DelayedAction(100, () =>
                    {
                        GatherHardwoodWithRealTimeDelay(monitor, HardwoodNPCName, collected =>
                        {
                            DeliverContractsItems(new List<ContractsDelivery>
                {
                    new ContractsDelivery
                    {
                        Items = collected,
                        RecipientID = Game1.player.UniqueMultiplayerID
                    }
                }, Config);

                            string summary = string.Join(", ", collected.Select(i => $"{i.Stack} {i.DisplayName}"));

                            IsCollectingService[serviceId] = false; // Reset flag
                        });
                    }));
                });
            }

            if (serviceId == ServiceId.Wood)
            {
                if (IsCollectingService.TryGetValue(serviceId, out bool isRunning) && isRunning)
                {
                    Game1.showRedMessage(T("ServiceInProgress", new { service = serviceId.ToString() }));

                    return;
                }

                IsCollectingService[serviceId] = true;

                string WoodNPCName = NewNPCName;
                var previewItems = ScanWoodItems(monitor, WoodNPCName);

                ProcessNPCCollection(monitor, previewItems, serviceId, WoodNPCName, confirmed =>
                {
                    Game1.delayedActions.Add(new DelayedAction(100, () =>
                    {
                        GatherWoodWithRealTimeDelay(monitor, WoodNPCName, collected =>
                        {
                            DeliverContractsItems(new List<ContractsDelivery>
                {
                    new ContractsDelivery
                    {
                        Items = collected,
                        RecipientID = Game1.player.UniqueMultiplayerID
                    }
                }, Config);

                            string summary = string.Join(", ", collected.Select(i => $"{i.Stack} {i.DisplayName}"));

                            IsCollectingService[serviceId] = false; // Reset flag
                        });
                    }));
                });
            }

            if (serviceId == ServiceId.Forageables)
            {
                if (IsCollectingService.TryGetValue(serviceId, out bool isRunning) && isRunning)
                {
                    Game1.showRedMessage(T("ServiceInProgress", new { service = serviceId.ToString() }));

                    return;
                }

                IsCollectingService[serviceId] = true;

                string ForageablesNPCName = NewNPCName;
                var previewItems = ScanForageablesItems(monitor, ForageablesNPCName);

                ProcessNPCCollection(monitor, previewItems, serviceId, ForageablesNPCName, confirmed =>
                {
                    Game1.delayedActions.Add(new DelayedAction(100, () =>
                    {
                        GatherForageablesWithRealTimeDelay(monitor, ForageablesNPCName, collected =>
                        {
                            DeliverContractsItems(new List<ContractsDelivery>
                {
                    new ContractsDelivery
                    {
                        Items = collected,
                        RecipientID = Game1.player.UniqueMultiplayerID
                    }
                }, Config);

                            string summary = string.Join(", ", collected.Select(i => $"{i.Stack} {i.DisplayName}"));

                            IsCollectingService[serviceId] = false; // Reset flag
                        });
                    }));
                });
            }

            if (serviceId == ServiceId.Stone)
            {
                if (IsCollectingService.TryGetValue(serviceId, out bool isRunning) && isRunning)
                {
                    Game1.showRedMessage(T("ServiceInProgress", new { service = serviceId.ToString() }));

                    return;
                }

                IsCollectingService[serviceId] = true;

                string StoneNPCName = NewNPCName;
                var previewItems = ScanStoneItems(monitor, StoneNPCName);

                ProcessNPCCollection(monitor, previewItems, serviceId, StoneNPCName, confirmed =>
                {
                    Game1.delayedActions.Add(new DelayedAction(100, () =>
                    {
                        GatherStoneWithRealTimeDelay(monitor, StoneNPCName, collected =>
                        {
                            DeliverContractsItems(new List<ContractsDelivery>
                {
                    new ContractsDelivery
                    {
                        Items = collected,
                        RecipientID = Game1.player.UniqueMultiplayerID
                    }
                }, Config);

                            string summary = string.Join(", ", collected.Select(i => $"{i.Stack} {i.DisplayName}"));

                            IsCollectingService[serviceId] = false; // Reset flag
                        });
                    }));
                });
            }

            if (serviceId == ServiceId.Weeds)
            {
                if (IsCollectingService.TryGetValue(serviceId, out bool isRunning) && isRunning)
                {
                    Game1.showRedMessage(T("ServiceInProgress", new { service = serviceId.ToString() }));

                    return;
                }

                IsCollectingService[serviceId] = true;

                string WeedsNPCName = NewNPCName;
                var previewItems = ScanWeedsItems(monitor, WeedsNPCName);

                ProcessNPCCollection(monitor, previewItems, serviceId, WeedsNPCName, confirmed =>
                {
                    Game1.delayedActions.Add(new DelayedAction(100, () =>
                    {
                        GatherWeedsWithRealTimeDelay(monitor, WeedsNPCName, collected =>
                        {
                            DeliverContractsItems(new List<ContractsDelivery>
                            {
                                new ContractsDelivery
                                {
                                    Items = collected,
                                    RecipientID = Game1.player.UniqueMultiplayerID
                                }
                            }, Config);

                            string summary = string.Join(", ", collected.Select(i => $"{i.Stack} {i.DisplayName}"));

                            IsCollectingService[serviceId] = false; // Reset flag
                        });
                    }));
                });
            }

            if (serviceId == ServiceId.Animals)
            {
                if (IsCollectingService.TryGetValue(serviceId, out bool isRunning) && isRunning)
                {
                    Game1.showRedMessage(T("ServiceInProgress", new { service = serviceId.ToString() }));

                    return;
                }

                IsCollectingService[serviceId] = true;

                string AnimalProductsNPCName = NewNPCName;
                var previewItems = ScanAnimalProductsItems(monitor, AnimalProductsNPCName);

                ProcessNPCCollection(monitor, previewItems, serviceId, AnimalProductsNPCName, confirmed =>
                {
                    Game1.delayedActions.Add(new DelayedAction(100, () =>
                    {
                        GatherAnimalProductsWithRealTimeDelay(monitor, AnimalProductsNPCName, collected =>
                        {
                            DeliverContractsItems(new List<ContractsDelivery>
                {
                    new ContractsDelivery
                    {
                        Items = collected,
                        RecipientID = Game1.player.UniqueMultiplayerID
                    }
                }, Config);

                            string summary = string.Join(", ", collected.Select(i => $"{i.Stack} {i.DisplayName}"));

                            IsCollectingService[serviceId] = false; // Reset flag
                        });
                    }));
                });
            }

            if (serviceId == ServiceId.Tappers)
            {
                if (IsCollectingService.TryGetValue(serviceId, out bool isRunning) && isRunning)
                {
                    Game1.showRedMessage(T("ServiceInProgress", new { service = serviceId.ToString() }));

                    return;
                }

                IsCollectingService[serviceId] = true;

                string TappersNPCName = NewNPCName;
                var previewItems = ScanTapperItems(monitor, TappersNPCName);

                ProcessNPCCollection(monitor, previewItems, serviceId, TappersNPCName, confirmed =>
                {
                    Game1.delayedActions.Add(new DelayedAction(100, () =>
                    {
                        GatherTapperItemsWithRealTimeDelay(monitor, TappersNPCName, collected =>
                        {
                            DeliverContractsItems(new List<ContractsDelivery>
                            {
                                new ContractsDelivery
                                {
                                    Items = collected,
                                    RecipientID = Game1.player.UniqueMultiplayerID
                                }
                            }, Config);

                            string summary = string.Join(", ", collected.Select(i => $"{i.Stack} {i.DisplayName}"));

                            IsCollectingService[serviceId] = false; // Reset flag
                        });
                    }));
                });
            }

            if (serviceId == ServiceId.Producers)
            {
                if (IsCollectingService.TryGetValue(serviceId, out bool isRunning) && isRunning)
                {
                    Game1.showRedMessage(T("ServiceInProgress", new { service = serviceId.ToString() }));

                    return;
                }

                IsCollectingService[serviceId] = true;

                string ProducersNPCName = NewNPCName;
                var previewItems = ScanTapperItems(monitor, ProducersNPCName);

                ProcessNPCCollection(monitor, previewItems, serviceId, ProducersNPCName, confirmed =>
                {
                    Game1.delayedActions.Add(new DelayedAction(100, () =>
                    {
                        GatherProducerItemsWithRealTimeDelay(monitor, ProducersNPCName, collected =>
                        {
                            DeliverContractsItems(new List<ContractsDelivery>
                            {
                                new ContractsDelivery
                                {
                                    Items = collected,
                                    RecipientID = Game1.player.UniqueMultiplayerID
                                }
                            }, Config);

                            string summary = string.Join(", ", collected.Select(i => $"{i.Stack} {i.DisplayName}"));

                            IsCollectingService[serviceId] = false; // Reset flag
                        });
                    }));
                });
            }
        }
    }
}
