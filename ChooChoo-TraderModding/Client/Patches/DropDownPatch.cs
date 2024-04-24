using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI.WeaponModding;
using HarmonyLib;
using ChooChooTraderModding.Config;
using UnityEngine;
using UnityEngine.UI;
using System;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System.Collections.Generic;

namespace ChooChooTraderModding
{
    public class DropDownPatch : ModulePatch
    {
        public static FieldInfo GridItemView_Caption;
        public static FieldInfo GridItemView_itemViewStats;
        public static FieldInfo ItemViewStats_modTypeIcon;
        public static FieldInfo ItemView_ColorPanel;
        public static FieldInfo ModdingSelectableItemView_NotInEquipmentIcon;
        public static FieldInfo ItemView_BackgroundShadow;
        public static FieldInfo ItemView_Border;

        protected override MethodBase GetTargetMethod()
        {
            GridItemView_Caption = AccessTools.Field(typeof(GridItemView), "Caption");
            GridItemView_itemViewStats = AccessTools.Field(typeof(GridItemView), "_itemViewStats");
            ItemViewStats_modTypeIcon = AccessTools.Field(typeof(ItemViewStats), "_modTypeIcon");
            ItemView_ColorPanel = AccessTools.Field(typeof(ItemView), "ColorPanel");
            ModdingSelectableItemView_NotInEquipmentIcon = AccessTools.Field(typeof(ModdingSelectableItemView), "_missingInInventory");
            ItemView_BackgroundShadow = AccessTools.Field(typeof(ItemView), "BackgroundShadow");
            ItemView_Border = AccessTools.Field(typeof(ItemView), "_border");

            return AccessTools.GetDeclaredMethods(typeof(DropDownMenu)).Single(delegate (MethodInfo m)
            {
                ParameterInfo[] parameters = m.GetParameters();
                return parameters.Length == 4 && parameters[0].Name == "sourceContext" && parameters[3].Name == "container";
            });
        }

        [PatchPostfix]
        private static void PostFix(DropDownMenu __instance, Item item, ref RectTransform container)
        {
            Transform lastChild = container.transform.GetChild(container.transform.childCount - 1);
            if (lastChild == null) return;

            ModdingSelectableItemView modItemView = lastChild.GetComponent<ModdingSelectableItemView>();
            if (modItemView == null) { ConsoleScreen.LogError("Couldn't find modItemView"); return; }

            // Remove the old tag, in case it comes from the pool where the slot icon was in before
            TraderModdingUtils.RemoveExistingPriceTag(modItemView);

            // Add the price tag
            if (TraderModdingConfig.ShowPriceTags.Value)
                TraderModdingUtils.AddItemPriceTag(modItemView, item);

            // Restore borders in case they are out of the pool and we modified them
            RestoreBorder(modItemView);

            // Color backgrounds if any
            Color backGroundColor = new Color();
            bool itemNeedsToBeBought = false;
            if (!TraderModdingUtils.GetColorForItem(item, ref backGroundColor, ref itemNeedsToBeBought))
                return;


            Image colorPanel = (Image)ItemView_ColorPanel.GetValue(modItemView as ItemView);
            if (colorPanel == null) { ConsoleScreen.LogError("Couldn't create background color panel"); return; }

            // Add the highlight background
            GameObject colorPanelCopy = GameObject.Instantiate(colorPanel.gameObject);
            colorPanelCopy.transform.SetParent(lastChild, false);
            colorPanelCopy.transform.SetSiblingIndex(colorPanel.transform.GetSiblingIndex() + 1);
            colorPanelCopy.name = "HighlightBackground";
            Image backgroundImage = colorPanelCopy.GetComponent<Image>();
            backgroundImage.color = backGroundColor;

            Globals.itemsInUseOverlays.Add(colorPanelCopy);
        }

        public static void RestoreBorder(ModdingSelectableItemView modItemView)
        {
            Image borderImg = (Image)ItemView_Border.GetValue(modItemView);
            if (borderImg != null)
            {
                // Border
                RectTransform borderRect = borderImg.gameObject.GetComponent<RectTransform>();
                borderImg.color = new Color(0.2863f, 0.3176f, 0.3294f, 1f);
                borderImg.type = Image.Type.Sliced;
                borderRect.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                // Shadow
                Transform shadowT = borderImg.transform.parent.Find("Border Shadow");
                if (shadowT != null)
                {
                    //Image borderShadow = (Image)ItemView_BackgroundShadow.GetValue(modItemView);
                    Image borderShadow = shadowT.gameObject.GetComponent<Image>();
                    if (borderShadow != null)
                    {
                        borderShadow.color = new Color(1f, 1f, 1f, 1f);
                    }
                }
            }
        }
    }

    public class DropDownPatchDeleteOverlays : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(DropDownMenu),
                x => x.Name == nameof(DropDownMenu.Close));
        }

        [PatchPostfix]
        private static void PostFix()
        {
            // Destroy all previous overlays, if any
            foreach (var go in Globals.itemsInUseOverlays)
            {
                GameObject.Destroy(go);
            }
            Globals.itemsInUseOverlays.Clear();
        }
    }


    public class ModdingScreenSlotViewPatch : ModulePatch
    {
        private static FieldInfo ModdingScreenSlotView_slot_0;
        private static FieldInfo ModdingScreenSlotView__moddingItemContainer;

        protected override MethodBase GetTargetMethod()
        {
            ModdingScreenSlotView_slot_0 = AccessTools.Field(typeof(ModdingScreenSlotView), "slot_0");
            ModdingScreenSlotView__moddingItemContainer = AccessTools.Field(typeof(ModdingScreenSlotView), "_moddingItemContainer");

            return AccessTools.FirstMethod(typeof(ModdingScreenSlotView),
                x => x.Name == nameof(ModdingScreenSlotView.method_0));
        }

        [PatchPostfix]
        private static void PostFix(ModdingScreenSlotView __instance, LootItemClass item)
        {
            Slot slot = (Slot)ModdingScreenSlotView_slot_0.GetValue(__instance);
            if (slot == null ||slot.ContainedItem == null)
                return;

            RectTransform moddingContainer = (RectTransform)ModdingScreenSlotView__moddingItemContainer.GetValue(__instance);
            Transform modView = moddingContainer.transform.GetChild(0);
            if (modView == null) return;

            ModdingSelectableItemView modItemView = modView.gameObject.GetComponent<ModdingSelectableItemView>();

            bool itemNeedsToBeBought = false;

            if (!TraderModdingConfig.ColorBorders.Value)
                DropDownPatch.RestoreBorder(modItemView);
            else
            {
                Image borderImg = (Image)DropDownPatch.ItemView_Border.GetValue(modItemView);
                if (borderImg == null) return;
                RectTransform borderRect = borderImg.gameObject.GetComponent<RectTransform>();

                //Image borderShadow = (Image)DropDownPatch.ItemView_BackgroundShadow.GetValue(modItemView);
                //if (borderShadow == null) return;
                Transform borderShadowTransform = modView.Find("Border Shadow");
                Image borderShadow = borderShadowTransform.GetComponent<Image>();

                if (borderShadow == null) return;

                bool defaultBorder = false;

                Color backGroundColor = new Color();
                if (!TraderModdingUtils.GetColorForItem(item, ref backGroundColor, ref itemNeedsToBeBought))
                {
                    defaultBorder = true;
                    backGroundColor = new Color(0.2941f, 0.3098f, 0.3216f, 1f);
                }

                borderImg.color = backGroundColor;
                borderShadow.color = backGroundColor;

                if (defaultBorder)
                {
                    borderImg.type = Image.Type.Sliced;
                    borderRect.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                }
                else
                {
                    borderShadow.color.SetAlpha(1.0f);
                    borderImg.color.SetAlpha(1.0f);
                    borderImg.type = Image.Type.Simple;
                    borderRect.localScale = new Vector3(1.1f, 1.1f, 1.0f);
                }
            }

            TraderModdingUtils.RemoveExistingPriceTag(modItemView);

            // Show the price tag if the part is not already on the gun
            if (TraderModdingConfig.ShowPriceTagsOnWeaponItems.Value || (TraderModdingConfig.ShowPriceTags.Value && !Globals.itemsOnGun.Contains(item.TemplateId)))
                TraderModdingUtils.AddItemPriceTag(modItemView, item, false);

            // Add to the list of items to buy if necessary
            if (itemNeedsToBeBought)
            {
                Globals.itemsToBuy.Add(item.TemplateId);
            }              
        }
    }
}