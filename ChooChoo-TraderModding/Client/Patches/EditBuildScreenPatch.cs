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
                __instance.method_41(onlyAvailableTicked);
            }
            else if (onlytraders.GetComponent<Toggle>().isOn)
            {
                script.UpdateModView();
            }
            else
                __instance.method_41(false);
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
            Globals.itemsOnGun = new string[0];
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
            bool assembled = await __result;

            if (assembled)
            {
                Globals.script.UpdateModView();
            }
        }
    }
}
