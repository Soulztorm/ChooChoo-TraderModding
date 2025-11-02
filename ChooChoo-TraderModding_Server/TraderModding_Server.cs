using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Services;

namespace TraderModding;


class ModInfoData
{
    public string tpl { get; set; }
    public string cost { get; set; }
    public int tidx { get; set; }
    public string id { get; set; }
    public int lp { get; set; }
    public int lm { get; set; }
}

class TraderData
{
    public int dollar_to_ruble { get; set; } = 1;
    public int euro_to_ruble { get; set; } = 1;
    public List<string> traderIDs { get; set; } = new();
    public List<ModInfoData> modInfoData { get; set; } = new();
}

[Injectable(TypePriority = Int32.MaxValue)]
public class TraderModding : IOnLoad
{
    public Task OnLoad() { return Task.CompletedTask; }
    
    public static ISptLogger<TraderModding> logger;

    public TraderModding(ISptLogger<TraderModding> _logger)
    {
        logger = _logger;
    }
}


[Injectable]
public class TraderModdingRouter : StaticRouter
{
    private static readonly ConfigServer configServer = ServiceLocator.ServiceProvider.GetRequiredService<ConfigServer>();
    private static readonly RagfairServerHelper ragfairServerHelper = ServiceLocator.ServiceProvider.GetRequiredService<RagfairServerHelper>();
    private static readonly RagfairPriceService ragfairPriceService = ServiceLocator.ServiceProvider.GetRequiredService<RagfairPriceService>();
    private static readonly DatabaseService databaseService = ServiceLocator.ServiceProvider.GetRequiredService<DatabaseService>();
    private static readonly ProfileHelper profileHelper = ServiceLocator.ServiceProvider.GetRequiredService<ProfileHelper>();
    private static readonly TraderAssortHelper traderAssortHelper = ServiceLocator.ServiceProvider.GetRequiredService<TraderAssortHelper>();
    private static readonly ItemHelper itemHelper = ServiceLocator.ServiceProvider.GetRequiredService<ItemHelper>();
    private static readonly JsonUtil jsonUtil = ServiceLocator.ServiceProvider.GetRequiredService<JsonUtil>();
    
    private static readonly Dictionary<string, string> _money = new() {
        {Money.ROUBLES, "r"}, { Money.DOLLARS, "d" }, {Money.EUROS, "e"}};
    
    public TraderModdingRouter(
        JsonUtil jsonUtil,
        HttpResponseUtil httpResponseUtil) : base(
        jsonUtil,
        GetTraderModdingRoutes()
    ){ }
    
    private static List<RouteAction> GetTraderModdingRoutes()
    {
        return
        [
            new RouteAction(
                "/choochoo-trader-modding/json",
                async (
                    url,
                    info,
                    sessionId,
                    output
                ) => await GetTraderMods(sessionId, false)
            ),
            new RouteAction(
                "/choochoo-trader-modding/json-flea",
                async (
                    url,
                    info,
                    sessionId,
                    output
                ) => await GetTraderMods(sessionId, true)
            )
        ];
    }
    
    private static ValueTask<string> GetTraderMods(MongoId sessionId, bool get_flea_items)
    {
        TraderData allTraderData = new TraderData
        {
            dollar_to_ruble = 146,
            euro_to_ruble = 159
        };
        int traderIDX = -1;

        var pmcData = profileHelper.GetPmcProfile(sessionId);
        if (pmcData == null)
        {
            TraderModding.logger.Error("PMC profile not found, cannot get trader mods");
            return new ValueTask<string>("");
        }

        HashSet<MongoId> modAvailableFromTraders = new HashSet<MongoId>();

        // Get any buy restriction modifiers for the profiles game version
        double buyRestrictionMaxBonus = 1.0;
        if (pmcData.Info != null && pmcData.Info.GameVersion != null &&
            databaseService.GetGlobals().Configuration.TradingSettings.BuyRestrictionMaxBonus.TryGetValue(pmcData.Info.GameVersion, out var buyMaxBonus))
        {
            buyRestrictionMaxBonus = buyMaxBonus.Multiplier;
        }
        
        // Check for flea unlock
        bool fleaUnlocked = (pmcData.Info != null && pmcData.Info.Level != null && pmcData.Info.Level >= databaseService.GetGlobals().Configuration.RagFair.MinUserLevel);

        // Get trader data
        foreach (var trader in databaseService.GetTraders())
        {
            // Skip traders without assorts
            if (trader.Value.Assort == null) continue;
            
            TraderAssort traderAssort = traderAssortHelper.GetAssort(sessionId, trader.Key, false);
            if (traderAssort.Items.Count == 0)
                continue;
            
            // Add to list of valid traders
            allTraderData.traderIDs.Add(trader.Key);
            traderIDX++;

            foreach (var item in traderAssort.Items)
            {
                // See if this item is a money trade to get conversion rates from
                if (itemHelper.IsOfBaseclass(item.Template, BaseClasses.MONEY) && traderAssort.BarterScheme.ContainsKey(item.Id))
                {
                    var t = traderAssort.BarterScheme[item.Id][0][0];
                    if (t.Count.HasValue)
                    {
                        if (item.Template == Money.DOLLARS)
                            allTraderData.dollar_to_ruble = (int)Math.Ceiling(t.Count.Value);
                        else if (item.Template == Money.EUROS)
                            allTraderData.euro_to_ruble = (int)Math.Ceiling(t.Count.Value);
                    }
                }
                
                // Skip non mods
                if (!itemHelper.IsOfBaseclass(item.Template, BaseClasses.MOD)) continue;
                
                // Skip empty barter schemes
                if (!traderAssort.BarterScheme.TryGetValue(item.Id, out var barters) || barters.Count <= 0) continue;
                
                // Find a valid barter scheme that accepts only money (in case there are multiples)
                BarterScheme barterSchemeForItem = null;
                foreach (var barter in barters)
                {
                    if (_money.ContainsKey(barter[0].Template))
                    {
                        barterSchemeForItem = barter[0];
                        break;
                    };
                }
                if (barterSchemeForItem == null || item.Upd == null) continue;

                int buyrestrictionCurrent = item.Upd.BuyRestrictionCurrent.GetValueOrDefault(0);
                int buyrestrictionMax = item.Upd.BuyRestrictionMax.GetValueOrDefault(0);
                int buyRestrictionMaxWithBonus = (int)(buyrestrictionMax * buyRestrictionMaxBonus);
                bool buylimitReached = buyrestrictionMax == 0 || buyrestrictionCurrent >= buyrestrictionMax;

                if (!buylimitReached &&                                                             // Personal buy limit not reached AND 
                    ((item.Upd.UnlimitedCount.HasValue && item.Upd.UnlimitedCount.Value) ||         // unlimited stock
                    (item.Upd.StackObjectsCount is > 0)))                                           // OR limited stack, but still stock in trader
                {
                    ModInfoData mac = new ModInfoData()
                    {
                        tpl = item.Template,
                        cost = getCostString(barterSchemeForItem),
                        tidx = traderIDX,
                        id = item.Id,
                        lp = buyrestrictionCurrent,
                        lm = buyRestrictionMaxWithBonus
                    };
                    
                    allTraderData.modInfoData.Add(mac);
                }

                // Track trader items that did not reach restrictions 
                if (!buylimitReached)
                    modAvailableFromTraders.Add(item.Template);
            }
        }
        
        
        // Fetch items not available from traders from flea
        if (get_flea_items && fleaUnlocked){
            RagfairConfig ragfairConfig = configServer.GetConfig<RagfairConfig>();
            var priceRange = ragfairConfig.Dynamic.PriceRanges.Default;
            var templates = databaseService.GetItems();
            
            foreach (var titem in templates.Values)
            {
                MongoId tplId = titem.Id;
                // If this is already in the trader list, dont add to flea list
                if (!itemHelper.IsOfBaseclass(tplId, BaseClasses.MOD) || 
                    modAvailableFromTraders.Contains(tplId) ||
                    !ragfairServerHelper.IsItemValidRagfairItem(new KeyValuePair<bool, TemplateItem?>(itemHelper.IsValidItem(tplId), titem)))
                    continue;
                
                // Get flea price with adjusted minimum range
                var price = ragfairPriceService.GetFleaPriceForItem(tplId);
                price *= priceRange.Min;

                ModInfoData mac = new ModInfoData()
                {
                    tpl = tplId,
                    cost = "0" + ((int)Math.Ceiling(price)) + "r", 
                    tidx = -1,
                    id = "",
                    lp = -1,
                    lm = -1
                };
                
                allTraderData.modInfoData.Add(mac);
            }
        }     
        
        var json = jsonUtil.Serialize(allTraderData);
        if (json is null)
        {
            TraderModding.logger.Error("Unable to serialize trader mods!");
            return new ValueTask<string>("");
        }
        
        return new ValueTask<string>(json);
    }
    
    static string getCostString(BarterScheme barterOffer)
    {
        if (!_money.TryGetValue(barterOffer.Template, out var moneysuffix))
            return "";
        
        return ((int)Math.Ceiling(barterOffer.Count.GetValueOrDefault())) + moneysuffix;
    }

}

