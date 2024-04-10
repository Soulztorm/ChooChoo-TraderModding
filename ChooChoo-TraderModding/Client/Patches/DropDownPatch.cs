using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.WeaponModding;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using TraderModding.Config;
using UnityEngine;
using UnityEngine.UI;

namespace TraderModding
{
    public class DropDownPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(DropDownMenu)).Single(delegate (MethodInfo m)
            {
                ParameterInfo[] parameters = m.GetParameters();
                return parameters.Length == 4 && parameters[0].Name == "sourceContext" && parameters[3].Name == "container";
            });
        }

        [PatchPostfix]
        private static void PostFix(DropDownMenu __instance, Item item, ref RectTransform container)
        {
            Color backGroundColor;

            if (TraderModdingConfig.HighlightAttachedItems.Value && Globals.itemsInUseNonBuyable.Contains(item.TemplateId))
                backGroundColor = TraderModdingConfig.ColorAttachedNonBuyable.Value;
            else if (TraderModdingConfig.HighlightAttachedItems.Value && Globals.itemsInUse.Contains(item.TemplateId))
                backGroundColor = TraderModdingConfig.ColorAttached.Value;
            else if(TraderModdingConfig.HighlightUsableItems.Value && Globals.itemsAvailable.Contains(item.TemplateId))
                backGroundColor = TraderModdingConfig.ColorUsable.Value;
            else      
                return;
            
            Transform lastChild = container.transform.GetChild(container.transform.childCount - 1);
            GameObject colorPanel = lastChild.Find("Color Panel").gameObject;

            GameObject colorPanelCopy = GameObject.Instantiate(colorPanel);
            colorPanelCopy.transform.SetParent(lastChild, false);
            colorPanelCopy.transform.SetSiblingIndex(colorPanel.transform.GetSiblingIndex() + 1);
            Image backgroundImage = colorPanelCopy.GetComponent<Image>();
            backgroundImage.color = backGroundColor;

            Globals.itemsInUseOverlays.Add(colorPanelCopy);           
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
        }
    }


    public class ModdingScreenSlotViewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(ModdingScreenSlotView),
                x => x.Name == nameof(ModdingScreenSlotView.method_0));
        }

        [PatchPostfix]
        private static void PostFix(ModdingScreenSlotView __instance, LootItemClass item)
        {
            ConsoleScreen.Log("Parent: " + __instance.transform.parent.name); 
            if (__instance.transform.parent.name != "Slots Container")
                return;

            Transform modding_menu_item = __instance.transform.Find("modding_menu_item");
            if (modding_menu_item == null) return;
            ConsoleScreen.Log("Found modding_menu_item");


            Transform item_view_container = modding_menu_item.Find("ItemViewContainer");
            if (item_view_container == null) return;
            ConsoleScreen.Log("Found ItemViewContainer");

            Transform modView = item_view_container.GetChild(0);
            if (modView == null) return;
            ConsoleScreen.Log("Found modView");

            //Transform colorPanelTransform = modView.Find("Color Panel");
            //if (colorPanelTransform == null) return;

            //Transform customColorPanel = modView.Find("CustomColorPanel");
            //if (customColorPanel != null) 
            //    GameObject.Destroy(customColorPanel);


            Transform borderTransform = modView.Find("Border");
            if (borderTransform == null) return;

            Image borderImg = borderTransform.GetComponent<Image>();
            RectTransform borderRect = borderTransform.GetComponent<RectTransform>();

            Transform borderShadowTransform = modView.Find("Border Shadow");
            Image borderShadow = borderShadowTransform.GetComponent<Image>();

            bool defaultBorder = false;
            Color backGroundColor;

            if (TraderModdingConfig.HighlightAttachedItems.Value && Globals.itemsInUseNonBuyable.Contains(item.TemplateId))
                backGroundColor = TraderModdingConfig.ColorAttachedNonBuyable.Value;
            else if (TraderModdingConfig.HighlightAttachedItems.Value && Globals.itemsInUse.Contains(item.TemplateId))
                backGroundColor = TraderModdingConfig.ColorAttached.Value;
            else if (TraderModdingConfig.HighlightUsableItems.Value && Globals.itemsAvailable.Contains(item.TemplateId))
                backGroundColor = TraderModdingConfig.ColorUsable.Value;
            else
            {
                defaultBorder = true;
                backGroundColor = new Color(0.2941f, 0.3098f, 0.3216f, 1f);
            }

            borderImg.color = backGroundColor;
            borderShadow.color = backGroundColor;

            if (defaultBorder)
            {
                borderImg.type = Image.Type.Sliced;
                borderRect.localScale = new Vector3(1.0f, 1.0f, 0.0f);
            }
            else
            {
                borderShadow.color.SetAlpha(1.0f);
                borderImg.color.SetAlpha(1.0f);
                borderImg.type = Image.Type.Simple;
                borderRect.localScale = new Vector3(1.1f, 1.1f, 0.0f);
            }
        }
    }
}