using Comfort.Common;
using System.Collections.Generic;
using System.Linq;
using System;
using EFT;
using EFT.UI;
using EFT.InventoryLogic;
using UnityEngine;
using TMPro;
using SPT.Reflection.Utils;
using EFT.Trading;
using System.Collections;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using TraderModding.Config;

namespace TraderModding
{
    public class TraderModdingScript : MonoBehaviour
    {
        public EditBuildScreen __instance;
        public ISession __session;
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
            return (item.CurrentAddress is ItemAddress && item.Parent.Container.ParentItem.IsContainer);
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
            
            // Get all usable mods on the player and not on any weapon
            Item[] playeritems_usable_mods = { };
            HashSet<MongoID> allmods_player = GetItems_Player(ref playeritems_usable_mods, TraderModdingConfig.ShowAttachedItems.Value);
            
            // Remove all items from fake stash to rebuild
            Globals.fakestash.Grid.RemoveAll();
            
            if (traderData == null)
            {
                ConsoleScreen.LogError("Couldn't get traderdata, proceeding without it (Almost all features won't work. Please report this to me)");
                foreach (Item item in Globals.allmods)
                {
                    Globals.fakestash.Grid.AddAnywhere(item, EErrorHandlingType.Throw);
                }
            }
            else
            {
                foreach (Item item in Globals.allmods)
                {
                    if (Globals.checkbox_traderOnly_toggle.isOn)
                    {
                        bool traderHasMod = Globals.traderModInfo.ContainsKey(item.TemplateId) && Globals.traderModInfo[item.TemplateId].cost_string[0] != '0';
                        if (!(
                            (TraderModdingConfig.InvertTraderSelection.Value ? !traderHasMod : traderHasMod) ||
                            Globals.itemsOnGun.Contains(item.TemplateId) ||
                            allmods_player.Contains(item.TemplateId)))
                            continue;
                    }
                    
                    Globals.fakestash.Grid.AddAnywhere(item, EErrorHandlingType.Throw);
                }
            }
            
            GClass3468 manip = new GClass3468(__instance.InventoryController, new CompoundItem[] { (CompoundItem)Globals.fakestashTraderController.RootItem }, playeritems_usable_mods, Globals.profile, __session.RagFair.Available);
            __instance.UpdateManipulation(manip);
            __instance.RefreshWeapon();
        }

        public void GetTraderItems()
        {
            // Get all mods available from traders
            traderData = TraderModdingUtils.GetData(TraderModdingConfig.ShowFleaPriceTags.Value);
            Globals.dollars_to_rubles = traderData.dollar_to_ruble;
            Globals.euros_to_rubles = traderData.euro_to_ruble;

            // I figured for estimating their price, somehow this is 132, no idea about euros, since there are so little items.
            const int bsg_dollars_to_rubles = 132;

            Globals.traderModInfo.Clear();

            foreach (ModInfoData mod in traderData.modInfoData)
            {    
                ModInfo modData = new ModInfo
                {
                    trader_inventory_itemid = mod.id,
                    trader_id = mod.tidx < 0 ? "" : traderData.traderIDs[mod.tidx],
                    cost_string = mod.cost,
                    limit_current = mod.lp,
                    limit_max = mod.lm
                };

                if (!Globals.traderModInfo.ContainsKey(mod.tpl))
                    Globals.traderModInfo.Add(mod.tpl, modData);
                else
                {
                    // Get the current best offer for this mod in the list
                    ModInfo currentMod = Globals.traderModInfo[mod.tpl];
                    
                    // Check for personal limit
                    bool oldLimitReached = currentMod.limit_max != 0 && currentMod.limit_current >= currentMod.limit_max;
                    if (oldLimitReached)
                    {
                        bool newLimitReached = mod.lm != 0 && mod.lp >= mod.lm;
                        // The current entry reached the current personal limit, but the new one doesn't. Replace the available offer with the new one
                        if (!newLimitReached)
                        {
                            Globals.traderModInfo[mod.tpl] = modData;
                            continue;
                        }
                    }
                    
                    // Convert the existing cost to rubles
                    string existingCostString = currentMod.cost_string;
                    int existingCostAmount = 0;
                    try { existingCostAmount = Int32.Parse(existingCostString.Substring(0, existingCostString.Length - 1)); } catch { continue; }

                    int existingCostRubles = 0;
                    char currencyExistingCost = existingCostString.Last<char>();
                    if (currencyExistingCost == 'r')
                        existingCostRubles = existingCostAmount;
                    else if (currencyExistingCost == 'd')
                        existingCostRubles = existingCostAmount * bsg_dollars_to_rubles;
                    else if (currencyExistingCost == 'e')
                        existingCostRubles = existingCostAmount * traderData.euro_to_ruble;


                    // Get the cost for the mod we just tried to add again, then see if lower, if so -> update cost
                    int newCostAmount = 0;
                    try { newCostAmount = Int32.Parse(mod.cost.Substring(0, mod.cost.Length - 1)); } catch { continue; }

                    int newCostRubles = 0;
                    char currencyNewCost = mod.cost.Last<char>();
                    if (currencyNewCost == 'r')
                        newCostRubles = newCostAmount;
                    else if (currencyNewCost == 'd')
                        newCostRubles = newCostAmount * bsg_dollars_to_rubles;
                    else if (currencyNewCost == 'e')
                        newCostRubles = newCostAmount * traderData.euro_to_ruble;


                    if (newCostRubles < existingCostRubles)         
                        Globals.traderModInfo[mod.tpl] = modData;
                }
            }
        }

        HashSet<MongoID> GetItems_Player(ref Item[] playeritems_usable_mods, bool showAttachedItems)
        {
            if (showAttachedItems)
                playeritems_usable_mods = __instance.InventoryController.Inventory.GetPlayerItems(EPlayerItems.AllExceptHideoutStashes).ToArray<Item>();
            else
                playeritems_usable_mods = __instance.InventoryController.Inventory.GetPlayerItems(EPlayerItems.AllExceptHideoutStashes).Where(IsItemUsable).ToArray<Item>();

            return new HashSet<MongoID>(playeritems_usable_mods.Select(mod => mod.TemplateId));
        }

        public void GetItemsInUse()
        {
            var allPlayerItems = __instance.InventoryController.Inventory.GetPlayerItems(EPlayerItems.AllExceptHideoutStashes);
            Globals.itemsAvailable = allPlayerItems.Where(IsItemUsable).Select(mod => mod.TemplateId).ToArray();

            Globals.itemsInUse_realItem = allPlayerItems.
                Where(item => !Globals.itemsAvailable.Contains(item.TemplateId)).ToList();

            Globals.itemsInUse = Globals.itemsInUse_realItem.Select(mod => mod.TemplateId).ToArray();
        }

        public void GetItemsInUseNotPurchasable()
        {
            Globals.itemsInUseNonBuyable = Globals.itemsInUse.Where(item => !traderData.modInfoData.Any(mod => mod.tpl == item)).ToArray();
        }

        public void GetItemsOnGun()
        {
            if (weaponBody == null) { ConsoleScreen.LogError("Couldn't get items on gun, weaponBody == null"); return; }

            // Get all mods that are already on the gun we are modding
            Globals.itemsOnGun = weaponBody.GetAllItems().OfType<Mod>().Select(mod => mod.TemplateId).ToArray();
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

        public async void TryToBuyItems()
        {
            if (Globals.itemsToBuy.Count == 0)
                return;

            ITraderInteractions traderInteractions = ClientAppUtils.GetMainApp().Session;

            Dictionary<char, MongoID> map_currency = new Dictionary<char, MongoID>()
            {
                { 'r',  "5449016a4bdc2d6f028b456f" },
                { 'd',  "5696686a4bdc2da3298b456a" },
                { 'e',  "569668774bdc2da2298b4568" }
            };

            // Get the trader for each item
            foreach (var itemToBuyMongoID in Globals.itemsToBuy)
            {
                ModInfo modInfo = Globals.traderModInfo[itemToBuyMongoID];
                // Skip flea items for now
                if (modInfo.trader_id == "")
                {
                    NotificationManagerClass.DisplayWarningNotification("Skipping flea item: " + itemToBuyMongoID.LocalizedShortName(), EFT.Communications.ENotificationDurationType.Long);
                    continue;
                }
                
                // Check if limit reached
                if (modInfo.limit_current >= modInfo.limit_max)
                {
                    NotificationManagerClass.DisplayWarningNotification(
                        itemToBuyMongoID.LocalizedShortName() + " limit reached (" + modInfo.limit_current + "/" + modInfo.limit_max + ")",
                        EFT.Communications.ENotificationDurationType.Long);
                    continue;
                }

                // Get the cost of the item from the cost string
                int cost_amount = 0;
                try { cost_amount = Int32.Parse(modInfo.cost_string.Substring(0, modInfo.cost_string.Length - 1)); }
                catch { continue; }

                // Get the money to pay with
                var money_stacks = __instance.InventoryController.Inventory.GetAllItemByTemplate(map_currency[modInfo.cost_string.Last()]).ToList();
                // If there is no stack of that currency, or not enough, you broke :)
                if (money_stacks.Count == 0 || money_stacks.Sum(item => item.StackObjectsCount) < cost_amount)
                {
                    NotificationManagerClass.DisplayWarningNotification("Not enough money for: " + itemToBuyMongoID.LocalizedShortName(), EFT.Communications.ENotificationDurationType.Long);
                    continue;
                }

                // Reference the money stacks (The backend seems to look for other stacks, so the first index is enough)
                TradingItemReference[] refs = { new EFT.Trading.TradingItemReference { Item = money_stacks[0], Count = cost_amount } };

                // Purchase from the selected trader with inventory item id, and the money references
                var buyItemTask = traderInteractions.ConfirmPurchase(modInfo.trader_id, modInfo.trader_inventory_itemid, 1, 0, refs);
                if (!buyItemTask.IsCompleted)
                    await buyItemTask;

                if (buyItemTask.Result.Succeed)
                {
                    // Count up the purchased limit, because server refresh comes later, track this offline to prevent purchasing multiple items if the stock limit is reached
                    Globals.traderModInfo[itemToBuyMongoID].limit_current++;
                    string costText = modInfo.cost_string;
                    TraderModdingUtils.TransformPriceTextToColored(ref costText);
                    NotificationManagerClass.DisplayMessageNotification("Bought " + itemToBuyMongoID.LocalizedShortName() + " for " + costText, EFT.Communications.ENotificationDurationType.Long);
                }
            }


            TraderModdingUtils.ClearBuyAndDetachItems();

            // Don't know why I can't make the Assemble button see the changes in inventory without doing this...
            __instance.CreateBuildManipulation();

            RefreshEverything(true);
        }

        public async void TryToDetachInUseItems()
        {
            // Nothing to detach.
            if (Globals.itemsToDetach.Count == 0)
                return;

            // Go through all the template IDs we would like to detach
            foreach (string tplID in Globals.itemsToDetach)
            {
                List<Item> listEquipped = new List<Item>();
                List<Item> listOnWeapons = new List<Item>();
                List<Item> listOnAnything = new List<Item>();
                Dictionary<MongoID, string> parentWeaponDict  = new Dictionary<MongoID, string>();

                // Find all possible items that we could detach for the given template id
                var allDetachPossibilities = Globals.itemsInUse_realItem.Where(item => item.TemplateId == tplID).ToList();
                foreach (var item in allDetachPossibilities)
                {
                    // Go through the parent hierarchy to find out if it is equipped, attached to weapons, or to something else
                    var parents = item.GetAllParentItems(true);
                    bool addedAsEquippedOrWeapon = false;
                    string weaponName = "";
                    
                    foreach (var parent in parents)
                    {
                        if (parent is Weapon)
                        {
                            if (__instance.InventoryController.IsItemEquipped(parent))
                            {
                                listEquipped.Add(item);
                            }
                            else
                            {
                                listOnWeapons.Add(item);
                            }
                            addedAsEquippedOrWeapon = true;
                            weaponName = parent.ShortName.Localized();
                            break;
                        }
                    }
                    if (!addedAsEquippedOrWeapon)
                    {
                        listOnAnything.Add(item);
                    }

                    // Track if / which weapon is the top level parent
                    parentWeaponDict[item.Id] = weaponName;
                }

                Item bestCandidateToDetach = null;
                bool bestCandidateIsEquipped = false;
                // First get an item that is attached to anything, mounts etc.
                if (listOnAnything.Count > 0)
                {
                    bestCandidateToDetach = listOnAnything[0];
                }
                // If there are none, remove from existing weapons
                else if (listOnWeapons.Count > 0)
                {
                    bestCandidateToDetach = listOnWeapons[0];
                }
                // Otherwise we won't detach it.
                else
                {
                    bestCandidateToDetach = listEquipped[0];
                    bestCandidateIsEquipped = true;
                }

             
                if (bestCandidateToDetach != null)
                {
                    string moveItemName = bestCandidateToDetach.ShortName.Localized();
                    string parentName = bestCandidateToDetach.Parent.Container.ParentItem.ShortName.Localized();
                    
                    if (!bestCandidateIsEquipped || TraderModdingConfig.DetachEquippedItems.Value)
                    {
                        // Try to move the item to stash
                        bool moveSuccess = false;
                        GStruct154<GInterface424> moveOperationSimulation = InteractionsHandlerClass.QuickFindAppropriatePlace(bestCandidateToDetach, __instance.InventoryController, __instance.InventoryController.Inventory.Stash.ToEnumerable<StashItemClass>(), InteractionsHandlerClass.EMoveItemOrder.TryTransfer, true);
                        if (moveOperationSimulation.Succeeded)
                        {
                            var moveItemTask = __instance.InventoryController.TryRunNetworkTransaction(moveOperationSimulation, null);
                            if (!moveItemTask.IsCompleted)
                            {
                                await moveItemTask;
                            }
                            moveSuccess = moveItemTask.Result.Succeed;
                        }
                        
                        // Modify the parent string to indicate if the toplevel parent was a weapon
                        string weaponString = parentWeaponDict[bestCandidateToDetach.Id];
                        if (!weaponString.IsNullOrEmpty())
                        {
                            if (parentName != weaponString)
                                parentName += " (" + weaponString + ")";
                        }

                        string detachText = TraderModdingUtils.ColorText(moveItemName, "c4bc89") + " from " +
                                            TraderModdingUtils.ColorText(parentName, "c4bc89");
                        if (moveSuccess)
                            NotificationManagerClass.DisplayMessageNotification("Detached " + detachText);
                        else
                            NotificationManagerClass.DisplayWarningNotification("Detaching of " + detachText + " failed!");
                    }
                    else
                    {
                        NotificationManagerClass.DisplayWarningNotification("Did not detach " + moveItemName + ", because it is equipped!");
                    }
                }
                else
                {
                    NotificationManagerClass.DisplayWarningNotification("Could not find any appropriate item to detach!");
                }
            }

            TraderModdingUtils.ClearBuyAndDetachItems();

            // Don't know why I can't make the Assemble button see the changes in inventory without doing this...
            __instance.CreateBuildManipulation();

            // Overwrite the above manipulation with our own again, and update item states, at least dont get trader data again
            RefreshEverything(false);
        }
    }
}