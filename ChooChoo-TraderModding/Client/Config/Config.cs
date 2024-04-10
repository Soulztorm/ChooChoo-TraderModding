using BepInEx.Configuration;
using System;
using UnityEngine;

namespace TraderModding.Config
{
    internal static class TraderModdingConfig
    {
        private const string GeneralSectionTitle = "1. General";
        public static ConfigEntry<bool> DefaultToTraderOnly;
        public static ConfigEntry<bool> ShowAttachedItems;

        private const string HighlightAttachedSectionTitle = "2. Attached Items";
        public static ConfigEntry<bool> HighlightAttachedItems;
        public static ConfigEntry<Color> ColorAttached;
        public static ConfigEntry<Color> ColorAttachedNonBuyable;

        private const string HighlightUsableSectionTitle = "3. Usable Items";
        public static ConfigEntry<bool> HighlightUsableItems;
        public static ConfigEntry<Color> ColorUsable;

        private const string InvertTradersSectionTitle = "4. Invert Trader Only Items";
        public static ConfigEntry<bool> InvertTraderSelection;

        public static void InitConfig(ConfigFile config)
        {
            DefaultToTraderOnly = config.Bind(
                GeneralSectionTitle, 
                "Default to trader only view", 
                true, 
                "When the edit preset screen opens for the first time after game launch, or after a raid, should it default to only show trader items?");
            
            

            HighlightUsableItems = config.Bind(
                HighlightUsableSectionTitle, 
                "Highlight usable items", 
                true,
                new ConfigDescription("Highlight directly usable (Ultimately not attached to any gun) items",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 })
                );
            HighlightUsableItems.SettingChanged += UpdateModView;

            ColorUsable = config.Bind(
                HighlightUsableSectionTitle, 
                "Color for usable items", 
                new Color(0.2f, 1.0f, 0.0f, 0.3f),
                new ConfigDescription("What color to use when highlighting usable items?",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 0 }));


            ShowAttachedItems = config.Bind(
                HighlightAttachedSectionTitle,
                "Show attached items (Not purchasable)",
                true,
                new ConfigDescription("Show items that are already attached to other weapons, but not purchasable from traders",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 3 })
                );
            ShowAttachedItems.SettingChanged += UpdateModView;

            HighlightAttachedItems = config.Bind(
                HighlightAttachedSectionTitle, 
                "Highlight already attached items", 
                true,
                new ConfigDescription("Highlight items that are already attached to other weapons",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));
            HighlightAttachedItems.SettingChanged += UpdateModView;

            ColorAttached = config.Bind(
                HighlightAttachedSectionTitle,
                "Color for attached items (Buyable)",
                new Color(1.0f, 1.0f, 0.0f, 0.4f),
                new ConfigDescription("What color to use when highlighting attached items that you can buy from traders?",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            ColorAttachedNonBuyable = config.Bind(
                HighlightAttachedSectionTitle,
                "Color for attached items (Non buyable)",
                new Color(1.0f, 0.5f, 0.0f, 0.4f),
                new ConfigDescription("What color to use when highlighting attached items that are NOT purchasable by traders?",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 0 }));


            InvertTraderSelection = config.Bind(
                InvertTradersSectionTitle,
                "HIDE all items from traders instead",
                false,
                "Instead of showing ONLY trader items and your own, show NO trader items");
            InvertTraderSelection.SettingChanged += InvertTraderSelectionChanged;
        }


        private static void InvertTraderSelectionChanged(object sender, EventArgs e)
        {
            if (Globals.traderOnlyCheckboxText == null) { return; }

            Globals.traderOnlyCheckboxText.LocalizationKey = InvertTraderSelection.Value ? "Use NO trader items" : "Use only trader items";

            if (Globals.script != null)
                Globals.script.UpdateModView();
        }

        private static void UpdateModView(object sender, EventArgs e)
        {
            if (Globals.script == null) { return; }

            Globals.script.UpdateModView();
        }
    }
}