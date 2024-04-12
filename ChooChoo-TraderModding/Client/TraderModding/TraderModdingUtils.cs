using System.Threading.Tasks;
using Aki.Common.Http;
using Newtonsoft.Json;

namespace TraderModding
{
	public class TraderModdingUtils
	{
		public static ModAndCost[] GetTraderMods()
		{
			string json = RequestHandler.GetJson("/trader-modding/json");
			return JsonConvert.DeserializeObject<ModAndCost[]>(json);
		}

		public static ModAndCost[] GetData()
		{
            ModAndCost[] mods = null;
			Task task = Task.Run(delegate
			{
				mods = TraderModdingUtils.GetTraderMods();
			});
			task.Wait();
			return mods;
		}
	}

	public class ModAndCost
	{
		public string tpl;
		public string cost;
	}
}
