using Comfort.Common;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System;
using EFT;
using EFT.UI;
using EFT.InventoryLogic;
using UnityEngine;
using UnityEngine.UI;
using ChooChooTraderModding.Config;

namespace ChooChooTraderModding
{
    public class TraderModdingOnlyScript : MonoBehaviour
    {
        public EditBuildScreen __instance;
        public Toggle onlyAvailableToggle;
        public Toggle onlyTradersToggle;
        public Item weaponBody = null;

        TraderData traderData;

        public void ToggleTradersOnlyView(bool tradersOnly)
        {
            if (tradersOnly && onlyAvailableToggle.isOn)
            {
                onlyAvailableToggle.SetIsOnWithoutNotify(false);
            }

            UpdateModView();
        }        
        
        public void ToggleOnlyAvailableView(bool onlyAvailable)
        {
            if (onlyAvailable && onlyTradersToggle.isOn)
            {
                onlyTradersToggle.SetIsOnWithoutNotify(false);
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
            // If we only want to see available mods the BSG way (kekw), do nothing
            if (__instance.enabled && onlyAvailableToggle.isOn) 
                return;

            // Get all mods that exist
            ItemFactory itemFactory = Singleton<ItemFactory>.Instance;
            Item[] allmods = itemFactory.CreateAllModsEver();

            // Get the player profile and build inv controller class
            Profile profile = (Profile)AccessTools.Field(typeof(EditBuildScreen), "profile_0").GetValue(__instance);
            InventoryControllerClass inventoryControllerClass = new InventoryControllerClass(profile, false, null);

            // Get all usable mods on the player and not on any weapon
            Item[] playeritems_usable_mods = { };
            List<string> allmods_player = GetItems_Player(ref playeritems_usable_mods, TraderModdingConfig.ShowAttachedItems.Value);

            GetItemsOnGun();

            StashClass stashClass = itemFactory.CreateFakeStash(null);
            stashClass.Grids[0] = new GClass2500(Guid.NewGuid().ToString(), 30, 1, true, Array.Empty<ItemFilter>(), stashClass);
            TraderControllerClass traderControllerClass = new TraderControllerClass(stashClass, "here lies profile id", Guid.NewGuid().ToString(), false, EOwnerType.Profile, null, null);
            
            foreach (Item item in allmods)
            {
                if (onlyTradersToggle.isOn)
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

            GClass2830 manip = new GClass2830(inventoryControllerClass, new LootItemClass[] { (LootItemClass)traderControllerClass.RootItem }, playeritems_usable_mods);
            __instance.UpdateManipulation(manip);
            __instance.RefreshWeapon();
        }

        public void GetTraderItems()
        {
            // Get all mods available from traders
            traderData = TraderModdingUtils.GetData();

            Globals.traderModsTplCost.Clear();
            foreach (ModAndCost mod in traderData.modsAndCosts)
            {
                try
                {
                    Globals.traderModsTplCost.Add(mod.tpl, mod.cost);
                }
                catch (Exception e)
                {
                }
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

            var playerItemsInUse = allPlayerItems.
                Where(item => !looseItemsPlayer.Contains(item.TemplateId)).
                Select(mod => mod.TemplateId).ToList();

            Globals.itemsInUse = playerItemsInUse.ToArray();
            Globals.itemsAvailable = looseItemsPlayer.ToArray();
        }

        public void GetItemsInUseNotPurchasable()
        {
            Globals.itemsInUseNonBuyable = Globals.itemsInUse.Where(item => !traderData.modsAndCosts.Any(mod => mod.tpl == item)).ToArray();
        }

        public void GetItemsOnGun()
        {
            // Get all mods that are already on the gun we are modding
            List<string> allmods_gun = weaponBody.GetAllItems().OfType<Mod>().Select(mod => mod.TemplateId).ToList();
            Globals.itemsOnGun = allmods_gun.ToArray();
        }
    }
}