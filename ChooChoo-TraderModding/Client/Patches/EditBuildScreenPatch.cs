using System.Reflection;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using ChooChooTraderModding.Config;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using EFT;

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
            
            if (FieldInfos.EditBuildScreen__onlyAvailableToggle == null) { ConsoleScreen.LogError("FieldInfo for onlyAvailableItems == null"); return; }

            Globals.checkbox_availableOnly_toggle = (Toggle)FieldInfos.EditBuildScreen__onlyAvailableToggle.GetValue(__instance);
            if (Globals.checkbox_availableOnly_toggle == null) { ConsoleScreen.LogError("Couldn't get checkbox for onlyAvailableItems"); return; }

            // Clone the existing checkbox and parent it to the toggle group
            GameObject onlyTradersCheckbox = GameObject.Instantiate(Globals.checkbox_availableOnly_toggle.gameObject);
            onlyTradersCheckbox.name = "OnlyTraders";
            onlyTradersCheckbox.transform.SetParent(Globals.checkbox_availableOnly_toggle.transform.parent, false);

            // Attach script to the checkbox
            TraderModdingOnlyScript script = onlyTradersCheckbox.AddComponent<TraderModdingOnlyScript>();
            script.__instance = __instance;
            // Reference to script
            Globals.script = script;

            // Get a reference to the text, and change it
            LocalizedText labelText = onlyTradersCheckbox.GetComponentInChildren<LocalizedText>();
            labelText.LocalizationKey =  TraderModdingConfig.InvertTraderSelection.Value ? "Use NO trader items" : "Use only trader items";
            Globals.checkbox_traderOnly_text = labelText;

            // Listeners for only trader checkbox
            Globals.checkbox_traderOnly_toggle = onlyTradersCheckbox.GetComponent<Toggle>();
            Globals.checkbox_traderOnly_toggle.isOn = TraderModdingConfig.DefaultToTraderOnly.Value;
            Globals.checkbox_traderOnly_toggle.onValueChanged.AddListener(new UnityAction<bool>(Globals.script.ToggleTradersOnlyView));

            // Replace original only available listener with ours
            Globals.checkbox_availableOnly_toggle.onValueChanged.RemoveAllListeners();
            Globals.checkbox_availableOnly_toggle.onValueChanged.AddListener(new UnityAction<bool>(Globals.script.ToggleOnlyAvailableView));



            // Create Build Cost Panel and parent it to the screen
            Globals.buildCostPanelGO = new GameObject("BuildCostPanel");
            Globals.buildCostPanelGO.transform.SetParent(__instance.gameObject.transform, false);

            var img = Globals.buildCostPanelGO.AddComponent<Image>();
            img.material.mainTexture = Texture2D.whiteTexture;
            img.color = new Color(0.1486f, 0.1565f, 0.1604f, 1) ;

            var rectTransform = Globals.buildCostPanelGO.GetComponent<RectTransform>();
            var dragComponent = Globals.buildCostPanelGO.AddComponent<UIDragComponent>();
            dragComponent.Init(rectTransform, true);

            var contentFitter = Globals.buildCostPanelGO.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layoutGroup = Globals.buildCostPanelGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.padding = new RectOffset(7, 7, 5, 5);

            // Scale from the top left
            rectTransform.pivot = new Vector2(0, 1);
            Globals.buildCostPanelGO.transform.position = new Vector3(215, 1014, 0);


            // Clone a button to detach used items
            ButtonWithHint assembleButton = (ButtonWithHint)FieldInfos.EditBuildScreen__assembleButton.GetValue(__instance);
            if (assembleButton == null) { ConsoleScreen.LogError("Couldn't get assemble button to clone"); return; }

            // Instantiate and parent
            GameObject detachItemsButton = GameObject.Instantiate(assembleButton.gameObject);
            detachItemsButton.name = "DetachItems";
            detachItemsButton.transform.SetParent(assembleButton.transform.parent, false);
            detachItemsButton.transform.SetSiblingIndex(detachItemsButton.transform.GetSiblingIndex() - 1);

            // Get the layout group this button is in to adjust spacing
            var layout = assembleButton.transform.parent.gameObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;

            // Overwrite listeners and set label text
            ButtonWithHint buttonWithHint = detachItemsButton.GetComponent<ButtonWithHint>();
            Button button = (Button)FieldInfos.ButtonWithHint__button.GetValue(buttonWithHint);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(new UnityAction(Globals.script.TryToDetachInUseItems));
            
            Image background = button.gameObject.GetComponent<Image>();
            background.color = Color.yellow;

            Transform iconT = button.transform.Find("Icon");
            if (iconT != null) {
                iconT.localPosition = Vector3.zero;
                iconT.localRotation = Quaternion.Euler(0, 0, 180);
                Image icon = iconT.gameObject.GetComponent<Image>();
                icon.color = new Color(0.75f, 1f, 0.5f, 1.0f);
            }

            TextMeshProUGUI label = (TextMeshProUGUI)FieldInfos.ButtonWithHint__label.GetValue(buttonWithHint);
            label.text = "DETACH MODS IN USE";


            Globals.detachButtonCanvasGroup = detachItemsButton.GetComponent<CanvasGroup>();
            Globals.detachButtonCanvasGroup.alpha = 0.5f;
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
        public static void Postfix(EditBuildScreen __instance, EditBuildScreen.GClass3591 controller)
        {
            Globals.isOnModdingScreen = true;

            if (Globals.buildCostTextGO == null && Globals.buildCostPanelGO != null)
            {
                TextMeshProUGUI weaponName = (TextMeshProUGUI)FieldInfos.EditBuildScreen__weaponName.GetValue(__instance);

                if (weaponName != null)
                {
                    var captionGO = weaponName.gameObject;

                    Globals.buildCostTextGO = GameObject.Instantiate(captionGO);
                    Globals.buildCostTextGO.name = "BuildCostText";
                    Globals.buildCostTextGO.transform.SetParent(Globals.buildCostPanelGO.transform, false);
                    GameObject.Destroy(Globals.buildCostTextGO.GetComponent<ContentSizeFitter>());

                    var panelText = Globals.buildCostTextGO.GetComponent<TextMeshProUGUI>();
                    panelText.alignment = TMPro.TextAlignmentOptions.TopRight;
                    panelText.fontSize = TraderModdingConfig.BuildCostFontSize.Value;
                    panelText.text = TraderModdingUtils.build_cost_header;
                }
                else
                {
                    ConsoleScreen.LogError("Could not find weapon name gameobject to clone.");
                }
            }

            Globals.script.weaponBody = controller.Item;

            TraderModdingUtils.ClearBuyAndDetachItems();

            Globals.script.RefreshEverything();
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
            Globals.itemsOnGun = new MongoID[0];
            Globals.itemsInUse = new MongoID[0];
            Globals.itemsInUseNonBuyable = new MongoID[0];
            Globals.itemsAvailable = new MongoID[0];
            Globals.traderModsTplCost.Clear();

            TraderModdingUtils.ClearBuyAndDetachItems();

            if (Globals.buildCostTextGO != null)
            {
                var buildCostText = Globals.buildCostTextGO.GetComponent<TextMeshProUGUI>();
                buildCostText.text = TraderModdingUtils.build_cost_header;
            }
        }
    }

    public class EditBuildScreenAssembledWeaponPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(GClass3259),
                x => x.Name == nameof(GClass3259.Assemble));
        }

        [PatchPostfix]
        public static async void Postfix(Task<bool> __result)
        {
            if (Globals.isOnModdingScreen && Globals.script != null)
            {
                bool assembled = await __result;

                if (assembled)
                {
                    Globals.script.GetItemsOnGun();
                    Globals.script.GetItemsInUse();
                    Globals.script.GetItemsInUseNotPurchasable();

                    Globals.script.__instance.RefreshWeapon();
                }
            }
        }
    }
}
