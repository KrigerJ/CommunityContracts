using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.Objects;
using static CommunityContracts.Core.NPCServiceMenu;

public class ModConfig
{
    public SButton CheatMenuHotkey { get; set; } = SButton.None;
    public SButton DeliveryLocationHotkey { get; set; } = SButton.None;
    public string DeliveryChestColor { get; set; } = "White";
    public string HighlightColor { get; set; } = "Yellow";
    public string FontColor { get; set; } = "Black";
    public bool EnableContractTooltip { get; set; } = true;
    public bool EnableCropService { get; set; } = false;
    public bool EnableProcessTimeReduction { get; set; } = false;
    public bool EnableAnimalsService { get; set; } = false;
    public bool EnableProducersService { get; set; } = false;
    public Dictionary<string, int> NPCContractPercents { get; set; } = new()
    {
        { "Basic", 80 },
        { "Custom", 70 },
    };
    public Dictionary<ServiceId, int> SeviceContractPercents { get; set; } = new()
    {
        { ServiceId.Animals, 5 },
        { ServiceId.CrabPots, 5 },
        { ServiceId.Crops, 5 },
        { ServiceId.Forageables, 5 },
        { ServiceId.Hardwood, 20 },
        { ServiceId.Honey, 5 },
        { ServiceId.Stone, 20 },
        { ServiceId.Weeds, 20 },
        { ServiceId.Wood, 20 },
        { ServiceId.Tappers, 5 },
        { ServiceId.Producers, 5 }
    };
    public int BaitFee { get; set; } = 3;
    public int PotSetFee { get; set; } = 50;
    public int CollectionDelay { get; set; } = 2000;
    public Dictionary<string, int> NPCMinQuantity { get; set; } = new()
    {
        { "Basic", 1 },
        { "Abigail", 1 },
        { "Alex", 1 },
        { "Caroline",4 },
        { "Demetrius", 3 },
        { "Elliott", 2 },
        { "Emily", 3 },
        { "Evelyn", 3 },
        { "George", 5 },
        { "Haley", 3 },
        { "Jas", 3 },
        { "Jodi", 2 },
        { "Leah", 4 },
        { "Leo", 6 },
        { "Linus", 4 },
        { "Maru", 3 },
        { "Pam", 3 },
        { "Penny", 4 },
        { "Sam", 3 },
        { "Sandy", 3 },
        { "Sebastian", 2 },
        { "Shane", 3 },
        { "Vincent", 4 },
        { "Wizard", 2 }
    };
    public string DropLocationName { get; set; } = "Mailbox Back";
    public int DropTileX { get; set; } = 68;
    public int DropTileY { get; set; } = 15;
    public Dictionary<string, Vector2> PresetLocations { get; set; } = new()
    {
        { "MailboxBack", new Vector2(68, 15) },
        { "MailboxBack1", new Vector2(68, 14) },
        { "MailboxBack2", new Vector2(68, 13) },
        { "MailboxBack3", new Vector2(68, 12) },
        { "PorchWoodpile", new Vector2(59, 15) }
    };
    public Dictionary<string, Color> ChestColors { get; set; } = new()
    {
        { "White", Color.White },
        { "Sky Blue", Color.LightSkyBlue },
        { "Gold", Color.Gold },
        { "Green", Color.LightGreen },
        { "Purple", Color.MediumPurple },
        { "Orange", Color.Orange },
        { "Pink", Color.Pink }
    };
    public Dictionary<string, Color> HighlightColors { get; set; } = new()
    {
        { "Yellow", Color.Yellow * 0.75f },
        { "Red", Color.Red * 0.6f },
        { "Green", Color.LimeGreen * 0.6f },
        { "Blue", Color.DeepSkyBlue * 0.6f },
        { "White", Color.White * 0.5f },
        { "Purple", Color.MediumPurple * 0.6f }
    };
    public Dictionary<string, Color> FontColors { get; set; } = new()
    {
        { "Black", Color.Black },
        { "White", Color.White },
        { "Yellow", Color.Yellow },
        { "Red", Color.Red },
        { "Green", Color.LimeGreen },
        { "Blue", Color.DeepSkyBlue },
        { "Purple", Color.MediumPurple }
    };
}
