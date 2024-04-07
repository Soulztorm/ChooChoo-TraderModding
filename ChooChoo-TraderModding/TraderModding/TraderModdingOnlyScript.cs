using Comfort.Common;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System;
using EFT;
using EFT.UI;
using EFT.InventoryLogic;
using UnityEngine;
using System.Threading.Tasks;
using Aki.Common.Http;
using Newtonsoft.Json;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TraderModding
{
    public class TraderModdingOnlyScript : MonoBehaviour
    {
        public EditBuildScreen __instance;
        public Toggle onlyAvailableToggle;
        public Toggle onlyTradersToggle;
        public Item weaponBody = null;

        string[] tradermods;
        
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

        public void UpdateModView()
        {
            // If we only want to see available mods, do nothing
            if (onlyAvailableToggle.isOn) 
                return;

            // Get all mods that exist
            ItemFactory itemFactory = Singleton<ItemFactory>.Instance;
            Item[] allmods = itemFactory.CreateAllModsEver();

            // Get the player profile and build inv controller class
            Profile profile = (Profile)AccessTools.Field(typeof(EditBuildScreen), "profile_0").GetValue(__instance);
            InventoryControllerClass inventoryControllerClass = new InventoryControllerClass(profile, false, null);

            // Get all usable mods on the player and not on any weapon
            Item[] playeritems_usable_mods = __instance.InventoryController.Inventory.GetPlayerItems(EPlayerItems.All).Where(new Func<Item, bool>(__instance.method_16)).ToArray<Item>();

            List<string> allmods_player = playeritems_usable_mods.Select(mod => mod.TemplateId).ToList();

            // Get all mods in stash that are not alread on any other gun
            LootItemClass[] array2 = new LootItemClass[] { profile.Inventory.Stash };
            List<string> allmods_stash = array2.GetAllItemsFromCollections().Where(new Func<Item, bool>(__instance.method_16)).Select(mod => mod.TemplateId).ToList();
            
            StashClass stashClass = itemFactory.CreateFakeStash(null);
            stashClass.Grids[0] = new GClass2500(Guid.NewGuid().ToString(), 30, 1, true, Array.Empty<ItemFilter>(), stashClass);
            TraderControllerClass traderControllerClass = new TraderControllerClass(stashClass, "here lies profile id", Guid.NewGuid().ToString(), false, EOwnerType.Profile, null, null);

            // Get all mods that are already on the gun we are modding
            List<string> allmods_gun = weaponBody.GetAllItems().Select(mod => mod.TemplateId).ToList();
            
            foreach (Item item in allmods)
            {
                if (onlyTradersToggle.isOn && !
                    (tradermods.Contains(item.TemplateId) ||
                    allmods_gun.Contains(item.TemplateId) || 
                    allmods_stash.Contains(item.TemplateId) || 
                    allmods_player.Contains(item.TemplateId))) 
                    continue;

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
            tradermods = TraderModdingUtils.GetData();
        }
    }
}