using BepInEx;
using ChooChooTraderModding.Config;

namespace ChooChooTraderModding
{
	[BepInPlugin("choo.choo.tradermodding", "Choo² Trader Modding", "1.3.2")]
	public class TraderModdingPlugin : BaseUnityPlugin
	{
		private void Awake()
		{
			TraderModdingConfig.InitConfig(Config);

            new EditBuildScreenPatch().Enable();
			new EditBuildScreenShowPatch().Enable();
			new EditBuildScreenClosePatch().Enable();
			new EditBuildScreenAssembledWeaponPatch().Enable();
			new DropDownPatch().Enable();
			new DropDownPatchDeleteOverlays().Enable();
			new ModdingScreenSlotViewPatch().Enable();
		}
	}
}
