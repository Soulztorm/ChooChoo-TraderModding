using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChooChooTraderModding
{
    internal static class Globals
    {
        public static bool isOnModdingScreen = false;

        // The main script
        public static TraderModdingOnlyScript script = null;

        // Trader data
        public static Dictionary<string, string> traderModsTplCost = new Dictionary<string, string>();
        public static int dollars_to_rubles = 0;
        public static int euros_to_rubles = 0;

        // Items in use etc.
        public static string[] itemsInUse = new string[0];
        public static List<Item> itemsInUse_realItem = new List<Item>();
        public static string[] itemsInUseNonBuyable = new string[0];
        public static string[] itemsAvailable = new string[0];
        public static string[] itemsOnGun = new string[0];

        public static List<string> itemsToBuy = new List<string>();
        public static List<string> itemsToDetach = new List<string>();

        // Gameobjects
        public static Toggle checkbox_availableOnly_toggle = null;
        public static Toggle checkbox_traderOnly_toggle = null;
        public static LocalizedText checkbox_traderOnly_text = null;
       
        public static Dictionary<ModdingSelectableItemView, List<GameObject>> addedPriceTags = new Dictionary<ModdingSelectableItemView, List<GameObject>>();

        public static List<GameObject> itemsInUseOverlays = new List<GameObject>();

        public static GameObject buildCostPanelGO = null;
        public static GameObject buildCostTextGO = null;

        public static CanvasGroup detachButtonCanvasGroup = null;
    }
}