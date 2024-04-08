using BepInEx;
using TraderModding.Config;

namespace TraderModding
{
	[BepInPlugin("choo.choo.tradermodding", "Choo² Trader Modding", "1.3.0")]
	public class TraderModdingPlugin : BaseUnityPlugin
	{
		private void Awake()
		{
			TraderModdingConfig.InitConfig(Config);

            new EditBuildScreenPatch().Enable();
			new EditBuildScreenShowPatch().Enable();
			new DropDownPatch().Enable();
			new DropDownPatchDeleteOverlays().Enable();
		}
	}
}
