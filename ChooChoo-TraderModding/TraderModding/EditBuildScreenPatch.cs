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

            GameObject newCheckb = GameObject.Instantiate(onlyavail);
            newCheckb.name = "OnlyTraders";
            newCheckb.transform.SetParent(togglegroup.transform, false);
            LocalizedText labelText = newCheckb.GetComponentInChildren<LocalizedText>();
            labelText.LocalizationKey = "Use only trader items";

            TraderModdingOnlyScript script = newCheckb.AddComponent<TraderModdingOnlyScript>();
            script.__instance = __instance;

            newCheckb.GetComponent<Toggle>().onValueChanged.AddListener(new UnityAction<bool>(script.ToggleTradersOnlyView));
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

            // Let's also fix BSG's bug that closing and reopening the modding screen can have the checkbox on without any effect
            Toggle onlyAvailableCheckbox = onlyavailable.GetComponent<Toggle>();
            bool onlyAvailableTicked = onlyAvailableCheckbox.isOn;
            if (onlyAvailableTicked) 
                __instance.method_41(onlyAvailableTicked);
            else
                script.UpdateModView();
        }
    }

    public class EditBuildScreenUseAvailablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(EditBuildScreen),
                x => x.Name == nameof(EditBuildScreen.method_41));
        }

        [PatchPrefix]
        public static bool Prefix(EditBuildScreen __instance, bool arg)
        {
            // else check for our trader only checkbox, if this is off as well, we show all items anyway, same as this method would
            GameObject togglegroup = __instance.transform.Find("Toggle Group").gameObject;
            GameObject onlytraders = togglegroup.transform.Find("OnlyTraders").gameObject;

            TraderModdingOnlyScript script = onlytraders.GetComponent<TraderModdingOnlyScript>();
            script.useOnlyAvailable = arg;

            // If show only available items was toggled on, continue
            if (arg)
                return true;

            script.UpdateModView();
            return false;
        }
    }
}
