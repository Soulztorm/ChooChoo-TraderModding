using System.Reflection;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using ChooChooTraderModding.Config;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.PlayerLoop;
using EFT.UI.WeaponModding;
using Comfort.Common;

namespace ChooChooTraderModding
{
    public class ItemObserveScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemObserveScreen<EditBuildScreen.GClass3126, EditBuildScreen>), nameof(ItemObserveScreen<EditBuildScreen.GClass3126, EditBuildScreen>.Update));
        }

        [PatchPrefix]
        public static void Prefix(ItemObserveScreen<EditBuildScreen.GClass3126, EditBuildScreen> __instance)
        {
            if (Globals.isOnModdingScreen)
            {
                WeaponPreview wp = AccessTools.Field(__instance.GetType(), "_weaponPreview").GetValue(__instance) as WeaponPreview;

                if (wp != null)
                {
                    Transform transform = wp.WeaponPreviewCamera.transform;

                    if (Input.mouseScrollDelta.y != 0)
                    {
                        float zoomAmount = Input.mouseScrollDelta.y * 0.1f;
                        if (transform.position.z + zoomAmount < -0.05)
                        {
                            transform.Translate(new Vector3(0f, 0f, zoomAmount));
                            __instance.UpdatePositions();
                        }
                    }


                    //// Mouse wheel was not pressed, but now it is
                    //if (Input.GetMouseButton(2))
                    //{
                    //    ConsoleScreen.Log("MousewheelDown");
                    //    if (!Globals.isMiddleMouseDown)
                    //    {
                    //        Globals.isMiddleMouseDown = true;
                    //        Globals.lastMousePosDown = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                    //        Globals.lastRotatorRot = wp.Rotator.rotation;
                    //    }
                    //}
                    //else
                    //    Globals.isMiddleMouseDown = false;
                }
            }
        }
    }


    public class ItemObserveScreenRefreshIconsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemObserveScreen<EditBuildScreen.GClass3126, EditBuildScreen>), nameof(ItemObserveScreen<EditBuildScreen.GClass3126, EditBuildScreen>.method_6));
        }

        [PatchPostfix]
        static void Postfix()
        {
            TraderModdingUtils.UpdateBuildCost();
        }
    }



    //public class ItemObserveScreenPatchPost : ModulePatch
    //{
    //    protected override MethodBase GetTargetMethod()
    //    {
    //        return AccessTools.Method(typeof(ItemObserveScreen<EditBuildScreen.GClass3126, EditBuildScreen>), nameof(ItemObserveScreen<EditBuildScreen.GClass3126, EditBuildScreen>.Update));
    //    }

    //    [PatchPostfix]
    //    public static void Postfix(ItemObserveScreen<EditBuildScreen.GClass3126, EditBuildScreen> __instance)
    //    {
    //        if (Globals.isOnModdingScreen)
    //        {
    //            WeaponPreview wp = AccessTools.Field(__instance.GetType(), "_weaponPreview").GetValue(__instance) as WeaponPreview;

    //            if (wp != null)
    //            {
    //                if (Globals.isMiddleMouseDown)
    //                {
    //                    Vector3 deltaMouse = new Vector3((Input.mousePosition.x - Globals.lastMousePosDown.x) * 0.0001f, (Input.mousePosition.y - Globals.lastMousePosDown.y) * 0.0001f, 0.0f);
    //                    ConsoleScreen.Log("Delta " + deltaMouse.x.ToString() + ", " + deltaMouse.y.ToString());

    //                    Vector3 newPos = wp.Rotator.position;
    //                    newPos.x = deltaMouse.x;
    //                    newPos.y = deltaMouse.y;
    //                    wp.Rotator.position = newPos;
    //                    wp.Rotator.rotation = Globals.lastRotatorRot;
    //                    __instance.UpdatePositions();
    //                }
    //            }
    //        }
    //    }
    //}
}
