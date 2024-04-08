using BepInEx.Configuration;
using UnityEngine;

namespace TraderModding.Config
{
    internal static class TraderModdingConfig
    {
        private const string GeneralSectionTitle = "General";
        public static ConfigEntry<bool> DefaultToTraderOnly;
        public static ConfigEntry<bool> ShowAttachedItems;

        private const string HighlightUsableSectionTitle = "Highlight Usable Items";
        public static ConfigEntry<bool> HighlightUsableItems;
        public static ConfigEntry<Color> ColorUsable;

        private const string HighlightAttachedSectionTitle = "Highlight Attached Items";
        public static ConfigEntry<bool> HighlightAttachedItems;
        public static ConfigEntry<Color> ColorAttached;

        public static void InitConfig(ConfigFile config)
        {
            DefaultToTraderOnly = config.Bind(
                GeneralSectionTitle, 
                "Default to trader only view", 
                true, 
                "When the edit preset screen opens, should it default to only show trader items?");
            
            ShowAttachedItems = config.Bind(
                GeneralSectionTitle, 
                "Show attached items", 
                true, 
                "Should we also show items that are already attached to other weapons?");



            HighlightUsableItems = config.Bind(
                HighlightUsableSectionTitle, 
                "Highlight usable items", 
                true,
                new ConfigDescription("Should we highlight directly usable (Ultimately not attached to any gun) items?",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 })
                );

            ColorUsable = config.Bind(
                HighlightUsableSectionTitle, 
                "Color for usable items", 
                new Color(0.2f, 1.0f, 0.0f, 0.3f),
                new ConfigDescription("What color to use when highlighting usable items?",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 0 }));



            HighlightAttachedItems = config.Bind(
                HighlightAttachedSectionTitle, 
                "Highlight already attached items", 
                true,
                new ConfigDescription("Should we highlight items that are already attached to other weapons?",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            ColorAttached = config.Bind(
                HighlightAttachedSectionTitle,
                "Color for already attached items",
                new Color(1.0f, 1.0f, 0.0f, 0.4f),
                new ConfigDescription("What color to use when highlighting usable items?",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 0 }));
        }
    }
}