using System;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TraderModding
{
    internal class ModInfoData
    {
        public string tpl;
        public string cost;
        public int tidx;
        public string id;
        public int lp;
        public int lm;
    }
    internal class TraderData
    {
        public int dollar_to_ruble;
        public int euro_to_ruble;
        public string[] traderIDs;
        public ModInfoData[] modInfoData;
    }
    internal class ModInfo
    {
        public string trader_inventory_itemid;
        public string trader_id;
        public string cost_string;
        public int limit_current;
        public int limit_max;
    }
    internal static class Globals
    {
        public static bool isOnModdingScreen = false;

        // The main script
        public static TraderModdingScript script = null;
        
        // Profile
        public static Profile profile = null;

        // Trader data
        public static Dictionary<MongoID, ModInfo> traderModInfo = new Dictionary<MongoID, ModInfo>();
        public static int dollars_to_rubles = 0;
        public static int euros_to_rubles = 0;

        // Items in use etc.
        public static Item[] allmods = Array.Empty<Item>();
        public static MongoID[] itemsInUse = Array.Empty<MongoID>();
        public static List<Item> itemsInUse_realItem = new List<Item>();
        public static MongoID[] itemsInUseNonBuyable = Array.Empty<MongoID>();
        public static MongoID[] itemsAvailable = Array.Empty<MongoID>();
        public static MongoID[] itemsOnGun = Array.Empty<MongoID>();

        public static List<MongoID> itemsToBuy = new List<MongoID>();
        public static List<MongoID> itemsToDetach = new List<MongoID>();
        
        // Fake stash for the available items when modding
        public static StashItemClass fakestash = null;
        public static TraderControllerClass fakestashTraderController = null;

        // Gameobjects
        public static Toggle checkbox_availableOnly_toggle = null;
        public static Toggle checkbox_traderOnly_toggle = null;
        public static LocalizedText checkbox_traderOnly_text = null;
       
        public static Dictionary<ModdingSelectableItemView, List<GameObject>> addedPriceTags = new Dictionary<ModdingSelectableItemView, List<GameObject>>();

        public static List<GameObject> itemsInUseOverlays = new List<GameObject>();

        public static GameObject buildCostPanelGO = null;
        public static GameObject buildCostTextGO = null;

        public static CanvasGroup detachButtonCanvasGroup = null;
        public static CanvasGroup quickbuyButtonCanvasGroup = null;
    }
}