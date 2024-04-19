using System.Reflection;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using ChooChooTraderModding.Config;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ChooChooTraderModding
{
    public class EditBuildScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EditBuildScreen), nameof(EditBuildScreen.Awake));
        }

        [PatchPostfix]
        public static void Postfix(EditBuildScreen __instance)
        {
            GameObject togglegroup = __instance.transform.Find("Toggle Group").gameObject;
            GameObject onlyavail = togglegroup.transform.Find("OnlyAvailable").gameObject;

            GameObject onlyTradersCheckbox = GameObject.Instantiate(onlyavail);
            onlyTradersCheckbox.name = "OnlyTraders";
            onlyTradersCheckbox.transform.SetParent(togglegroup.transform, false);
            LocalizedText labelText = onlyTradersCheckbox.GetComponentInChildren<LocalizedText>();
            labelText.LocalizationKey =  TraderModdingConfig.InvertTraderSelection.Value ? "Use NO trader items" : "Use only trader items";
            Globals.traderOnlyCheckboxText = labelText;

            TraderModdingOnlyScript script = onlyTradersCheckbox.AddComponent<TraderModdingOnlyScript>();
            script.__instance = __instance;
            script.onlyAvailableToggle = onlyavail.GetComponent<Toggle>();
            script.onlyTradersToggle = onlyTradersCheckbox.GetComponent<Toggle>();
            script.onlyTradersToggle.isOn = TraderModdingConfig.DefaultToTraderOnly.Value;

            script.onlyTradersToggle.onValueChanged.AddListener(new UnityAction<bool>(script.ToggleTradersOnlyView));

            // Replace original only available listener with ours
            script.onlyAvailableToggle.onValueChanged.RemoveAllListeners();
            script.onlyAvailableToggle.onValueChanged.AddListener(new UnityAction<bool>(script.ToggleOnlyAvailableView));



            // New panel
            var screenGO = __instance.gameObject;

            GameObject newPanel = new GameObject("NewPanel");
            newPanel.transform.SetParent(screenGO.transform, false);

            var img = newPanel.AddComponent<Image>();
            img.material.mainTexture = Texture2D.whiteTexture;
            img.color = new Color(0.0667f, 0.0706f, 0.0706f, 1) ;

            var rectTransform = newPanel.GetComponent<RectTransform>();
            var dragComponent = newPanel.AddComponent<UIDragComponent>();
            dragComponent.Init(rectTransform, true);

            var contentFitter = newPanel.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layoutGroup = newPanel.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.padding = new RectOffset(5, 5, 5, 5);

            // Scale from the top left
            rectTransform.pivot = new Vector2(0, 1);
            newPanel.transform.position = new Vector3(215, 1014, 0);

            // Reference to script
            Globals.script = script;
        }
    }

    public class EditBuildScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(EditBuildScreen),
                x => x.Name == nameof(EditBuildScreen.Show));
        }

        [PatchPostfix]
        public static void Postfix(EditBuildScreen __instance, EditBuildScreen.GClass3126 controller)
        {
            Globals.isOnModdingScreen = true;



            if (Globals.buildCostTextGO ==  null)
            {
                var newPanelGO = __instance.transform.Find("NewPanel");
                var captionGO = __instance.transform.Find("Sub-caption").gameObject;

                Globals.buildCostTextGO = GameObject.Instantiate(captionGO);
                Globals.buildCostTextGO.transform.SetParent(newPanelGO, false);
                GameObject.Destroy(Globals.buildCostTextGO.GetComponent<ContentSizeFitter>());

                var panelText = Globals.buildCostTextGO.GetComponent<CustomTextMeshProUGUI>();
                panelText.alignment = TMPro.TextAlignmentOptions.TopRight;
                panelText.fontSize = 14;
                panelText.text =  
                    "-  Build Cost  -" +
                    "\n‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾" +
                    "\n123<color=#c4bc89> ₽</color>" +
                    "\n+ 99876456<color=#03d100> $</color>" +
                    "\n‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾" +
                    "\n998764562<color=#c4bc89> ₽</color>";
            }


            GameObject togglegroup = __instance.transform.Find("Toggle Group").gameObject;
            GameObject onlyavailable = togglegroup.transform.Find("OnlyAvailable").gameObject;
            GameObject onlytraders = togglegroup.transform.Find("OnlyTraders").gameObject;

            TraderModdingOnlyScript script = onlytraders.GetComponent<TraderModdingOnlyScript>();
            script.weaponBody = controller.Item;

            // Get the trader items
            script.GetTraderItems();

            // Get items in use
            script.GetItemsInUse();

            // Get items in use that are not purchasable
            script.GetItemsInUseNotPurchasable();

            // Let's also fix BSG's bug that closing and reopening the modding screen can have the checkbox on without any effect
            bool onlyAvailableTicked = onlyavailable.GetComponent<Toggle>().isOn;
            if (onlyAvailableTicked) 
            { 
                script.GetItemsOnGun();
                __instance.method_41(true);
            }
            else if (onlytraders.GetComponent<Toggle>().isOn)
            {
                script.UpdateModView();
            }
            else
            {
                script.GetItemsOnGun();
                __instance.method_41(false);
            }
        }
    }

    public class EditBuildScreenClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(EditBuildScreen),
                x => x.Name == nameof(EditBuildScreen.Close));
        }

        [PatchPostfix]
        public static void Postfix(EditBuildScreen __instance)
        {
            Globals.isOnModdingScreen = false;
            Globals.itemsOnGun = new string[0];
            Globals.itemsInUse = new string[0];
            Globals.itemsInUseNonBuyable = new string[0];
            Globals.itemsAvailable = new string[0];
            Globals.traderModsTplCost.Clear();
        }
    }

    public class EditBuildScreenAssembledWeaponPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(GClass2832),
                x => x.Name == nameof(GClass2832.Assemble));
        }

        [PatchPostfix]
        public static async void Postfix(Task<bool> __result)
        {
            if (Globals.isOnModdingScreen)
            {
                bool assembled = await __result;

                if (assembled)
                {
                    Globals.script.UpdateModView();
                }
            }
        }
    }
}
