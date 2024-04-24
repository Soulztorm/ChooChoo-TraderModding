using System.Threading.Tasks;
using Aki.Common.Http;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
using ChooChooTraderModding.Config;
using EFT.UI;
using System;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System.Reflection;
using TMPro;
using JetBrains.Annotations;
using System.Collections.Generic;

namespace ChooChooTraderModding
{
    public class TraderModdingUtils
    {
        public const string ruble_colorstring = "<color=#c4bc89> ₽</color>";
        public const string dollar_colorstring = "<color=#03d100> $</color>";
        public const string euro_colorstring = "<color=#0073de> €</color>";

        public const string build_cost_header = "-  Build Cost  -\n‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾";

        public static TraderData GetTraderMods(bool flea)
        {
            string json = flea ? RequestHandler.GetJson("/choochoo-trader-modding/json-flea") : RequestHandler.GetJson("/choochoo-trader-modding/json");
            return JsonConvert.DeserializeObject<TraderData>(json);
        }

        public static TraderData GetData(bool flea)
        {
            TraderData traderData = null;
            Task task = Task.Run(delegate
            {
                traderData = TraderModdingUtils.GetTraderMods(flea);
            });
            task.Wait();

            //ConsoleScreen.Log("Price dollar: " + traderData.dollar_to_ruble.ToString());
            //ConsoleScreen.Log("Price euro: " + traderData.euro_to_ruble.ToString());
            return traderData;
        }

        public static void AddItemPriceTag(ModdingSelectableItemView modItemView, Item item, bool addToGlobalList = true)
        {
            TextMeshProUGUI caption = (TextMeshProUGUI) DropDownPatch.GridItemView_Caption.GetValue(modItemView);
            ItemViewStats itemViewStats = (ItemViewStats) DropDownPatch.GridItemView_itemViewStats.GetValue(modItemView);
            if (itemViewStats == null) { ConsoleScreen.LogError("Couldn't add price tag (itemViewStats == null)"); return; }

            Image modTypeIcon = (Image) DropDownPatch.ItemViewStats_modTypeIcon.GetValue(itemViewStats);

            if (caption != null && modTypeIcon != null)
            {
                string costText;
                if (Globals.traderModsTplCost.TryGetValue(item.TemplateId, out costText))
                {
                    TransformPriceTextToColored(ref costText);

                    // Add a black background
                    Image colorPanel = (Image) DropDownPatch.ItemView_ColorPanel.GetValue(modItemView);

                    GameObject itemPriceTagBackground = GameObject.Instantiate(colorPanel.gameObject);
                    itemPriceTagBackground.name = "ItemPriceTagBG2";
                    itemPriceTagBackground.transform.SetParent(modTypeIcon.transform, false);
                    RectTransform bgRect = itemPriceTagBackground.GetComponent<RectTransform>();
                    bgRect.anchoredPosition = Vector2.zero;
                    bgRect.anchorMax = new Vector2(4.5f, 1f);
                    bgRect.anchorMin = new Vector2(1f, -0.1f);
                    bgRect.pivot = new Vector2(0.5f, 0.5f);
                    bgRect.sizeDelta = new Vector2(0.5f, 0.5f);
                    Image bgImage = itemPriceTagBackground.GetComponent<Image>();
                    bgImage.color = Color.black;

                    // Create a copy of the name for the price tag
                    GameObject itemPriceTag = GameObject.Instantiate(caption.gameObject);
                    itemPriceTag.transform.SetParent(modTypeIcon.transform, false);
                    itemPriceTag.name = "ItemPriceTag";
                    itemPriceTag.transform.SetSiblingIndex(itemPriceTagBackground.transform.GetSiblingIndex() + 1);

                    RectTransform priceTagRect = itemPriceTag.GetComponent<RectTransform>();
                    priceTagRect.anchoredPosition = new Vector2(0, 0);
                    priceTagRect.anchorMin = new Vector2(1.2f, 0);
                    priceTagRect.anchorMax = new Vector2(50, 1);
                    priceTagRect.offsetMin = priceTagRect.offsetMax = priceTagRect.pivot = Vector2.zero;

                    // Finally set the price
                    CustomTextMeshProUGUI itemPriceText = itemPriceTag.GetComponent<CustomTextMeshProUGUI>();
                    itemPriceText.text = costText;

                    if (addToGlobalList)
                    {
                        Globals.itemsInUseOverlays.Add(itemPriceTag);
                        Globals.itemsInUseOverlays.Add(itemPriceTagBackground);
                    }
                    else
                    {
                        AddPriceTagNoGlobalCleanup(modItemView, itemPriceTag, itemPriceTagBackground);
                    }


                    // Disable the not in eq icon
                    GameObject NotInEquipmentIcon = (GameObject)DropDownPatch.ModdingSelectableItemView_NotInEquipmentIcon.GetValue(modItemView);
                    if (NotInEquipmentIcon != null)
                        NotInEquipmentIcon.SetActive(false);
                }
            }
        }

        private static void AddPriceTagNoGlobalCleanup(ModdingSelectableItemView modItemView, GameObject itemPriceTag, GameObject itemPriceTagBG)
        {
            List<GameObject> list = new List<GameObject>();
            list.Add(itemPriceTag);
            list.Add(itemPriceTagBG);
            Globals.addedPriceTags[modItemView] = list;
        }

        public static void RemoveExistingPriceTag(ModdingSelectableItemView modItemView)
        {
            List<GameObject> priceTagGOs = new List<GameObject>();
            if(Globals.addedPriceTags.TryGetValue(modItemView, out priceTagGOs))
            {
                foreach(GameObject priceTag in priceTagGOs)
                    GameObject.Destroy(priceTag);

                Globals.addedPriceTags[modItemView].Clear();
            }
        }

        public static bool GetColorForItem(Item item, ref Color color, ref bool needsBuying)
        {
            if (TraderModdingConfig.HighlightOnWeaponItems.Value && Globals.itemsOnGun.Contains(item.TemplateId))
                color = TraderModdingConfig.ColorOnWeapon.Value;
            else if (TraderModdingConfig.HighlightAttachedItems.Value && Globals.itemsInUseNonBuyable.Contains(item.TemplateId))
                color = TraderModdingConfig.ColorAttachedNonBuyable.Value;
            else if (TraderModdingConfig.HighlightAttachedItems.Value && Globals.itemsInUse.Contains(item.TemplateId))
            {
                color = TraderModdingConfig.ColorAttached.Value;
                needsBuying = true;
            }
            else if (TraderModdingConfig.HighlightUsableItems.Value && Globals.itemsAvailable.Contains(item.TemplateId))
                color = TraderModdingConfig.ColorUsable.Value;
            else
            {
                needsBuying = true;
                return false;
            }

            return true;
        }

        public static void TransformPriceTextToColored(ref string priceText)
        {
            char currency = priceText.Last<char>();

            if (currency == 'r')
                priceText = priceText.Substring(0, priceText.Length - 1) + ruble_colorstring;
            else if (currency == 'd')
                priceText = priceText.Substring(0, priceText.Length - 1) + dollar_colorstring;
            else if (currency == 'e')
                priceText = priceText.Substring(0, priceText.Length - 1) + euro_colorstring;
        }

        public static void UpdateBuildCost()
        {
            if (!Globals.isOnModdingScreen || Globals.buildCostTextGO == null)
                return;

            // Sum up all prices
            int amount_rubles = 0;
            int amount_dollars = 0;
            int amount_euros = 0;

            foreach (var itemToBuy in Globals.itemsToBuy)
            {
                string itemCost = "";
                if (!Globals.traderModsTplCost.TryGetValue(itemToBuy, out itemCost))
                    continue;

                int amount = 0;
                try
                {
                    amount = Int32.Parse(itemCost.Substring(0, itemCost.Length - 1));
                }
                catch { continue; }

                char currency = itemCost.Last<char>();
                if (currency == 'r')
                    amount_rubles += amount;
                else if (currency == 'd')
                    amount_dollars += amount;
                else if (currency == 'e')
                    amount_euros += amount;
            }


            string final_text = build_cost_header;

            if (amount_dollars > 0)
            {
                string dollars_text = amount_dollars.ToString() + "d";
                TransformPriceTextToColored(ref dollars_text);
                final_text += "\n" + dollars_text;
            }
            if (amount_euros > 0)
            {
                string euros_text = amount_euros.ToString() + "e";
                TransformPriceTextToColored(ref euros_text);
                final_text += (amount_dollars == 0 ? "\n" : "\n+ ") + euros_text;
            }
            if (amount_rubles > 0)
            {
                string rubles_text = amount_rubles.ToString() + "r";
                TransformPriceTextToColored(ref rubles_text);
                final_text += ((amount_dollars == 0 && amount_euros == 0) ? "\n" : "\n+ ") + rubles_text;
            }



            int amount_total_rubles = amount_rubles + amount_dollars * Globals.dollars_to_rubles + amount_euros * Globals.euros_to_rubles;
            if (amount_total_rubles > 0 && (amount_dollars > 0 || amount_euros > 0))
            {
                string total_rubles_text = amount_total_rubles.ToString() + "r";
                TransformPriceTextToColored(ref total_rubles_text);
                final_text += "\n‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾\n" + "= " + total_rubles_text;
            }


            var buildCostText = Globals.buildCostTextGO.GetComponent<CustomTextMeshProUGUI>();
            buildCostText.text = final_text;
        }
    }


	public class ModAndCost
	{
		public string tpl;
		public string cost;
	}

    public class TraderData
    {
        public int dollar_to_ruble;
        public int euro_to_ruble;
        public ModAndCost[] modsAndCosts;
    }
}
