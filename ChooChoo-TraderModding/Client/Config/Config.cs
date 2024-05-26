using BepInEx.Configuration;
using System;
using UnityEngine;

namespace ChooChooTraderModding.Config
{
    internal static class TraderModdingConfig
    {
        private const string GeneralSectionTitle = "1. General";
        public static ConfigEntry<bool> DefaultToTraderOnly;
        public static ConfigEntry<bool> ColorBorders;
        public static ConfigEntry<bool> ShowAttachedItems;

        private const string TraderPricesTitle = "2. Price Tags";
        public static ConfigEntry<bool> ShowPriceTags;
        public static ConfigEntry<bool> ShowFleaPriceTags;
        public static ConfigEntry<bool> ShowPriceTagsOnWeaponItems;
        public static ConfigEntry<bool> AbbreviatePrices;

        private const string BuildCostTitle = "3. Build Cost";
        public static ConfigEntry<bool> ShowBuildCost;
        public static ConfigEntry<int> BuildCostFontSize;

        private const string HighlightSectionTitle = "4. Highlight Items";
        public static ConfigEntry<bool> HighlightAttachedItems;
        public static ConfigEntry<bool> HighlightUsableItems;
        public static ConfigEntry<bool> HighlightOnWeaponItems;

        private const string ColorsSectionTitle = "5. Color Settings";
        public static ConfigEntry<Color> ColorOnWeapon;
        public static ConfigEntry<Color> ColorUsable;
        public static ConfigEntry<Color> ColorAttached;
        public static ConfigEntry<Color> ColorAttachedNonBuyable;

        private const string DetachingItemsSectionTitle = "6. Detaching Items";
        public static ConfigEntry<bool> DetachEquippedItems;

        private const string InvertTradersSectionTitle = "7. Invert Trader Only Items";
        public static ConfigEntry<bool> InvertTraderSelection;

        public static void InitConfig(ConfigFile config)
        {
            // GENERAL
            DefaultToTraderOnly = config.Bind(
                GeneralSectionTitle, 
                "Default to trader only view", 
                true,
                new ConfigDescription("When the edit preset screen opens for the first time after game launch, or after a raid, should it default to only show trader items?",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 })
            );

            ColorBorders = config.Bind(
                GeneralSectionTitle,
                "Color borders",
                true,
                new ConfigDescription("Color the borders of all mod slots in the preset screen according to availability",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 })
                );
            ColorBorders.SettingChanged += UpdateModView;

            ShowAttachedItems = config.Bind(
                GeneralSectionTitle,
                "Show attached items (Not buyable)",
                true,
                new ConfigDescription("Show items that are already attached to other weapons, but not currently purchasable from traders",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 0 })
                );
            ShowAttachedItems.SettingChanged += UpdateModView;



            // PRICE TAGS
            ShowPriceTags = config.Bind(
                TraderPricesTitle,
                "Show price tags on items",
                true,
                new ConfigDescription("Show trader price tags on mod icons",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 3 })
            );
            ShowPriceTags.SettingChanged += UpdateModView;

            ShowFleaPriceTags = config.Bind(
                TraderPricesTitle,
                "Show flea price tags",
                true,
                new ConfigDescription("Show flea market price tags if the item is not available from traders",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 })
            );
            ShowFleaPriceTags.SettingChanged += UpdateModViewFlea;

            ShowPriceTagsOnWeaponItems = config.Bind(
                TraderPricesTitle,
                "Price tags on current weapon",
                false,
                new ConfigDescription("Also show trader price tags of mods that are already on the weapon you are currently modding (Default off, looks cleaner to me)",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 })
            );
            ShowPriceTagsOnWeaponItems.SettingChanged += UpdateModView;

            AbbreviatePrices = config.Bind(
                TraderPricesTitle,
                "Abbreviate prices",
                true,
                new ConfigDescription("Abbreviate prices, f.e. 69420 becomes 69,4k",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 0 })
            );
            AbbreviatePrices.SettingChanged += UpdateModView;




            // BUILD COST
            ShowBuildCost = config.Bind(
                BuildCostTitle,
                "Show build cost panel",
                true,
                new ConfigDescription("Show build cost panel",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 })
            );
            ShowBuildCost.SettingChanged += UpdateBuildCostPanel;

            BuildCostFontSize = config.Bind(
                BuildCostTitle,
                "Build cost font size",
                16,
                new ConfigDescription("Font size for the build cost panel",
                new AcceptableValueRange<int>(8, 32),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 0 })
            );
            BuildCostFontSize.SettingChanged += UpdateBuildCostPanel;





            // HIGHLIGHT ITEMS
            HighlightOnWeaponItems = config.Bind(
                HighlightSectionTitle, 
                "Highlight items on current weapon", 
                true,
                new ConfigDescription("Highlight items that are attached to the weapon you are currently modding",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 3 }));
            HighlightOnWeaponItems.SettingChanged += UpdateModView;
            
            HighlightAttachedItems = config.Bind(
                HighlightSectionTitle, 
                "Highlight attached items", 
                true,
                new ConfigDescription("Highlight items that are already attached to other weapons (Doesn't work 100% perfectly)",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));
            HighlightAttachedItems.SettingChanged += UpdateModView;

            HighlightUsableItems = config.Bind(
                HighlightSectionTitle,
                "Highlight usable items",
                true,
                new ConfigDescription("Highlight directly usable (Ultimately not attached to any gun) items",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 })
                );
            HighlightUsableItems.SettingChanged += UpdateModView;



            // COLOR SETTINGS
            ColorOnWeapon = config.Bind(
                ColorsSectionTitle,
                "Color for items on current weapon",
                new Color(0.4745f, 0.7686f, 1f, 0.5f),
                new ConfigDescription("What color to use when highlighting items that are already on the weapon you are currently modding",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 3 }));

            ColorAttachedNonBuyable = config.Bind(
                ColorsSectionTitle,
                "Color for attached items (Not buyable)",
                new Color(1.0f, 0.5f, 0.0f, 0.4f),
                new ConfigDescription("What color to use when highlighting attached items that are NOT purchasable by traders",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));

            ColorAttached = config.Bind(
                ColorsSectionTitle,
                "Color for attached items (Buyable)",
                new Color(1.0f, 1.0f, 0.0f, 0.4f),
                new ConfigDescription("What color to use when highlighting attached items that you can buy from traders",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            ColorUsable = config.Bind(
                ColorsSectionTitle,
                "Color for usable items",
                new Color(0.2f, 1.0f, 0.0f, 0.3f),
                new ConfigDescription("What color to use when highlighting usable items?",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 0 }));


            DetachEquippedItems = config.Bind(
                DetachingItemsSectionTitle,
                "Also detach from equipped guns",
                false,
                "When detaching items in use, should we also rip them off your currently equipped guns? (Still prioritizes: Mods attached to other loose mods, then mods attached to guns in stash)");


            InvertTraderSelection = config.Bind(
                InvertTradersSectionTitle,
                "HIDE all items from traders instead",
                false,
                "Instead of showing ONLY trader items and your own, show NO trader items");
            InvertTraderSelection.SettingChanged += InvertTraderSelectionChanged;
        }


        private static void InvertTraderSelectionChanged(object sender, EventArgs e)
        {
            if (Globals.checkbox_traderOnly_text == null) { return; }

            Globals.checkbox_traderOnly_text.LocalizationKey = InvertTraderSelection.Value ? "Use NO trader items" : "Use only trader items";

            if (Globals.script != null)
                Globals.script.UpdateModView();
        }

        private static void UpdateModViewFlea(object sender, EventArgs e)
        {
            if (Globals.script == null) { return; }

            Globals.script.GetTraderItems();
            Globals.script.UpdateModView();
        }

        private static void UpdateModView(object sender, EventArgs e)
        {
            if (Globals.script == null) { return; }

            Globals.script.UpdateModView();
        }

        private static void UpdateBuildCostPanel(object sender, EventArgs e)
        {
            if (Globals.script == null) { return; }

            Globals.script.UpdateBuildCostPanel();
        }
    }
}