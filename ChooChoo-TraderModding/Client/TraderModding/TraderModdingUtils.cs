using System.Threading.Tasks;
using SPT.Common.Http;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
using ChooChooTraderModding.Config;
using EFT.UI;
using System;
using EFT.UI.DragAndDrop;
using TMPro;
using System.Collections.Generic;
using System.Globalization;

namespace ChooChooTraderModding
{
    internal class TraderModdingUtils
    {
        public static CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("de-de");

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
            TextMeshProUGUI caption = (TextMeshProUGUI)FieldInfos.GridItemView_Caption.GetValue(modItemView);
            ItemViewStats itemViewStats = (ItemViewStats)FieldInfos.GridItemView_itemViewStats.GetValue(modItemView);
            if (itemViewStats == null) { ConsoleScreen.LogError("Couldn't add price tag (itemViewStats == null)"); return; }

            Image modTypeIcon = (Image) FieldInfos.ItemViewStats_modTypeIcon.GetValue(itemViewStats);

            if (caption != null && modTypeIcon != null)
            {
                if (Globals.traderModInfo.ContainsKey(item.TemplateId))
                {
                    string costText = Globals.traderModInfo[item.TemplateId].cost_string;

                    if (TraderModdingConfig.AbbreviatePrices.Value)
                        TransformPriceTextToAbbreviated(ref costText);

                    TransformPriceTextToColored(ref costText);

                    bool isFleaItem = costText[0] == '0';
                    if (isFleaItem)
                        costText = costText.Substring(1);

                    // Add a black background
                    Image colorPanel = (Image)FieldInfos.ItemView_ColorPanel.GetValue(modItemView);

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
                    bgImage.color = isFleaItem ? new Color(0.33f, 0.0f, 0.0f) : Color.black;

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
                    TextMeshProUGUI itemPriceText = itemPriceTag.GetComponent<TextMeshProUGUI>();
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
                    GameObject NotInEquipmentIcon = (GameObject)FieldInfos.ModdingSelectableItemView_NotInEquipmentIcon.GetValue(modItemView);
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

        public static bool GetColorForItem(Item item, ref Color color, ref bool needsBuying, ref bool itemNeedsToBeDetached)
        {
            if (TraderModdingConfig.HighlightOnWeaponItems.Value && Globals.itemsOnGun.Contains(item.TemplateId))
                color = TraderModdingConfig.ColorOnWeapon.Value;
            else if (TraderModdingConfig.HighlightAttachedItems.Value && Globals.itemsInUseNonBuyable.Contains(item.TemplateId))
            {
                color = TraderModdingConfig.ColorAttachedNonBuyable.Value;
                itemNeedsToBeDetached = true;
            }
            else if (TraderModdingConfig.HighlightAttachedItems.Value && Globals.itemsInUse.Contains(item.TemplateId))
            {
                color = TraderModdingConfig.ColorAttached.Value;
                needsBuying = true;
                itemNeedsToBeDetached = true;
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

        public static void TransformPriceTextToAbbreviated(ref string priceText)
        {
            string priceTextNoCurrency = priceText.Substring(0, priceText.Length - 1);

            int amount = Int32.Parse(priceTextNoCurrency);
            if (amount < 1000)
                return;

            bool isFleaItem = priceText[0] == '0';
            char currency = priceText.Last<char>();

            float amountInK = amount / 1000.0f;

            string finalString = isFleaItem ? "0" : "";
            finalString += amountInK.ToString("F1", cultureInfo) + 'k' + currency;
            priceText = finalString;
        }

        public static void TransformPriceTextToColored(ref string priceText)
        {
            string priceTextNoCurrency = priceText.Substring(0, priceText.Length - 1);

            char currency = priceText.Last<char>();

            if (currency == 'r')
                priceText = priceTextNoCurrency + ruble_colorstring;
            else if (currency == 'd')
                priceText = priceTextNoCurrency + dollar_colorstring;
            else if (currency == 'e')
                priceText = priceTextNoCurrency + euro_colorstring;
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
                if (!Globals.traderModInfo.ContainsKey(itemToBuy))
                    continue;

                string costString = Globals.traderModInfo[itemToBuy].cost_string;
                int amount = 0;
                try
                {
                    amount = Int32.Parse(costString.Substring(0, costString.Length - 1));
                }
                catch { continue; }

                char currency = costString.Last<char>();
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


            var buildCostText = Globals.buildCostTextGO.GetComponent<TextMeshProUGUI>();
            buildCostText.text = final_text;
        }

        public static void ClearBuyAndDetachItems()
        {
            Globals.itemsToBuy.Clear();
            Globals.itemsToDetach.Clear();

            if (Globals.detachButtonCanvasGroup != null)
                Globals.detachButtonCanvasGroup.alpha = 0.5f;
        }
    }
}
