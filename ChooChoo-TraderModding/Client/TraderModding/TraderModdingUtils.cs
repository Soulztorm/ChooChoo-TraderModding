using System.Threading.Tasks;
using Aki.Common.Http;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
using ChooChooTraderModding.Config;
using EFT.UI;

namespace ChooChooTraderModding
{
	public class TraderModdingUtils
	{
        public const string ruble_colorstring = "<color=#c4bc89> ₽</color>";
        public const string dollar_colorstring = "<color=#03d100> $</color>";
        public const string euro_colorstring = "<color=#0073de> €</color>";

        public static TraderData GetTraderMods()
		{
			string json = RequestHandler.GetJson("/choochoo-trader-modding/json");
			return JsonConvert.DeserializeObject<TraderData>(json);
		}

		public static TraderData GetData()
		{
            TraderData traderData = null;
			Task task = Task.Run(delegate
			{
                traderData = TraderModdingUtils.GetTraderMods();
			});
			task.Wait();

            //ConsoleScreen.Log("Price dollar: " + traderData.dollar_to_ruble.ToString());
            //ConsoleScreen.Log("Price euro: " + traderData.euro_to_ruble.ToString());
			return traderData;
		}

        public static void AddItemPriceTag(Transform parent, Item item, bool addToGlobalList = true)
        {
            // Find the info panel
            Transform infoPanel = parent.Find("Info Panel");

            // Now if we find a name and have a price for that item, add the pricetag
            Transform gunName = infoPanel.Find("Name");
            if (gunName != null)
            {
                Transform modTypeIcon = infoPanel.Find("Mod Type Icon");
                if (modTypeIcon != null)
                {
                    string costText;
                    if (Globals.traderModsTplCost.TryGetValue(item.TemplateId, out costText))
                    {
                        TransformPriceTextToColored(ref costText);

                        // Add a black background
                        GameObject colorPanel = parent.Find("Color Panel").gameObject;
                        GameObject itemPriceTagBackground = GameObject.Instantiate(colorPanel);
                        itemPriceTagBackground.name = "ItemPriceTagBG";
                        itemPriceTagBackground.transform.SetParent(modTypeIcon, false);
                        RectTransform bgRect = itemPriceTagBackground.GetComponent<RectTransform>();
                        bgRect.anchoredPosition = Vector2.zero;
                        bgRect.anchorMax = new Vector2(4.5f, 1f);
                        bgRect.anchorMin = new Vector2(1f, -0.1f);
                        bgRect.pivot = new Vector2(0.5f, 0.5f);
                        bgRect.sizeDelta = new Vector2(0.5f, 0.5f);
                        Image bgImage = itemPriceTagBackground.GetComponent<Image>();
                        bgImage.color = Color.black;

                        if (addToGlobalList)
                            Globals.itemsInUseOverlays.Add(itemPriceTagBackground);

                        // Create a copy of the name for the price tag
                        GameObject itemPriceTag = GameObject.Instantiate(gunName.gameObject);
                        itemPriceTag.transform.SetParent(modTypeIcon, false);
                        itemPriceTag.name = "ItemPriceTag";
                        itemPriceTag.transform.SetSiblingIndex(itemPriceTagBackground.transform.GetSiblingIndex() + 1);

                        RectTransform priceTagRect = itemPriceTag.GetComponent<RectTransform>();
                        priceTagRect.anchoredPosition = new Vector2(0, 0);
                        priceTagRect.anchorMin = new Vector2(1.2f, 0);
                        priceTagRect.anchorMax = new Vector2(50, 1);
                        priceTagRect.offsetMin = priceTagRect.offsetMax = priceTagRect.pivot = Vector2.zero;

                        if (addToGlobalList)
                            Globals.itemsInUseOverlays.Add(itemPriceTag);

                        // Finally set the price
                        CustomTextMeshProUGUI itemPriceText = itemPriceTag.GetComponent<CustomTextMeshProUGUI>();
                        itemPriceText.text = costText;


                        // Disable the not in eq icon
                        Transform NotInEquipmentIcon = infoPanel.Find("NotInEquipmentIcon");
                        if (NotInEquipmentIcon != null)
                            NotInEquipmentIcon.gameObject.SetActive(false);
                    }
                }
            }
        }

        public static void RemoveExistingPriceTag(Transform parent)
        {
            // If we already added a price tag, destroy that one first
            Transform existingPriceTag = parent.Find("Info Panel/Mod Type Icon/ItemPriceTag");
            if (existingPriceTag != null)
                GameObject.Destroy(existingPriceTag.gameObject);

            Transform existingPriceTagBG = parent.Find("Info Panel/Mod Type Icon/ItemPriceTagBG");
            if (existingPriceTagBG != null)
                GameObject.Destroy(existingPriceTagBG.gameObject);
        }

        public static bool GetColorForItem(Item item, ref Color color)
        {
            if (TraderModdingConfig.HighlightOnWeaponItems.Value && Globals.itemsOnGun.Contains(item.TemplateId))
                color = TraderModdingConfig.ColorOnWeapon.Value;
            else if (TraderModdingConfig.HighlightAttachedItems.Value && Globals.itemsInUseNonBuyable.Contains(item.TemplateId))
                color = TraderModdingConfig.ColorAttachedNonBuyable.Value;
            else if (TraderModdingConfig.HighlightAttachedItems.Value && Globals.itemsInUse.Contains(item.TemplateId))
                color = TraderModdingConfig.ColorAttached.Value;
            else if (TraderModdingConfig.HighlightUsableItems.Value && Globals.itemsAvailable.Contains(item.TemplateId))
                color = TraderModdingConfig.ColorUsable.Value;
            else
                return false;

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
