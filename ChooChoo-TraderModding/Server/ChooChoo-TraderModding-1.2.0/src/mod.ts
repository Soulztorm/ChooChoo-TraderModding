//
// =====================================================================
// Credit to wara for the original server mod to receive trader offers!!
// =====================================================================
//
// Tradermodding 1.5.1 servermod - by ChooChoo / wara
// 

import { DependencyContainer } from "tsyringe";
import type { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import type { StaticRouterModService } from "@spt-aki/services/mod/staticRouter/StaticRouterModService";
import { TraderAssortHelper } from "@spt-aki/helpers/TraderAssortHelper";
import * as modConfig from "../config/config.json";
import { ItemHelper } from "@spt-aki/helpers/ItemHelper";
import { BaseClasses } from "@spt-aki/models/enums/BaseClasses";
import { IBarterScheme } from "@spt-aki/models/eft/common/tables/ITrader";

class ChooChooTraderModding implements IPreAkiLoadMod {

    public preAkiLoad(container: DependencyContainer): void {
        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");

        staticRouterModService.registerStaticRouter(
            "TraderModdingRouter",
            [
                {
                    url: "/choochoo-trader-modding/json",
                    action: (url, info, sessionId, output) => {
                        const json = this.getTraderMods(container, sessionId, false);
                        return json;
                    }
                },
                {
                    url: "/choochoo-trader-modding/json-flea",
                    action: (url, info, sessionId, output) => {
                        const json = this.getTraderMods(container, sessionId, true);
                        return json;
                    }
                }
            ],
            "choochoo-trader-modding"
        );
    }

    public getTraderMods(container: DependencyContainer, sessionId: string, flea: boolean): string {
        const allTraderIds = [
            "54cb50c76803fa8b248b4571",
            "54cb57776803fa99248b456e",
            "58330581ace78e27b8b10cee",
            "5935c25fb3acc3127c3d8cd9",
            "5a7c2eca46aef81a7ca2145d",
            "5ac3b934156ae10c4430e83c",
            "5c0647fdd443bc2504c2d371"
        ];

        // Roubles, Dollars, Euros
        const money = ["5449016a4bdc2d6f028b456f", "5696686a4bdc2da3298b456a", "569668774bdc2da2298b4568"]
        //const money_symbols = ["<color=#c4bc89> ₽</color>", "<color=#03d100> $</color>", "<color=#0073de> €</color>"]
        const money_symbols = ["r", "d", "e"]

        // add custom traders defined in the config file
        for (const trader of modConfig.customTraderIds) {
            allTraderIds.push(trader);
        }

        const traderAssortHelper = container.resolve<TraderAssortHelper>("TraderAssortHelper");
        const itemHelper = container.resolve<ItemHelper>("ItemHelper");
        let addedByUnlimitedCount = false;

        type ModAndCost = {
            tpl: string;
            cost: string;
        }
        type TraderData = {
            dollar_to_ruble: number;
            euro_to_ruble: number;
            modsAndCosts: ModAndCost[];
        }
        const allTraderData: TraderData = {dollar_to_ruble: 0, euro_to_ruble: 0, modsAndCosts: []};

        for (const trader of allTraderIds) {
            const traderAssort = traderAssortHelper.getAssort(sessionId, trader, flea);

            for (const item of traderAssort.items) {
                addedByUnlimitedCount = false;

                if (itemHelper.isOfBaseclass(item._tpl, BaseClasses.MOD)) {
                    if (traderAssort.barter_scheme[item._id] !== undefined) {
                        // for now no barter offers. Eventually might add the option to toggle it on in the config but I don't feel like it rn
                        const barterOffer = traderAssort.barter_scheme[item._id][0][0];
                        if (!this.isBarterOffer(barterOffer, money)) {
                            if (item.upd !== undefined) { 
                                const mac: ModAndCost  = { "tpl": item._tpl, "cost": this.getCostString(barterOffer, money, money_symbols)};
                                if (item.upd.UnlimitedCount !== undefined) {
                                    // probably unnecessary but to be safe.
                                    if (item.upd.UnlimitedCount == true) {
                                        allTraderData.modsAndCosts.push(mac);
                                        addedByUnlimitedCount = true;
                                    }
                                }
                                if (item.upd.StackObjectsCount !== undefined && !addedByUnlimitedCount) {
                                    if (item.upd.StackObjectsCount > 0) {
                                        allTraderData.modsAndCosts.push(mac);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (itemHelper.isOfBaseclass(item._tpl, BaseClasses.MONEY)){
                    // Dollar
                    if (item._tpl == "5696686a4bdc2da3298b456a"){
                        const t = traderAssort.barter_scheme[item._id][0][0];
                        if (t !== undefined)
                            allTraderData.dollar_to_ruble = t.count;
                    }
                    // Euro
                    else if (item._tpl == "569668774bdc2da2298b4568"){
                        const t = traderAssort.barter_scheme[item._id][0][0];
                        if (t !== undefined)
                            allTraderData.euro_to_ruble = t.count;
                    }     
                }
            }
        }

        const json = JSON.stringify(allTraderData);    
        return json;
    }

    public isBarterOffer(barter_scheme: IBarterScheme, money: string[]): boolean {
        if (money.includes(barter_scheme._tpl)) {
            return false;
        }
        return true;
    }

    public getCostString(barter_scheme: IBarterScheme, money: string[], money_symbols: string[]) : string {
        const moneyIndex = money.findIndex((string) => string == barter_scheme._tpl);
        if (moneyIndex == -1)
            return "";
     
        return Math.ceil(barter_scheme.count).toString() + money_symbols[moneyIndex];
    }
}

module.exports = { mod: new ChooChooTraderModding() }