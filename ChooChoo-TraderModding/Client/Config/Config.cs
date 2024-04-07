using BepInEx.Configuration;

namespace TraderModding.Config
{
    internal static class TraderModdingConfig
    {
        public static ConfigEntry<bool> ShowAttachedItems;

        public static void InitConfig(ConfigFile config)
        {
            ShowAttachedItems = config.Bind(
                "Trader Modding", 
                "ShowAttachedItems", 
                false, 
                new ConfigDescription("When showing only trader available items (And loose mods), should we also show mods that are alread attached to other weapons?", 
                null, 
                new ConfigurationManagerAttributes { Order = 0 }));
        }
    }
}