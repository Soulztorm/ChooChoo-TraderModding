using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using EFT;
using Comfort.Common;
using EFT.InventoryLogic;
using TraderModding.Config;

namespace TraderModding
{
    public class EditBuildScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EditBuildScreen), nameof(EditBuildScreen.Awake));
        }

        [PatchPostfix]
        public static async void Postfix(EditBuildScreen __instance)
        {
            
            if (FieldInfos.EditBuildScreen__onlyAvailableToggle == null) { ConsoleScreen.LogError("FieldInfo for onlyAvailableItems == null"); return; }

            Globals.checkbox_availableOnly_toggle = (Toggle)FieldInfos.EditBuildScreen__onlyAvailableToggle.GetValue(__instance);
            if (Globals.checkbox_availableOnly_toggle == null) { ConsoleScreen.LogError("Couldn't get checkbox for onlyAvailableItems"); return; }

            // Clone the existing checkbox and parent it to the toggle group
            GameObject onlyTradersCheckbox = GameObject.Instantiate(Globals.checkbox_availableOnly_toggle.gameObject, Globals.checkbox_availableOnly_toggle.transform.parent, false);
            onlyTradersCheckbox.name = "OnlyTraders";

            // Attach script to the checkbox
            TraderModdingScript script = onlyTradersCheckbox.AddComponent<TraderModdingScript>();
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
            GameObject detachItemsButton = GameObject.Instantiate(assembleButton.gameObject, assembleButton.transform.parent, false);
            detachItemsButton.name = "DetachItems";
            detachItemsButton.transform.SetSiblingIndex(detachItemsButton.transform.GetSiblingIndex() - 1);

            // Get the layout group this button is in to adjust spacing
            var layoutAssembleButton = assembleButton.transform.parent.gameObject.GetComponent<HorizontalLayoutGroup>();
            layoutAssembleButton.spacing = 5;

            // Overwrite listeners and set label text
            ButtonWithHint detachButtonWithHint = detachItemsButton.GetComponent<ButtonWithHint>();
            Button detachButton = (Button)FieldInfos.ButtonWithHint__button.GetValue(detachButtonWithHint);
            detachButton.onClick.RemoveAllListeners();
            detachButton.onClick.AddListener(Globals.script.TryToDetachInUseItems);
            
            Image detachBG = detachButton.gameObject.GetComponent<Image>();
            detachBG.color = Color.yellow;

            Transform detachIcon = detachButton.transform.Find("Icon");
            if (detachIcon != null) {
                detachIcon.localPosition = Vector3.zero;
                detachIcon.localRotation = Quaternion.Euler(0, 0, 180);
                detachIcon.gameObject.GetComponent<Image>().color = new Color(0.75f, 1f, 0.5f, 1.0f);
            }

            TextMeshProUGUI detachLabel = (TextMeshProUGUI)FieldInfos.ButtonWithHint__label.GetValue(detachButtonWithHint);
            detachLabel.text = "DETACH MODS IN USE";
            
            Globals.detachButtonCanvasGroup = detachItemsButton.GetComponent<CanvasGroup>();
            Globals.detachButtonCanvasGroup.alpha = 0.5f;
            
            
            
            
            
            // Clone a button for quickbuy
            // Instantiate and parent
            GameObject quickbuyButton = GameObject.Instantiate(assembleButton.gameObject, Globals.buildCostPanelGO.transform, false);
            quickbuyButton.name = "QuickBuy";
            
            // Overwrite listeners and set label text
            ButtonWithHint quickbuyButtonWithHint = quickbuyButton.GetComponent<ButtonWithHint>();
            Button quickBuyButton = (Button)FieldInfos.ButtonWithHint__button.GetValue(quickbuyButtonWithHint);
            quickBuyButton.onClick.RemoveAllListeners();
            quickBuyButton.onClick.AddListener(Globals.script.TryToBuyItems);
            
            Image background = quickBuyButton.gameObject.GetComponent<Image>();
            background.color = new  Color(0.5f, 0.7f, 0.4f, 1.0f);

            Transform iconT = quickBuyButton.transform.Find("Icon");
            if (iconT != null) {
                iconT.localPosition = Vector3.zero;
                Image icon = iconT.gameObject.GetComponent<Image>();
                icon.sprite = EFTHardSettings.Instance.StaticIcons.DialogLineSprites[GClass3666.EDialogLineIconType.ShoppingCart];
                icon.color = new Color(0.5f, 1f, 0.3f, 1.0f);
            }

            TextMeshProUGUI label = (TextMeshProUGUI)FieldInfos.ButtonWithHint__label.GetValue(quickbuyButtonWithHint);
            label.text = "QUICK BUY ITEMS";
            
            Globals.quickbuyButtonCanvasGroup = quickbuyButton.GetComponent<CanvasGroup>();
            Globals.quickbuyButtonCanvasGroup.alpha = 0.4f;
            
            
            
            // Get all mods that exist
            ItemFactoryClass itemFactoryClass = Singleton<ItemFactoryClass>.Instance;
            if (itemFactoryClass == null)
                return;

            // Get all mods that exist, should be fine to do it only once when the build screen awakes
            Globals.allmods = itemFactoryClass.CreateAllModsEver();
            Array.Sort(Globals.allmods, 
                (i1, i2) => String.Compare(i1.LocalizedShortName(), i2.LocalizedShortName(), StringComparison.OrdinalIgnoreCase));

            // Create a fake stash to hold a pool of fake available items, we move mods in here when they should show up in the modding screen
            Globals.fakestash = itemFactoryClass.CreateFakeStash();
            Globals.fakestash.Grids[0] = new GClass3115(Guid.NewGuid().ToString(), 30, 1, true, Array.Empty<ItemFilter>(),  Globals.fakestash);
            Globals.fakestashTraderController = new TraderControllerClass( Globals.fakestash, "TraderModdingStash", Guid.NewGuid().ToString(), false);
            
            // Set fake items stack size and lock state to be moved
            foreach (Item item in Globals.allmods)
            {
                item.StackObjectsCount = item.StackMaxSize;
                foreach (Item item2 in item.GetAllItems())
                {
                    item2.PinLockState = EItemPinLockState.Free;
                }
            }
            
            // Get the player profile
            if (FieldInfos.EditBuildScreen_profile_0 == null) { ConsoleScreen.LogError("FieldInfo for profile == null"); return; }
            Globals.profile = (Profile)FieldInfos.EditBuildScreen_profile_0.GetValue(__instance);
            if (Globals.profile == null) { ConsoleScreen.LogError("profile == null"); }
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
        public static void Postfix(EditBuildScreen __instance, EditBuildScreen.GClass3881 controller)
        {
            Globals.isOnModdingScreen = true;
            
            Globals.script.__session = controller.Session;
            
            if (Globals.buildCostTextGO == null && Globals.buildCostPanelGO != null)
            {
                TextMeshProUGUI weaponName = (TextMeshProUGUI)FieldInfos.EditBuildScreen__weaponName.GetValue(__instance);

                if (weaponName != null)
                {
                    var captionGO = weaponName.gameObject;

                    Globals.buildCostTextGO = GameObject.Instantiate(captionGO, Globals.buildCostPanelGO.transform, false);
                    Globals.buildCostTextGO.name = "BuildCostText";
                    Globals.buildCostTextGO.transform.SetSiblingIndex(Globals.buildCostTextGO.transform.GetSiblingIndex() - 1);
                    
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
            Globals.itemsOnGun = Array.Empty<MongoID>();
            Globals.itemsInUse = Array.Empty<MongoID>();
            Globals.itemsInUseNonBuyable = Array.Empty<MongoID>();
            Globals.itemsAvailable = Array.Empty<MongoID>();
            Globals.traderModInfo.Clear();

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
            return AccessTools.FirstMethod(typeof(GClass3470),
                x => x.Name == nameof(GClass3470.Assemble));
        }

        [PatchPostfix]
        public static async void Postfix(Task<bool> __result)
        {
            if (Globals.isOnModdingScreen && Globals.script != null)
            {
                bool assembled = await __result;

                if (assembled)
                {
                    TraderModdingUtils.ClearBuyAndDetachItems();
                    
                    Globals.script.GetItemsOnGun();
                    Globals.script.GetItemsInUse();
                    Globals.script.GetItemsInUseNotPurchasable();

                    Globals.script.__instance.RefreshWeapon();
                }
            }
        }
    }
}
