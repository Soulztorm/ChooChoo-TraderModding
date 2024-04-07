using Comfort.Common;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System;
using EFT;
using EFT.UI;
using EFT.InventoryLogic;
using UnityEngine;

namespace TraderModding
{
    public class TraderModdingOnlyScript : MonoBehaviour
    {
        public EditBuildScreen __instance;
        public Item weaponBody = null;
        public bool useOnlyAvailable = false;

        bool useTradersOnly = false;
        
        public void ToggleTradersOnlyView(bool tradersOnly)
        {
            useTradersOnly = tradersOnly;
            UpdateModView();
        }

        public void UpdateModView()
        {
            // If we only want to see available mods, do nothing
            if (useOnlyAvailable) 
                return;

            // Get all mods available from traders
            string[] tradermods = TraderModdingUtils.GetData();

            // Get all mods that exist
            ItemFactory itemFactory = Singleton<ItemFactory>.Instance;
            Item[] allmods = itemFactory.CreateAllModsEver();

            // Get the player profile and build inv controller class
            Profile profile = Traverse.Create(__instance).Field("profile_0").GetValue() as Profile;
            InventoryControllerClass inventoryControllerClass = new InventoryControllerClass(profile, false, null);

            // Get all usable mods on the player and not on any weapon
            Item[] playeritems_usable_mods = __instance.InventoryController.Inventory.GetPlayerItems(EPlayerItems.All).Where(new Func<Item, bool>(__instance.method_16)).ToArray<Item>();
            
            // Get all mods in stash that are not alread on any other gun
            LootItemClass[] array2 = new LootItemClass[] { profile.Inventory.Stash };
            List<string> allmods_stash = array2.GetAllItemsFromCollections().Where(new Func<Item, bool>(__instance.method_16)).Select(mod => mod.TemplateId).ToList();
            
            StashClass stashClass = itemFactory.CreateFakeStash(null);
            stashClass.Grids[0] = new GClass2500(Guid.NewGuid().ToString(), 30, 1, true, Array.Empty<ItemFilter>(), stashClass);
            TraderControllerClass traderControllerClass = new TraderControllerClass(stashClass, "here lies profile id", Guid.NewGuid().ToString(), false, EOwnerType.Profile, null, null);

            List<string> allItemsInGun = weaponBody.GetAllItems().Select(mod => mod.TemplateId).ToList();
            
            foreach (Item item in allmods)
            {
                if (useTradersOnly && !(tradermods.Contains(item.TemplateId) || allItemsInGun.Contains(item.TemplateId) || allmods_stash.Contains(item.TemplateId))) 
                    continue;

                item.StackObjectsCount = item.StackMaxSize;
                stashClass.Grid.Add(item);
            }

            GClass2830 manip = new GClass2830(inventoryControllerClass, new LootItemClass[] { (LootItemClass)traderControllerClass.RootItem }, playeritems_usable_mods);
            __instance.UpdateManipulation(manip);
            __instance.RefreshWeapon();
        }
    }
}