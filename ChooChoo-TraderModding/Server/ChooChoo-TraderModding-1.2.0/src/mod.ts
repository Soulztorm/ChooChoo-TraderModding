//
// =====================================================================
// Credit to wara for the original server mod to receive trader offers!!
// =====================================================================
//
// Tradermodding 1.6.2 servermod - by ChooChoo / wara
// 

import { DependencyContainer } from "tsyringe";
import type { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import type { StaticRouterModService } from "@spt-aki/services/mod/staticRouter/StaticRouterModService";
import { TraderAssortHelper } from "@spt-aki/helpers/TraderAssortHelper";
import { ItemHelper } from "@spt-aki/helpers/ItemHelper";
import { BaseClasses } from "@spt-aki/models/enums/BaseClasses";
import { IBarterScheme } from "@spt-aki/models/eft/common/tables/ITrader";
import { Traders } from "@spt-aki/models/enums/Traders";
import { ProfileHelper } from "@spt-aki/helpers/ProfileHelper";

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
        // Roubles, Dollars, Euros
        const money = ["5449016a4bdc2d6f028b456f", "5696686a4bdc2da3298b456a", "569668774bdc2da2298b4568"]
        const money_symbols = ["r", "d", "e"]

        const traderAssortHelper = container.resolve<TraderAssortHelper>("TraderAssortHelper");
        const itemHelper = container.resolve<ItemHelper>("ItemHelper");
        const profileHelper = container.resolve<ProfileHelper>("ProfileHelper");

        const pmcData = profileHelper.getPmcProfile(sessionId);

        type ModAndCost = {
            tpl: string;
            cost: string;
        }
        type TraderData = {
            dollar_to_ruble: number;
            euro_to_ruble: number;
            modsAndCosts: ModAndCost[];
        }
        const allTraderData: TraderData = {dollar_to_ruble: 146, euro_to_ruble: 159, modsAndCosts: []};

        const allTraderIds = Object.keys(Traders);   
        allTraderIds.forEach(traderkey => {
            const trader = Traders[traderkey];

            // Skip fence, btr and lighthouse keeper
            if (traderkey == "FENCE" ||traderkey == "BTR" || traderkey == "LIGHTHOUSEKEEPER")
                return;

            // Check if the trader is currently locked
            if (!pmcData.TradersInfo[trader].unlocked){
                console.log("Trader: " + traderkey + " is locked, skipping items.");
                return;
            }

            const traderAssort = traderAssortHelper.getAssort(sessionId, trader, flea);

            // Just to be sure so any custom traders don't break.
            if (traderAssort == undefined || traderAssort.items == undefined)
                return;

            for (const item of traderAssort.items) {
                let addedByUnlimitedCount = false;

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
                        if (t !== undefined && t.count != undefined)
                            allTraderData.dollar_to_ruble = Math.ceil(t.count);
                    }
                    // Euro
                    else if (item._tpl == "569668774bdc2da2298b4568"){
                        const t = traderAssort.barter_scheme[item._id][0][0];
                        if (t !== undefined && t.count != undefined)
                            allTraderData.euro_to_ruble = Math.ceil(t.count);
                    }     
                }
            }
        })

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