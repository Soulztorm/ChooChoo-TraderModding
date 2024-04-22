using EFT.UI;
using System.Collections.Generic;
using UnityEngine;

namespace ChooChooTraderModding
{
    public static class Globals
    {
        public static bool isOnModdingScreen = false;

        public static Dictionary<string, string> traderModsTplCost = new Dictionary<string, string>();
        public static int dollars_to_rubles = 0;
        public static int euros_to_rubles = 0;

        public static string[] itemsInUse = new string[0];
        public static string[] itemsInUseNonBuyable = new string[0];
        public static string[] itemsAvailable = new string[0];
        public static string[] itemsOnGun = new string[0];
        public static List<GameObject> itemsInUseOverlays = new List<GameObject>();

        public static LocalizedText traderOnlyCheckboxText = null;
        public static TraderModdingOnlyScript script = null;

        public static GameObject buildCostPanelGO = null;
        public static GameObject buildCostTextGO = null;

        public static List<string> itemsToBuy = new List<string>();

        //public static Vector2 lastMousePosDown = Vector2.zero;
        //public static bool isMiddleMouseDown = false;

        //public static Quaternion lastRotatorRot = Quaternion.identity;
    }
}