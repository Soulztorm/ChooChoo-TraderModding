using BepInEx;
using TraderModding;

namespace Plugin
{
	[BepInPlugin("com.ChooChoo.TraderModding", "Choo² Trader Modding", "0.7.0")]
	public class Plugin : BaseUnityPlugin
	{
		private void Awake()
		{
			new EditBuildScreenPatch().Enable();
			new EditBuildScreenShowPatch().Enable();
			new EditBuildScreenUseAvailablePatch().Enable();
		}
	}
}
