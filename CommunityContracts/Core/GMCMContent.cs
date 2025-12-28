using StardewModdingAPI;
using GenericModConfigMenu;
using static ModEntry;

namespace CommunityContracts.Core
{
    internal class GMCMContent
    {
        public static void Register(IModHelper helper, IManifest manifest, IMonitor monitor, ModConfig config)
        {
            var gmcm = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm == null)
                return;

            gmcm.Register(
                mod: manifest,
                reset: () => config = new ModConfig(),
                save: () => helper.WriteConfig(config)
            );

            gmcm.AddSectionTitle(manifest, () => T("HotkeySettings"));

            gmcm.AddKeybind(
                mod: manifest,
                getValue: () => config.DeliveryLocationHotkey,
                setValue: value => config.DeliveryLocationHotkey = value,
                name: () => T("SetDeliveryLocationHotkey"),
                tooltip: () => T("SetDeliveryLocationHotkeyTooltip")
            );

            gmcm.AddKeybind(
                mod: manifest,
                getValue: () => config.CheatMenuHotkey,
                setValue: value => config.CheatMenuHotkey = value,
                name: () => T("ShortcutMenuHotkey"),
                tooltip: () => T("ShortcutMenuHotkeyTooltip")
            );

            gmcm.AddSectionTitle(manifest, () => T("DeliveryChestSettings"));

            // Delivery Chest Color
            gmcm.AddTextOption(
                mod: manifest,
                getValue: () => config.DeliveryChestColor,
                setValue: value => config.DeliveryChestColor = value,
                name: () => T("ChestColor"),
                tooltip: () => T("ChestColorTooltip"),
                allowedValues: config.ChestColors.Keys
                    .Select(key => T("ChestColor" + key.Replace(" ", "")))
                    .ToArray()
            );

            // Highlight Color
            gmcm.AddTextOption(
                mod: manifest,
                getValue: () => config.HighlightColor,
                setValue: value => config.HighlightColor = value,
                name: () => T("HighlightColor"),
                tooltip: () => T("HighlightColorTooltip"),
                allowedValues: config.HighlightColors.Keys
                    .Select(key => T("HighlightColor" + key.Replace(" ", "")))
                    .ToArray()
            );

            // Font Color
            gmcm.AddTextOption(
                mod: manifest,
                getValue: () => config.FontColor,
                setValue: value => config.FontColor = value,
                name: () => T("FontColor"),
                tooltip: () => T("FontColorTooltip"),
                allowedValues: config.FontColors.Keys
                    .Select(key => T("FontColor" + key))
                    .ToArray()
            );

            gmcm.AddSectionTitle(manifest, () => T("ServiceSettings"));


            gmcm.AddBoolOption(
                mod: manifest,
                name: () => T("EnableProcessTimeReduction"),
                tooltip: () => T("EnableProcessTimeReductionTooltip"),
                getValue: () => config.EnableProcessTimeReduction,
                setValue: value => config.EnableProcessTimeReduction = value
            );

            gmcm.AddBoolOption(
                mod: manifest,
                name: () => T("EnableCropCollection"),
                tooltip: () => T("EnableCropCollectionTooltip"),
                getValue: () => config.EnableCropService,
                setValue: value => config.EnableCropService = value
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.PotSetFee,
                setValue: value => config.PotSetFee = value,
                name: () => T("PotSetFee"),
                tooltip: () => T("PotSetFeeToolTip"),
                min: 10,
                max: 1000,
                interval: 10
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.BaitFee,
                setValue: value => config.BaitFee = value,
                name: () => T("BaitFee"),
                tooltip: () => T("BaitFeeToolTip"),
                min: 1,
                max: 25,
                interval: 1
            );

            gmcm.AddSectionTitle(manifest, () => T("CharacterSettings"));

            gmcm.AddBoolOption(
                mod: manifest,
                getValue: () => config.EnableContractTooltip,
                setValue: value => config.EnableContractTooltip = value,
                name: () => T("ShowContractTooltip"),
                tooltip: () => T("ShowContractTooltipTooltip")
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.CollectionDelay,
                setValue: value => config.CollectionDelay = value,
                name: () => T("CollectionDelay"),
                tooltip: () => T("CollectionDelayTooltip"),
                min: 100,
                max: 3000,
                interval: 100
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCContractPercents["Basic"],
                setValue: value => config.NPCContractPercents["Basic"] = value,
                name: () => T("ContractPercentBasic"),
                tooltip: () => T("ContractPercentBasicTooltip"),
                min: 5,
                max: 95,
                interval: 5
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCContractPercents["Custom"],
                setValue: value => config.NPCContractPercents["Custom"] = value,
                name: () => T("ContractPercentCustom"),
                tooltip: () => T("ContractPercentCustomTooltip"),
                min: 5,
                max: 95,
                interval: 5
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Basic"],
                setValue: value => config.NPCMinQuantity["Basic"] = value,
                name: () => T("NPCMinQuantity"),
                tooltip: () => T("NPCMinQuantityTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Abigail"],
                setValue: value => config.NPCMinQuantity["Abigail"] = value,
                name: () => T("NPCMinQuantityAbigail"),
                tooltip: () => T("NPCMinQuantityAbigailTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Alex"],
                setValue: value => config.NPCMinQuantity["Alex"] = value,
                name: () => T("NPCMinQuantityAlex"),
                tooltip: () => T("NPCMinQuantityAlexTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Caroline"],
                setValue: value => config.NPCMinQuantity["Caroline"] = value,
                name: () => T("NPCMinQuantityCaroline"),
                tooltip: () => T("NPCMinQuantityCarolineTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Demetrius"],
                setValue: value => config.NPCMinQuantity["Demetrius"] = value,
                name: () => T("NPCMinQuantityDemetrius"),
                tooltip: () => T("NPCMinQuantityDemetriusTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Elliott"],
                setValue: value => config.NPCMinQuantity["Elliott"] = value,
                name: () => T("NPCMinQuantityElliott"),
                tooltip: () => T("NPCMinQuantityElliottTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Emily"],
                setValue: value => config.NPCMinQuantity["Emily"] = value,
                name: () => T("NPCMinQuantityEmily"),
                tooltip: () => T("NPCMinQuantityEmilyTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Evelyn"],
                setValue: value => config.NPCMinQuantity["Evelyn"] = value,
                name: () => T("NPCMinQuantityEvelyn"),
                tooltip: () => T("NPCMinQuantityEvelynTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["George"],
                setValue: value => config.NPCMinQuantity["George"] = value,
                name: () => T("NPCMinQuantityGeorge"),
                tooltip: () => T("NPCMinQuantityGeorgeTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Haley"],
                setValue: value => config.NPCMinQuantity["Haley"] = value,
                name: () => T("NPCMinQuantityHaley"),
                tooltip: () => T("NPCMinQuantityHaleyTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Jas"],
                setValue: value => config.NPCMinQuantity["Jas"] = value,
                name: () => T("NPCMinQuantityJas"),
                tooltip: () => T("NPCMinQuantityJasTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Jodi"],
                setValue: value => config.NPCMinQuantity["Jodi"] = value,
                name: () => T("NPCMinQuantityJodi"),
                tooltip: () => T("NPCMinQuantityJodiTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Leah"],
                setValue: value => config.NPCMinQuantity["Leah"] = value,
                name: () => T("NPCMinQuantityLeah"),
                tooltip: () => T("NPCMinQuantityLeahTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Leo"],
                setValue: value => config.NPCMinQuantity["Leo"] = value,
                name: () => T("NPCMinQuantityLeo"),
                tooltip: () => T("NPCMinQuantityLeoTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Linus"],
                setValue: value => config.NPCMinQuantity["Linus"] = value,
                name: () => T("NPCMinQuantityLinus"),
                tooltip: () => T("NPCMinQuantityLinusTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Maru"],
                setValue: value => config.NPCMinQuantity["Maru"] = value,
                name: () => T("NPCMinQuantityMaru"),
                tooltip: () => T("NPCMinQuantityMaruTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Pam"],
                setValue: value => config.NPCMinQuantity["Pam"] = value,
                name: () => T("NPCMinQuantityPam"),
                tooltip: () => T("NPCMinQuantityPamTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Penny"],
                setValue: value => config.NPCMinQuantity["Penny"] = value,
                name: () => T("NPCMinQuantityPenny"),
                tooltip: () => T("NPCMinQuantityPennyTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Sam"],
                setValue: value => config.NPCMinQuantity["Sam"] = value,
                name: () => T("NPCMinQuantitySam"),
                tooltip: () => T("NPCMinQuantitySamTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Sandy"],
                setValue: value => config.NPCMinQuantity["Sandy"] = value,
                name: () => T("NPCMinQuantitySandy"),
                tooltip: () => T("NPCMinQuantitySandyTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Sebastian"],
                setValue: value => config.NPCMinQuantity["Sebastian"] = value,
                name: () => T("NPCMinQuantitySebastian"),
                tooltip: () => T("NPCMinQuantitySebastianTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Shane"],
                setValue: value => config.NPCMinQuantity["Shane"] = value,
                name: () => T("NPCMinQuantityShane"),
                tooltip: () => T("NPCMinQuantityShaneTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Vincent"],
                setValue: value => config.NPCMinQuantity["Vincent"] = value,
                name: () => T("NPCMinQuantityVincent"),
                tooltip: () => T("NPCMinQuantityVincentTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );

            gmcm.AddNumberOption(
                mod: manifest,
                getValue: () => config.NPCMinQuantity["Wizard"],
                setValue: value => config.NPCMinQuantity["Wizard"] = value,
                name: () => T("NPCMinQuantityWizard"),
                tooltip: () => T("NPCMinQuantityWizardTooltip"),
                min: 1,
                max: 20,
                interval: 1
            );
        }
    }
}
