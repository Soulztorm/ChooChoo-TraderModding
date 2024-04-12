using EFT.UI;
using System.Collections.Generic;
using UnityEngine;

namespace TraderModding
{
    public static class Globals
    {
       public static Dictionary<string, string> traderModsTplCost = new Dictionary<string, string>();

        public static string[] itemsInUse = new string[0];
        public static string[] itemsInUseNonBuyable = new string[0];
        public static string[] itemsAvailable = new string[0];
        public static List<GameObject> itemsInUseOverlays = new List<GameObject>();

        public static LocalizedText traderOnlyCheckboxText = null;
        public static TraderModdingOnlyScript script = null;
    }
}