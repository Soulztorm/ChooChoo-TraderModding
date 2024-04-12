using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.WeaponModding;
using HarmonyLib;
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
            Transform lastChild = container.transform.GetChild(container.transform.childCount - 1);
            if (lastChild == null) return;


            RemoveExistingPriceTag(lastChild);

            // Add the price tag
            AddItemPriceTag(lastChild, item);

            // Restore borders in case they are out of the pool and we modified them
            RestoreBorder(lastChild);

            // Color backgrounds if any
            Color backGroundColor;

            if (TraderModdingConfig.HighlightAttachedItems.Value && Globals.itemsInUseNonBuyable.Contains(item.TemplateId))
                backGroundColor = TraderModdingConfig.ColorAttachedNonBuyable.Value;
            else if (TraderModdingConfig.HighlightAttachedItems.Value && Globals.itemsInUse.Contains(item.TemplateId))
                backGroundColor = TraderModdingConfig.ColorAttached.Value;
            else if(TraderModdingConfig.HighlightUsableItems.Value && Globals.itemsAvailable.Contains(item.TemplateId))
                backGroundColor = TraderModdingConfig.ColorUsable.Value;
            else      
                return;


            // Add the highlight background
            GameObject colorPanel = lastChild.Find("Color Panel").gameObject;

            GameObject colorPanelCopy = GameObject.Instantiate(colorPanel);
            colorPanelCopy.transform.SetParent(lastChild, false);
            colorPanelCopy.transform.SetSiblingIndex(colorPanel.transform.GetSiblingIndex() + 1);
            colorPanelCopy.name = "HighlightBackground";
            Image backgroundImage = colorPanelCopy.GetComponent<Image>();
            backgroundImage.color = backGroundColor;

            Globals.itemsInUseOverlays.Add(colorPanelCopy);




        }

        public static void RestoreBorder(Transform borderParentItem)
        {
            Transform borderTransform = borderParentItem.Find("Border");
            if (borderTransform != null)
            {
                // Border
                Image borderImg = borderTransform.GetComponent<Image>();
                RectTransform borderRect = borderTransform.GetComponent<RectTransform>();
                borderImg.color = new Color(0.2863f, 0.3176f, 0.3294f, 1f);
                borderImg.type = Image.Type.Sliced;
                borderRect.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                // Shadow
                Transform borderShadowTransform = borderParentItem.Find("Border Shadow");
                if (borderShadowTransform != null)
                {
                    Image borderShadow = borderShadowTransform.GetComponent<Image>();
                    borderShadow.color = new Color(1f, 1f, 1f, 1f);
                }
            }
        }

        public static void RemoveExistingPriceTag(Transform parent)
        {
            // If we already added a price tag, destroy that one first
            Transform existingPriceTag = parent.Find("Info Panel/Mod Type Icon/ItemPriceTag");
            if (existingPriceTag != null)
                GameObject.Destroy(existingPriceTag.gameObject);

            Transform existingPriceTagBG = parent.Find("Info Panel/Mod Type Icon/ItemPriceTagBG");
            if (existingPriceTagBG != null)
                GameObject.Destroy(existingPriceTagBG.gameObject);
        }

        public static void AddItemPriceTag(Transform parent, Item item)
        {
            // Find the info panel
            Transform infoPanel = parent.Find("Info Panel");

            // Now if we find a name and have a price for that item, add the pricetag
            Transform gunName = infoPanel.Find("Name");
            if (gunName != null)
            {
                Transform modTypeIcon = infoPanel.Find("Mod Type Icon");
                if (modTypeIcon != null)
                {
                    string costText;
                    if (Globals.traderModsTplCost.TryGetValue(item.TemplateId, out costText))
                    {
                        // Add a black background
                        GameObject colorPanel = parent.Find("Color Panel").gameObject;
                        GameObject itemPriceTagBackground = GameObject.Instantiate(colorPanel);
                        itemPriceTagBackground.name = "ItemPriceTagBG";
                        itemPriceTagBackground.transform.SetParent(modTypeIcon, false);
                        RectTransform bgRect = itemPriceTagBackground.GetComponent<RectTransform>();
                        bgRect.anchoredPosition = Vector2.zero;
                        bgRect.anchorMax = new Vector2(4.5f, 1f);
                        bgRect.anchorMin = new Vector2(1f, -0.1f);
                        bgRect.pivot = new Vector2(0.5f, 0.5f);
                        bgRect.sizeDelta = new Vector2(0.5f, 0.5f);
                        Image bgImage = itemPriceTagBackground.GetComponent<Image>();
                        bgImage.color = Color.black;


                        // Create a copy of the name for the price tag
                        GameObject itemPriceTag = GameObject.Instantiate(gunName.gameObject);
                        itemPriceTag.transform.SetParent(modTypeIcon, false);
                        itemPriceTag.name = "ItemPriceTag";
                        itemPriceTag.transform.SetSiblingIndex(itemPriceTagBackground.transform.GetSiblingIndex()  + 1);

                        RectTransform priceTagRect = itemPriceTag.GetComponent<RectTransform>();
                        priceTagRect.anchoredPosition = new Vector2(0, 0);
                        priceTagRect.anchorMin = new Vector2(1.15f, 0);
                        priceTagRect.anchorMax = new Vector2(50, 1);
                        priceTagRect.offsetMin = priceTagRect.offsetMax = priceTagRect.pivot = Vector2.zero;

                        // Finally set the price
                        CustomTextMeshProUGUI itemPriceText = itemPriceTag.GetComponent<CustomTextMeshProUGUI>();
                        itemPriceText.text = costText;


                        // Disable the not in eq icon
                        Transform NotInEquipmentIcon = infoPanel.Find("NotInEquipmentIcon");
                        if (NotInEquipmentIcon != null)
                            NotInEquipmentIcon.gameObject.SetActive(false);
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
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(ModdingScreenSlotView),
                x => x.Name == nameof(ModdingScreenSlotView.method_0));
        }

        [PatchPostfix]
        private static void PostFix(ModdingScreenSlotView __instance, LootItemClass item)
        {
            if (__instance.transform.parent.name != "Slots Container")
                return;

            Transform item_view_container = __instance.transform.Find("modding_menu_item/ItemViewContainer");
            if (item_view_container == null) return;

            Transform modView = item_view_container.GetChild(0);
            if (modView == null) return;


            if (!TraderModdingConfig.ColorBorders.Value)
                DropDownPatch.RestoreBorder(modView);
            else
            {
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
        }
    }
}