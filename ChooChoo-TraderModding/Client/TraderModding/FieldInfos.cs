using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.WeaponModding;
using HarmonyLib;
using System.Reflection;

namespace ChooChooTraderModding
{
    internal static class FieldInfos
    {
        public static FieldInfo EditBuildScreen__onlyAvailableToggle = AccessTools.Field(typeof(EditBuildScreen), "_onlyAvailableToggle");
        public static FieldInfo EditBuildScreen_profile_0 = AccessTools.Field(typeof(EditBuildScreen), "profile_0");
        public static FieldInfo EditBuildScreen__weaponName = AccessTools.Field(typeof(EditBuildScreen), "_weaponName");
        public static FieldInfo EditBuildScreen__assembleButton = AccessTools.Field(typeof(EditBuildScreen), "_assembleButton");
        public static FieldInfo EditBuildScreen_gclass2766_0 = AccessTools.Field(typeof(EditBuildScreen), "gclass2766_0");
        public static FieldInfo EditBuildScreen_gclass2829_1 = AccessTools.Field(typeof(EditBuildScreen), "gclass2829_1");

        public static FieldInfo ButtonWithHint__button = AccessTools.Field(typeof(ButtonWithHint), "_button");
        public static FieldInfo ButtonWithHint__label = AccessTools.Field(typeof(ButtonWithHint), "_label");

        public static FieldInfo ItemView_ColorPanel = AccessTools.Field(typeof(ItemView), "ColorPanel");
        public static FieldInfo ItemView_Border = AccessTools.Field(typeof(ItemView), "_border");
        public static FieldInfo GridItemView_Caption = AccessTools.Field(typeof(GridItemView), "Caption");
        public static FieldInfo GridItemView_itemViewStats = AccessTools.Field(typeof(GridItemView), "_itemViewStats");
        public static FieldInfo ItemViewStats_modTypeIcon = AccessTools.Field(typeof(ItemViewStats), "_modTypeIcon");
        public static FieldInfo ModdingSelectableItemView_NotInEquipmentIcon = AccessTools.Field(typeof(ModdingSelectableItemView), "_missingInInventory");
        public static FieldInfo ModdingScreenSlotView_slot_0 = AccessTools.Field(typeof(ModdingScreenSlotView), "slot_0");
        public static FieldInfo ModdingScreenSlotView__moddingItemContainer = AccessTools.Field(typeof(ModdingScreenSlotView), "_moddingItemContainer");

        public static FieldInfo ItemObserveScreen_weaponPreview = AccessTools.Field(typeof(ItemObserveScreen<EditBuildScreen.GClass3126, EditBuildScreen>), "_weaponPreview");


    }
}