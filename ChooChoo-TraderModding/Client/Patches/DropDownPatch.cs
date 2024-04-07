using System;
using System.Collections.ObjectModel;
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
            if (!TraderModdingConfig.ShowAttachedItems.Value)
                return;

            if (Globals.itemsInUse.Contains(item.TemplateId))
            {
                Transform lastChild = container.transform.GetChild(container.transform.childCount - 1);
                GameObject colorPanel = lastChild.Find("Color Panel").gameObject;

                GameObject colorPanelCopy = GameObject.Instantiate(colorPanel);
                colorPanelCopy.name = "ItemInUseOverlay";
                colorPanelCopy.transform.SetParent(lastChild, false);
                colorPanelCopy.transform.SetSiblingIndex(colorPanel.transform.GetSiblingIndex() + 1);
                Image backgroundImage = colorPanelCopy.GetComponent<Image>();
                backgroundImage.color = new Color(1.0f, 1.0f, 0.0f, 0.4f);

                Globals.itemsInUseOverlays.Add(colorPanelCopy);
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
        }
    }
}