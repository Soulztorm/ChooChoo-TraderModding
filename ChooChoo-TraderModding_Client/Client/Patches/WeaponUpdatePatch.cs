using SPT.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using System.Reflection;

namespace TraderModding
{
    public class WeaponUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(EditBuildScreen),
                x => x.Name == nameof(EditBuildScreen.WeaponUpdate));
        }

        [PatchPrefix]
        static void Prefix()
        {
            TraderModdingUtils.ClearBuyAndDetachItems();
        }
    }
}