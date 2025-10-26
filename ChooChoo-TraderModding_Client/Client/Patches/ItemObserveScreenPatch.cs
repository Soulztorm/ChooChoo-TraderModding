using System.Reflection;
using SPT.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using UnityEngine;
using EFT.UI.WeaponModding;

namespace TraderModding
{
    public class ItemObserveScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemObserveScreen<EditBuildScreen.GClass3881, EditBuildScreen>), nameof(ItemObserveScreen<EditBuildScreen.GClass3881, EditBuildScreen>.Update));
        }

        [PatchPostfix]
        public static void Postfix(ItemObserveScreen<EditBuildScreen.GClass3881, EditBuildScreen> __instance)
        {
            if (__instance == null)
                return;

            if (Globals.isOnModdingScreen)
            {
                if (FieldInfos.ItemObserveScreen_weaponPreview == null)
                    return;

                WeaponPreview wp = FieldInfos.ItemObserveScreen_weaponPreview.GetValue(__instance) as WeaponPreview;

                if (wp != null)
                {
                    if (wp.WeaponPreviewCamera == null)
                        return;

                    Transform transform = wp.WeaponPreviewCamera.transform;

                    if (transform == null)
                        return;

                    if (Input.mouseScrollDelta.y != 0)
                    {
                        float zoomAmount = Input.mouseScrollDelta.y * 0.1f;
                        if (transform.position.z + zoomAmount < -0.05)
                        {
                            transform.Translate(new Vector3(0f, 0f, zoomAmount));
                            __instance.UpdatePositions();
                        }
                    }
                }
            }
        }
    }


    public class ItemObserveScreenRefreshIconsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemObserveScreen<EditBuildScreen.GClass3881, EditBuildScreen>), nameof(ItemObserveScreen<EditBuildScreen.GClass3881, EditBuildScreen>.method_6));
        }

        [PatchPostfix]
        static void Postfix()
        {
            TraderModdingUtils.UpdateBuildCost();
        }
    }
}
