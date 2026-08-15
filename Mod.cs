using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;

namespace Scream100;

public sealed record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.bensburnedwaffles.scream100";
    public string Name { get; init; } = "Scream 100 Grenade";
    public string Author { get; init; } = "BensBurnedWaffles";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.0.8");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.2");
    public bool HasPrepatcher { get; init; }
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 1)]
public sealed class Scream100Mod : IOnLoad
{
    public const string TemplateId = "76a0e2d8c1464f22a2c5fe01";

    private const string Vog17TemplateId = "5e32f56fcb6d5863cc5e5ee4";
    private const string TraderOfferId = "76a0e2d8c1464f22a2c5fe02";
    private const string GrenadeParentId = "543be6564bdc2df4348b4568";
    private const string GrenadeHandbookParentId = "5b5f7a2386f774093f2ed3c4";
    private const string PraporId = "54cb50c76803fa8b248b4571";
    private const string RoubleId = "5449016a4bdc2d6f028b456f";
    private const int Price = 8_500;
    private const double TotalFuseSeconds = 16.012;

    private readonly CustomItemService _customItemService;
    private readonly TradersTable _traders;

    public Scream100Mod(CustomItemService customItemService, TradersTable traders)
    {
        _customItemService = customItemService;
        _traders = traders;
    }

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CreateGrenade();
        AddPraporOffer();
        return Task.CompletedTask;
    }

    private void CreateGrenade()
    {
        Vector3 harmless = new(0f, 0f, 0f);
        NewItemFromCloneDetails details = new()
        {
            NewItemName = "weapon_grenade_scream_100",
            ItemTplToClone = Vog17TemplateId,
            ParentId = GrenadeParentId,
            NewId = TemplateId,
            HandbookParentId = GrenadeHandbookParentId,
            HandbookPriceRoubles = Price,
            FleaPriceRoubles = Price * 1.25,
            AddToHandbook = true,
            AddToFleaPriceDb = true,
            AddToWeaponShelf = false,
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new LocaleDetails
                {
                    Name = "Scream 100 grenade",
                    ShortName = "Scream 100",
                    Description =
                        "An experimental VOG-17-based sound grenade. Four seconds after being thrown, it emits "
                        + "an intense rising scream and then detonates. This version is harmless: the detonation "
                        + "produces no fragments, blast damage, blindness, or concussion.",
                },
            },
            OverrideProperties = new TemplateItemProperties
            {
                Weight = 0.28,
                StackMaxSize = 1,
                ExplDelay = TotalFuseSeconds,
                explDelay = TotalFuseSeconds,
                MinExplosionDistance = 0,
                MaxExplosionDistance = 0,
                FragmentsCount = 0,
                Strength = 0,
                Blindness = harmless,
                Contusion = harmless,
                ContusionDistance = 0,
                ArmorDistanceDistanceDamage = harmless,
                ThrowDamMax = 0,
                MinTimeToContactExplode = -1,
                PlayFuzeSound = false,
                ExplosionEffectType = "Grenade_new",
            },
        };

        CreateItemResult result = _customItemService.CreateItemFromClone(details);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Scream 100 creation failed: {string.Join("; ", result.Errors)}");
        }
    }

    private void AddPraporOffer()
    {
        if (!_traders.TryGetValue(PraporId, out Trader? prapor))
        {
            throw new InvalidOperationException("Scream 100 could not find Prapor in the trader database.");
        }

        MongoId offerId = TraderOfferId;
        prapor.Assort.Items.Add(new Item
        {
            Id = offerId,
            Template = TemplateId,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                UnlimitedCount = true,
                StackObjectsCount = 9_999_999,
                BuyRestrictionMax = 12,
                BuyRestrictionCurrent = 0,
            },
        });

        prapor.Assort.BarterScheme[offerId] = new List<List<BarterScheme>>
        {
            new()
            {
                new BarterScheme
                {
                    Count = Price,
                    Template = RoubleId,
                },
            },
        };
        prapor.Assort.LoyalLevelItems[offerId] = 1;
    }
}
