using Comfort.Common;
using System.Collections.Generic;
using System.Linq;
using System;
using EFT;
using EFT.UI;
using EFT.InventoryLogic;
using UnityEngine;
using ChooChooTraderModding.Config;
using TMPro;
using EFT.Weather;

namespace ChooChooTraderModding
{
    public class TraderModdingOnlyScript : MonoBehaviour
    {
        public EditBuildScreen __instance;
        public Item weaponBody = null;

        TraderData traderData;

        public void ToggleTradersOnlyView(bool tradersOnly)
        {
            TraderModdingUtils.ClearBuyAndDetachItems();

            if (tradersOnly && Globals.checkbox_availableOnly_toggle != null && Globals.checkbox_availableOnly_toggle.isOn)
            {
                Globals.checkbox_availableOnly_toggle.SetIsOnWithoutNotify(false);
            }

            UpdateModView();
        }        
        
        public void ToggleOnlyAvailableView(bool onlyAvailable)
        {
            TraderModdingUtils.ClearBuyAndDetachItems();

            if (onlyAvailable && Globals.checkbox_traderOnly_toggle != null && Globals.checkbox_traderOnly_toggle.isOn)
            {
                Globals.checkbox_traderOnly_toggle.SetIsOnWithoutNotify(false);
            }

            __instance.method_41(onlyAvailable);
        }

        bool IsLooseItem(Item item)
        {
            return (item.CurrentAddress is GClass2769 && item.Parent.Container.ParentItem.IsContainer);
        }

        bool IsItemUsable(Item itemToCheck)
        {
            if (itemToCheck == null)
                return false;

            if (__instance.InventoryController.IsItemEquipped(itemToCheck))
                return false;

            if (itemToCheck.IsChildOf(weaponBody))
                return true;

            if (itemToCheck is Weapon)
                return false;

            return IsLooseItem(itemToCheck);
        }

        public void UpdateModView()
        {
            if (__instance == null)
                return;

            // If we only want to see available mods the BSG way (kekw), do nothing
            if (__instance.enabled && Globals.checkbox_availableOnly_toggle != null && Globals.checkbox_availableOnly_toggle.isOn) 
                return;

            // Can't get the checkbox for some reason
            if (Globals.checkbox_traderOnly_toggle == null)
                return;

            // Get the player profile and build inv controller class
            if (FieldInfos.EditBuildScreen_profile_0 == null) { ConsoleScreen.LogError("FieldInfo for profile == null"); return; }

            Profile profile = (Profile)FieldInfos.EditBuildScreen_profile_0.GetValue(__instance);
            if (profile == null) { ConsoleScreen.LogError("profile == null"); return; }

            InventoryControllerClass inventoryControllerClass = new InventoryControllerClass(profile, false, null);

            // Get all mods that exist
            ItemFactory itemFactory = Singleton<ItemFactory>.Instance;
            if (itemFactory == null)
                return;

            Item[] allmods = itemFactory.CreateAllModsEver();

            // Get all usable mods on the player and not on any weapon
            Item[] playeritems_usable_mods = { };
            List<string> allmods_player = GetItems_Player(ref playeritems_usable_mods, TraderModdingConfig.ShowAttachedItems.Value);

            StashClass stashClass = itemFactory.CreateFakeStash(null);
            stashClass.Grids[0] = new GClass2500(Guid.NewGuid().ToString(), 30, 1, true, Array.Empty<ItemFilter>(), stashClass);
            TraderControllerClass traderControllerClass = new TraderControllerClass(stashClass, "here lies profile id", Guid.NewGuid().ToString(), false, EOwnerType.Profile, null, null);
            
            if (traderData == null)
            {
                ConsoleScreen.LogError("Couldn't get traderdata, proceeding without it (Almost all features won't work. Please report this to me)");
                foreach (Item item in allmods)
                {
                    item.StackObjectsCount = item.StackMaxSize;
                    stashClass.Grid.Add(item);
                }
            }
            else
            {
                foreach (Item item in allmods)
                {
                    if (Globals.checkbox_traderOnly_toggle.isOn)
                    {
                        bool traderHasMod = traderData.modsAndCosts.Any(mod => mod.tpl == item.TemplateId);
                        if (!(
                            (TraderModdingConfig.InvertTraderSelection.Value ? !traderHasMod : traderHasMod) ||
                            Globals.itemsOnGun.Contains(item.TemplateId) ||
                            allmods_player.Contains(item.TemplateId)))
                            continue;
                    }

                    item.StackObjectsCount = item.StackMaxSize;
                    stashClass.Grid.Add(item);
                }
            }


            GClass2830 manip = new GClass2830(inventoryControllerClass, new LootItemClass[] { (LootItemClass)traderControllerClass.RootItem }, playeritems_usable_mods);
            __instance.UpdateManipulation(manip);
            __instance.RefreshWeapon();
        }

        public void GetTraderItems()
        {
            // Get all mods available from traders
            traderData = TraderModdingUtils.GetData(false);
            Globals.dollars_to_rubles = traderData.dollar_to_ruble;
            Globals.euros_to_rubles = traderData.euro_to_ruble;

            Globals.traderModsTplCost.Clear();
            foreach (ModAndCost mod in traderData.modsAndCosts)
            {

                Globals.traderModsTplCost[mod.tpl] = mod.cost;


                // I converted to the lowest price before to show these,
                // but when assembling the gun, and searching for the mods ->
                // BSG just shows you the last found entry apparently, might be more expensive,
                // so we will show these as well instead of the cheapest....


                //try
                //{
                //    Globals.traderModsTplCost.Add(mod.tpl, mod.cost);
                //}
                //// Price already in list, check if lower after conversion
                //catch (ArgumentException e)
                //{
                //    Globals.traderModsTplCost[mod.tpl] = mod.cost;

                //    //// Convert the existring cost to rubles
                //    //string existingCostString = Globals.traderModsTplCost[mod.tpl];
                //    //int existingCostAmount = 0;
                //    //try { existingCostAmount = Int32.Parse(existingCostString.Substring(0, existingCostString.Length - 1)); } catch { continue; }

                //    //int existingCostRubles = 0;
                //    //char currencyExistingCost = existingCostString.Last<char>();
                //    //if (currencyExistingCost == 'r')
                //    //    existingCostRubles = existingCostAmount;
                //    //else if (currencyExistingCost == 'd')
                //    //    existingCostRubles = existingCostAmount * traderData.dollar_to_ruble;
                //    //else if (currencyExistingCost == 'e')
                //    //    existingCostRubles = existingCostAmount * traderData.euro_to_ruble;


                //    //// Get the cost for the mod we just tried to add again, then see if lower, if so -> update cost
                //    //int newCostAmount = 0;
                //    //try { newCostAmount = Int32.Parse(mod.cost.Substring(0, mod.cost.Length - 1)); } catch { continue; }

                //    //int newCostRubles = 0;
                //    //char currencyNewCost = mod.cost.Last<char>();
                //    //if (currencyNewCost == 'r')
                //    //    newCostRubles = newCostAmount;
                //    //else if (currencyNewCost == 'd')
                //    //    newCostRubles = newCostAmount * traderData.dollar_to_ruble;
                //    //else if (currencyNewCost == 'e')
                //    //    newCostRubles = newCostAmount * traderData.euro_to_ruble;

                //    //if (mod.tpl == "57cffb66245977632f391a99")
                //    //{
                //    //    ConsoleScreen.LogError("Magpul M-LOK old:" + existingCostString);
                //    //    ConsoleScreen.LogError("Magpul M-LOK new:" + mod.cost);
                //    //}


                //    //if (newCostRubles < existingCostRubles)
                //    //    Globals.traderModsTplCost[mod.tpl] = mod.cost;
                //}
            }
        }

        List<string> GetItems_Player(ref Item[] playeritems_usable_mods, bool showAttachedItems)
        {
            if (showAttachedItems)
                playeritems_usable_mods = __instance.InventoryController.Inventory.GetPlayerItems(EPlayerItems.All).ToArray<Item>();
            else
                playeritems_usable_mods = __instance.InventoryController.Inventory.GetPlayerItems(EPlayerItems.All).Where(IsItemUsable).ToArray<Item>();

            return playeritems_usable_mods.Select(mod => mod.TemplateId).ToList();
        }

        public void GetItemsInUse()
        {
            var allPlayerItems = __instance.InventoryController.Inventory.GetPlayerItems(EPlayerItems.All);
            var looseItemsPlayer = allPlayerItems.Where(IsItemUsable).Select(mod => mod.TemplateId).ToList();

            Globals.itemsInUse_realItem = allPlayerItems.
                Where(item => !looseItemsPlayer.Contains(item.TemplateId)).ToList();

            Globals.itemsInUse = Globals.itemsInUse_realItem.Select(mod => mod.TemplateId).ToArray();
            Globals.itemsAvailable = looseItemsPlayer.ToArray();
        }

        public void GetItemsInUseNotPurchasable()
        {
            Globals.itemsInUseNonBuyable = Globals.itemsInUse.Where(item => !traderData.modsAndCosts.Any(mod => mod.tpl == item)).ToArray();
        }

        public void GetItemsOnGun()
        {
            if (weaponBody == null) { ConsoleScreen.LogError("Couldn't get items on gun, weaponBody == null"); return; }

            // Get all mods that are already on the gun we are modding
            List<string> allmods_gun = weaponBody.GetAllItems().OfType<Mod>().Select(mod => mod.TemplateId).ToList();
            Globals.itemsOnGun = allmods_gun.ToArray();
        }

        public void UpdateBuildCostPanel()
        {
            if (Globals.buildCostPanelGO != null)
            {
                Globals.buildCostPanelGO.SetActive(TraderModdingConfig.ShowBuildCost.Value);
            }

            if (Globals.buildCostTextGO != null)
            {
                var panelText = Globals.buildCostTextGO.GetComponent<TextMeshProUGUI>();
                panelText.fontSize = TraderModdingConfig.BuildCostFontSize.Value;
            }
        }

        public void RefreshEverything(bool getTraderData = true)
        {
            GetItemsOnGun();

            if (getTraderData)
            {
                // Get the trader items
                GetTraderItems();
            }

            // Get items in use
            GetItemsInUse();

            // Get items in use that are not purchasable
            GetItemsInUseNotPurchasable();

            // Let's also fix BSG's bug that closing and reopening the modding screen can have the checkbox on without any effect
            if (Globals.checkbox_availableOnly_toggle.isOn)
            {
                __instance.method_41(true);
            }
            else if (Globals.checkbox_traderOnly_toggle.isOn)
            {
                UpdateModView();
            }
            else
            {
                __instance.method_41(false);
            }

            UpdateBuildCostPanel();
        }

        public void TryToDetachInUseItems()
        {
            foreach (string tplID in Globals.itemsToDetach)
            {
                Item realItem = Globals.itemsInUse_realItem.Find(item => item.TemplateId == tplID);

                if (realItem != null)
                {
                    string parentName = realItem.Parent.Container.ParentItem.ShortName.Localized(null);

                    // Try to move the item to stash
                    if (InteractionsHandlerClass.QuickFindAppropriatePlace(realItem, __instance.InventoryController, __instance.InventoryController.Inventory.Stash.ToEnumerable<StashClass>(), InteractionsHandlerClass.EMoveItemOrder.TryTransfer, false).Succeeded)
                    {
                        Globals.itemsToBuy.Remove(tplID);
                        NotificationManagerClass.DisplayMessageNotification("Detached " + realItem.ShortName.Localized(null) + " from " + parentName);
                    }
                    else
                    {
                        NotificationManagerClass.DisplayWarningNotification("Detaching of " + realItem.ShortName.Localized(null) + " from " + parentName + " failed!");
                    }
                }
            }

            // Don't know why I can't make the Assemble button see the changes in inventory without doing this...
            __instance.CreateBuildManipulation();

            // Overwrite the above manipulation with our own again, and update item states, at least dont get trader data again
            RefreshEverything(false);

            // Disable the detach button if all were detached succesfully
            if (Globals.itemsToBuy.Count == 0)
            {
                if (Globals.detachButtonCanvasGroup != null)
                    Globals.detachButtonCanvasGroup.alpha = 0.5f;
            }
        }
    }
}