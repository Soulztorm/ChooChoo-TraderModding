using System.Reflection;
using Aki.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TraderModding
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
            labelText.LocalizationKey = "Use only trader items";

            TraderModdingOnlyScript script = onlyTradersCheckbox.AddComponent<TraderModdingOnlyScript>();
            script.__instance = __instance;
            script.onlyAvailableToggle = onlyavail.GetComponent<Toggle>();
            script.onlyTradersToggle = onlyTradersCheckbox.GetComponent<Toggle>();
            script.onlyTradersToggle.isOn = true;

            script.onlyTradersToggle.onValueChanged.AddListener(new UnityAction<bool>(script.ToggleTradersOnlyView));

            // Replace original only available listener with ours
            script.onlyAvailableToggle.onValueChanged.RemoveAllListeners();
            script.onlyAvailableToggle.onValueChanged.AddListener(new UnityAction<bool>(script.ToggleOnlyAvailableView));
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
}