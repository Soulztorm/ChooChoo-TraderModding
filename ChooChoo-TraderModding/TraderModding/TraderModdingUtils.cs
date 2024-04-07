using System.Threading.Tasks;
using Aki.Common.Http;
using Newtonsoft.Json;

namespace TraderModding
{
	public class TraderModdingUtils
	{
		public static string[] GetTraderMods()
		{
			string json = RequestHandler.GetJson("/trader-modding/json");
			return JsonConvert.DeserializeObject<string[]>(json);
		}

		public static string[] GetData()
		{
			string[] mods = null;
			Task task = Task.Run(delegate
			{
				mods = TraderModdingUtils.GetTraderMods();
			});
			task.Wait();
			return mods;
		}
	}
}
