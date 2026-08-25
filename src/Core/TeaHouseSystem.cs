namespace Luminfield.Core;

public sealed record TeaHouseOfferDefinition(
    string Id,
    string ItemId,
    int Price,
    string NameKey,
    string DescriptionKey,
    string EffectId,
    string EffectNameKey,
    string EffectDescriptionKey,
    int EnergyRestore,
    float MovementMultiplier
);

public sealed record TeaHouseEffectSnapshot(
    string OfferId,
    string EffectId,
    string NameKey,
    string DescriptionKey,
    int Day,
    int ExpiresMinuteOfDay,
    float MovementMultiplier
);

public sealed record TeaHouseGatheringSnapshot(
    bool CanHost,
    bool Hosted,
    string ReasonKey,
    IReadOnlyList<string> GuestNpcIds
);

public sealed class TeaHouseSystem
{
    public const int GatheringStartMinute = 13 * 60;
    public const int GatheringEndMinute = 18 * 60;
    public const int RequiredGuestCount = 2;
    public const int GatheringRelationshipPoints = 3;
    public const int EffectDurationMinutes = 120;
    public const string CloudleafFocusOfferId = "tea_house_cloudleaf_focus";
    public const string MoonrootCalmOfferId = "tea_house_moonroot_calm";
    public const string StarbudSharingOfferId = "tea_house_starbud_sharing";
    public const string CloudleafFocusEffectId = "tea_effect_cloudleaf_focus";
    public const string MoonrootCalmEffectId = "tea_effect_moonroot_calm";
    public const string StarbudSharingEffectId = "tea_effect_starbud_sharing";

    private int _day = 1;
    private string _purchasedOfferId = string.Empty;
    private string _activeEffectId = string.Empty;
    private int _effectExpiresMinuteOfDay;
    private bool _gatheringHosted;
    private readonly HashSet<string> _gatheringGuestNpcIds =
        new(StringComparer.Ordinal);

    public static IReadOnlyList<TeaHouseOfferDefinition> Offers { get; } =
    [
        new(
            CloudleafFocusOfferId,
            DataCatalog.CloudleafTeaId,
            82,
            "tea_house.offer.cloudleaf_focus.name",
            "tea_house.offer.cloudleaf_focus.description",
            CloudleafFocusEffectId,
            "tea_house.effect.cloudleaf_focus.name",
            "tea_house.effect.cloudleaf_focus.description",
            12,
            1.08f
        ),
        new(
            MoonrootCalmOfferId,
            DataCatalog.MoonrootTonicId,
            108,
            "tea_house.offer.moonroot_calm.name",
            "tea_house.offer.moonroot_calm.description",
            MoonrootCalmEffectId,
            "tea_house.effect.moonroot_calm.name",
            "tea_house.effect.moonroot_calm.description",
            18,
            1.08f
        ),
        new(
            StarbudSharingOfferId,
            DataCatalog.StarbudPreserveId,
            72,
            "tea_house.offer.starbud_sharing.name",
            "tea_house.offer.starbud_sharing.description",
            StarbudSharingEffectId,
            "tea_house.effect.starbud_sharing.name",
            "tea_house.effect.starbud_sharing.description",
            10,
            1.08f
        )
    ];

    public event Action? Changed;

    public int Day => _day;
    public string PurchasedOfferId => _purchasedOfferId;
    public bool GatheringHosted => _gatheringHosted;
    public IReadOnlySet<string> GatheringGuestNpcIds => _gatheringGuestNpcIds;

    public void Reset(int day)
    {
        Restore(new TeaHouseSave { Day = day }, day);
    }

    public void Restore(TeaHouseSave? save, int currentDay)
    {
        var normalized = NormalizeSave(save, currentDay);
        _day = normalized.Day;
        _purchasedOfferId = normalized.PurchasedOfferId;
        _activeEffectId = normalized.ActiveEffectId;
        _effectExpiresMinuteOfDay = normalized.EffectExpiresMinuteOfDay;
        _gatheringHosted = normalized.GatheringHosted;
        _gatheringGuestNpcIds.Clear();
        foreach (var npcId in normalized.GatheringGuestNpcIds)
        {
            _gatheringGuestNpcIds.Add(npcId);
        }

        Changed?.Invoke();
    }

    public TeaHouseSave Capture() => new()
    {
        Day = _day,
        PurchasedOfferId = _purchasedOfferId,
        ActiveEffectId = _activeEffectId,
        EffectExpiresMinuteOfDay = _effectExpiresMinuteOfDay,
        GatheringHosted = _gatheringHosted,
        GatheringGuestNpcIds = _gatheringGuestNpcIds
            .Order(StringComparer.Ordinal)
            .ToList()
    };

    public static TeaHouseOfferDefinition OfferForDay(int day) =>
        Offers[(Math.Max(1, day) - 1) % Offers.Count];

    public static bool TryOffer(
        string offerId,
        out TeaHouseOfferDefinition offer
    )
    {
        offer = Offers.FirstOrDefault(candidate =>
            candidate.Id == offerId
        )!;
        return offer is not null;
    }

    public ActionResult CheckPurchase(
        string offerId,
        int currentDay,
        int coins,
        Inventory inventory
    )
    {
        EnsureDay(currentDay);
        if (!TryOffer(offerId, out var offer) ||
            OfferForDay(currentDay).Id != offerId)
        {
            return ActionResult.Fail("tea_house.offer.unavailable");
        }

        if (!string.IsNullOrWhiteSpace(_purchasedOfferId))
        {
            return ActionResult.Fail("tea_house.offer.already_purchased");
        }

        if (coins < offer.Price)
        {
            return ActionResult.Fail("shop.not_enough_coins");
        }

        return inventory.CanAdd(offer.ItemId, 1)
            ? ActionResult.Success(messageKey: "tea_house.offer.ready")
            : ActionResult.Fail("notice.inventory_full");
    }

    public void RecordPurchase(
        string offerId,
        int currentDay,
        int currentMinuteOfDay
    )
    {
        EnsureDay(currentDay);
        var offer = Offers.Single(candidate => candidate.Id == offerId);
        _purchasedOfferId = offer.Id;
        _activeEffectId = offer.EffectId;
        _effectExpiresMinuteOfDay = Math.Min(
            GameClock.EndMinute,
            currentMinuteOfDay + EffectDurationMinutes
        );
        Changed?.Invoke();
    }

    public TeaHouseEffectSnapshot? ActiveEffect(
        int currentDay,
        int currentMinuteOfDay
    )
    {
        EnsureDay(currentDay);
        if (string.IsNullOrWhiteSpace(_purchasedOfferId) ||
            string.IsNullOrWhiteSpace(_activeEffectId) ||
            currentMinuteOfDay >= _effectExpiresMinuteOfDay ||
            !TryOffer(_purchasedOfferId, out var offer) ||
            offer.EffectId != _activeEffectId)
        {
            return null;
        }

        return new TeaHouseEffectSnapshot(
            offer.Id,
            offer.EffectId,
            offer.EffectNameKey,
            offer.EffectDescriptionKey,
            _day,
            _effectExpiresMinuteOfDay,
            offer.MovementMultiplier
        );
    }

    public TeaHouseGatheringSnapshot GatheringSnapshot(
        int currentDay,
        int minuteOfDay,
        IReadOnlyList<string> presentNpcIds
    )
    {
        EnsureDay(currentDay);
        var guests = EligibleGuests(presentNpcIds);
        if (_gatheringHosted)
        {
            return new TeaHouseGatheringSnapshot(
                false,
                true,
                "tea_house.gathering.already_hosted",
                _gatheringGuestNpcIds.Order(StringComparer.Ordinal).ToArray()
            );
        }

        if (string.IsNullOrWhiteSpace(_purchasedOfferId))
        {
            return new TeaHouseGatheringSnapshot(
                false,
                false,
                "tea_house.gathering.needs_tea",
                guests
            );
        }

        if (minuteOfDay < GatheringStartMinute ||
            minuteOfDay >= GatheringEndMinute)
        {
            return new TeaHouseGatheringSnapshot(
                false,
                false,
                "tea_house.gathering.outside_hours",
                guests
            );
        }

        if (guests.Count < RequiredGuestCount)
        {
            return new TeaHouseGatheringSnapshot(
                false,
                false,
                "tea_house.gathering.needs_guests",
                guests
            );
        }

        return new TeaHouseGatheringSnapshot(
            true,
            false,
            "tea_house.gathering.ready",
            guests
        );
    }

    public ActionResult RecordGathering(
        int currentDay,
        int minuteOfDay,
        IReadOnlyList<string> presentNpcIds,
        out IReadOnlyList<string> hostedGuestNpcIds
    )
    {
        var snapshot = GatheringSnapshot(
            currentDay,
            minuteOfDay,
            presentNpcIds
        );
        hostedGuestNpcIds = snapshot.GuestNpcIds;
        if (!snapshot.CanHost)
        {
            return ActionResult.Fail(snapshot.ReasonKey);
        }

        _gatheringHosted = true;
        _gatheringGuestNpcIds.Clear();
        foreach (var guestId in snapshot.GuestNpcIds)
        {
            _gatheringGuestNpcIds.Add(guestId);
        }

        Changed?.Invoke();
        return ActionResult.Success(messageKey: "tea_house.gathering.hosted");
    }

    public static TeaHouseSave NormalizeSave(
        TeaHouseSave? save,
        int currentDay
    )
    {
        var day = Math.Max(1, currentDay);
        if (save is null || save.Day != day)
        {
            return new TeaHouseSave { Day = day };
        }

        var expectedOfferId = OfferForDay(day).Id;
        var offer = Offers.FirstOrDefault(candidate =>
            candidate.Id == save.PurchasedOfferId &&
            candidate.Id == expectedOfferId
        );
        var activeEffectId = offer is not null &&
            offer.EffectId == save.ActiveEffectId &&
            save.EffectExpiresMinuteOfDay > GameClock.StartMinute &&
            save.EffectExpiresMinuteOfDay <= GameClock.EndMinute
                ? save.ActiveEffectId
                : string.Empty;
        var purchasedOfferId = activeEffectId.Length > 0
            ? offer!.Id
            : string.Empty;
        var guestIds = (save.GatheringGuestNpcIds ?? [])
            .Where(VillageCatalog.Npcs.ContainsKey)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        var gatheringHosted = purchasedOfferId.Length > 0 &&
            save.GatheringHosted &&
            guestIds.Count >= RequiredGuestCount;

        return new TeaHouseSave
        {
            Day = day,
            PurchasedOfferId = purchasedOfferId,
            ActiveEffectId = activeEffectId,
            EffectExpiresMinuteOfDay = activeEffectId.Length > 0
                ? save.EffectExpiresMinuteOfDay
                : 0,
            GatheringHosted = gatheringHosted,
            GatheringGuestNpcIds = gatheringHosted ? guestIds : []
        };
    }

    private void EnsureDay(int currentDay)
    {
        if (_day == currentDay)
        {
            return;
        }

        _day = Math.Max(1, currentDay);
        _purchasedOfferId = string.Empty;
        _activeEffectId = string.Empty;
        _effectExpiresMinuteOfDay = 0;
        _gatheringHosted = false;
        _gatheringGuestNpcIds.Clear();
        Changed?.Invoke();
    }

    private static IReadOnlyList<string> EligibleGuests(
        IEnumerable<string> presentNpcIds
    ) => presentNpcIds
        .Where(VillageCatalog.Npcs.ContainsKey)
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToArray();
}
