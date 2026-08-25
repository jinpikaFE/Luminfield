using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class StarweaverTeaHouseTests
{
    [Fact]
    public void DailyOfferRotatesDeterministically()
    {
        Assert.Equal(
            TeaHouseSystem.CloudleafFocusOfferId,
            TeaHouseSystem.OfferForDay(1).Id
        );
        Assert.Equal(
            TeaHouseSystem.MoonrootCalmOfferId,
            TeaHouseSystem.OfferForDay(2).Id
        );
        Assert.Equal(
            TeaHouseSystem.StarbudSharingOfferId,
            TeaHouseSystem.OfferForDay(3).Id
        );
        Assert.Equal(
            TeaHouseSystem.CloudleafFocusOfferId,
            TeaHouseSystem.OfferForDay(4).Id
        );
    }

    [Fact]
    public void PurchaseIsAtomicAndAppliesTimedTastingEffect()
    {
        var session = TeaHouseSession(day: 1, minuteOfDay: 13 * 60);
        var offer = session.TodayTeaHouseOffer;
        var coinsBefore = session.Coins;
        var ownedBefore = session.Inventory.Count(offer.ItemId);

        var result = session.BuyTeaHouseOffer(offer.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(coinsBefore - offer.Price, session.Coins);
        Assert.Equal(ownedBefore + 1, session.Inventory.Count(offer.ItemId));
        Assert.Equal(offer.Id, session.TeaHouse.PurchasedOfferId);
        var effect = Assert.IsType<TeaHouseEffectSnapshot>(
            session.ActiveTeaHouseEffect
        );
        Assert.Equal(15 * 60, effect.ExpiresMinuteOfDay);

        session.SetPlayerLocation(504, 152, PlayerLocationIds.World);
        Assert.Equal(
            session.Weather.Current.OutdoorMovementMultiplier *
                offer.MovementMultiplier,
            session.PlayerMovementMultiplier,
            3
        );

        var expired = session.Capture();
        expired.MinuteOfDay = effect.ExpiresMinuteOfDay;
        session.Restore(expired);
        Assert.Null(session.ActiveTeaHouseEffect);
        Assert.Equal(
            session.Weather.Current.OutdoorMovementMultiplier,
            session.PlayerMovementMultiplier,
            3
        );
    }

    [Fact]
    public void PurchaseFailuresDoNotChangeSessionState()
    {
        var insufficientCoins = TeaHouseSession(coins: 0);
        AssertUnchanged(
            insufficientCoins,
            () => insufficientCoins.BuyTeaHouseOffer(
                insufficientCoins.TodayTeaHouseOffer.Id
            ),
            "shop.not_enough_coins"
        );

        var wrongOffer = TeaHouseSession();
        var unavailable = TeaHouseSystem.Offers.First(offer =>
            offer.Id != wrongOffer.TodayTeaHouseOffer.Id
        );
        AssertUnchanged(
            wrongOffer,
            () => wrongOffer.BuyTeaHouseOffer(unavailable.Id),
            "tea_house.offer.unavailable"
        );

        var wrongLocation = TeaHouseSession();
        wrongLocation.SetPlayerLocation(504, 152, PlayerLocationIds.World);
        AssertUnchanged(
            wrongLocation,
            () => wrongLocation.BuyTeaHouseOffer(
                wrongLocation.TodayTeaHouseOffer.Id
            ),
            "notice.nothing_to_interact"
        );

        var wrongTool = TeaHouseSession(selectedSlot: 1);
        AssertUnchanged(
            wrongTool,
            () => wrongTool.BuyTeaHouseOffer(
                wrongTool.TodayTeaHouseOffer.Id
            ),
            "notice.needs_hand"
        );

        var closed = TeaHouseSession(minuteOfDay: 8 * 60);
        AssertUnchanged(
            closed,
            () => closed.BuyTeaHouseOffer(closed.TodayTeaHouseOffer.Id),
            "notice.tea_house_closed"
        );

        var full = TeaHouseSession();
        FillInventory(full, full.TodayTeaHouseOffer.ItemId);
        AssertUnchanged(
            full,
            () => full.BuyTeaHouseOffer(full.TodayTeaHouseOffer.Id),
            "notice.inventory_full"
        );

        var duplicate = TeaHouseSession();
        Assert.True(duplicate.BuyTeaHouseOffer(
            duplicate.TodayTeaHouseOffer.Id
        ).Succeeded);
        AssertUnchanged(
            duplicate,
            () => duplicate.BuyTeaHouseOffer(
                duplicate.TodayTeaHouseOffer.Id
            ),
            "tea_house.offer.already_purchased"
        );
    }

    [Fact]
    public void AfternoonGatheringRewardsProjectedGuestsOnce()
    {
        var slot = FindGatheringSlot();
        var session = TeaHouseSession(slot.Day, slot.MinuteOfDay);
        var guests = session.PresentTeaHouseNpcIds();
        Assert.True(guests.Count >= TeaHouseSystem.RequiredGuestCount);
        var pointsBefore = guests.ToDictionary(
            npcId => npcId,
            npcId => session.Village.Relationship(npcId).Points,
            StringComparer.Ordinal
        );
        Assert.True(session.BuyTeaHouseOffer(
            session.TodayTeaHouseOffer.Id
        ).Succeeded);

        var result = session.HostTeaHouseGathering();

        Assert.True(result.Succeeded);
        Assert.True(session.TeaHouse.GatheringHosted);
        Assert.Equal(
            guests.Order(StringComparer.Ordinal),
            session.TeaHouse.GatheringGuestNpcIds.Order(StringComparer.Ordinal)
        );
        foreach (var npcId in guests)
        {
            Assert.Equal(
                Math.Min(
                    VillageSystem.MaximumRelationshipPoints,
                    pointsBefore[npcId] +
                        TeaHouseSystem.GatheringRelationshipPoints
                ),
                session.Village.Relationship(npcId).Points
            );
        }

        AssertUnchanged(
            session,
            session.HostTeaHouseGathering,
            "tea_house.gathering.already_hosted"
        );
    }

    [Fact]
    public void GatheringFailuresAndInvalidSaveAreNormalizedWithoutProgress()
    {
        var slot = FindGatheringSlot();
        var noTea = TeaHouseSession(slot.Day, slot.MinuteOfDay);
        AssertUnchanged(
            noTea,
            noTea.HostTeaHouseGathering,
            "tea_house.gathering.needs_tea"
        );

        var isolated = new TeaHouseSystem();
        isolated.Reset(2);
        var offer = TeaHouseSystem.OfferForDay(2);
        isolated.RecordPurchase(offer.Id, 2, 13 * 60);
        var isolatedBefore = JsonSerializer.Serialize(isolated.Capture());
        var failed = isolated.RecordGathering(
            2,
            13 * 60,
            [VillageCatalog.VessaId],
            out _
        );
        Assert.False(failed.Succeeded);
        Assert.Equal("tea_house.gathering.needs_guests", failed.MessageKey);
        Assert.Equal(
            isolatedBefore,
            JsonSerializer.Serialize(isolated.Capture())
        );

        var invalid = TeaHouseSystem.NormalizeSave(
            new TeaHouseSave
            {
                Day = 2,
                PurchasedOfferId = "unknown_offer",
                ActiveEffectId = "unknown_effect",
                EffectExpiresMinuteOfDay = 9999,
                GatheringHosted = true,
                GatheringGuestNpcIds =
                [
                    VillageCatalog.VessaId,
                    "unknown_npc"
                ]
            },
            2
        );
        Assert.Empty(invalid.PurchasedOfferId);
        Assert.Empty(invalid.ActiveEffectId);
        Assert.Equal(0, invalid.EffectExpiresMinuteOfDay);
        Assert.False(invalid.GatheringHosted);
        Assert.Empty(invalid.GatheringGuestNpcIds);

        var wrongDayOffer = TeaHouseSystem.OfferForDay(1);
        var wrongDay = TeaHouseSystem.NormalizeSave(
            new TeaHouseSave
            {
                Day = 2,
                PurchasedOfferId = wrongDayOffer.Id,
                ActiveEffectId = wrongDayOffer.EffectId,
                EffectExpiresMinuteOfDay = 15 * 60
            },
            2
        );
        Assert.Empty(wrongDay.PurchasedOfferId);
        Assert.Empty(wrongDay.ActiveEffectId);
        Assert.Equal(0, wrongDay.EffectExpiresMinuteOfDay);
    }

    [Fact]
    public void SaveRoundTripPersistsSameDayStateAndNextDayClearsIt()
    {
        var slot = FindGatheringSlot();
        var session = TeaHouseSession(slot.Day, slot.MinuteOfDay);
        Assert.True(session.BuyTeaHouseOffer(
            session.TodayTeaHouseOffer.Id
        ).Succeeded);
        Assert.True(session.HostTeaHouseGathering().Succeeded);
        var captured = session.Capture();

        var restored = new GameSession();
        restored.Restore(captured);
        Assert.Equal(
            JsonSerializer.Serialize(captured.TeaHouse),
            JsonSerializer.Serialize(restored.Capture().TeaHouse)
        );
        Assert.NotNull(restored.ActiveTeaHouseEffect);

        restored.EndDay();
        Assert.Equal(slot.Day + 1, restored.Clock.Day);
        Assert.Empty(restored.TeaHouse.PurchasedOfferId);
        Assert.False(restored.TeaHouse.GatheringHosted);
        Assert.Null(restored.ActiveTeaHouseEffect);
    }

    [Fact]
    public void TeaCounterPreviewOpensMenuAndWrongToolKeepsWarmWarning()
    {
        var session = TeaHouseSession();
        var available = session.PreviewSelectedTarget(
            VillageCatalog.StarwovenTeaCounterCell
        );
        Assert.Equal(TargetPreviewState.Available, available.State);
        Assert.Equal("target.action.open_tea_menu", available.LabelKey);
        Assert.Equal(TargetPreviewKind.Station, available.Kind);

        session.Inventory.Select(1);
        var wrongTool = session.PreviewSelectedTarget(
            VillageCatalog.StarwovenTeaCounterCell
        );
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.Equal("target.need.hand", wrongTool.LabelKey);
        Assert.Equal(TargetPreviewKind.Station, wrongTool.Kind);
    }

    private static GameSession TeaHouseSession(
        int day = 1,
        int minuteOfDay = 13 * 60,
        int coins = 500,
        int selectedSlot = 0
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Coins = coins;
        save.Player.Energy = 50;
        save.Player.SelectedSlot = selectedSlot;
        save.Player.LocationId = PlayerLocationIds.StarweaverTeaHouse;
        save.Player.X = VillageCatalog.StarwovenTeaCounterCell.X * 16 + 8;
        save.Player.Y =
            (VillageCatalog.StarwovenTeaCounterCell.Y + 1) * 16 + 8;
        session.Restore(save);
        return session;
    }

    private static void FillInventory(GameSession session, string excludedItemId)
    {
        var save = session.Capture();
        var fillers = DataCatalog.Items.Values
            .Where(item => item.Kind != ItemKind.Tool)
            .Where(item => item.Id != excludedItemId)
            .DistinctBy(item => item.Id, StringComparer.Ordinal)
            .Take(Inventory.SlotCount - Inventory.StartingToolCount)
            .Select(item => new InventorySlot
            {
                ItemId = item.Id,
                Count = item.MaxStack
            })
            .ToList();
        Assert.Equal(
            Inventory.SlotCount - Inventory.StartingToolCount,
            fillers.Count
        );
        save.Inventory.AddRange(fillers);
        session.Restore(save);
        Assert.False(session.Inventory.CanAdd(excludedItemId, 1));
    }

    private static (int Day, int MinuteOfDay) FindGatheringSlot()
    {
        var session = new GameSession();
        session.NewGame();
        for (var day = 1; day <= CalendarSystem.DaysPerYear; day++)
        {
            for (var minute = TeaHouseSystem.GatheringStartMinute;
                 minute < TeaHouseSystem.GatheringEndMinute;
                 minute += GameClock.MinutesPerTick)
            {
                var count = session.Village.CurrentNpcs(
                    day,
                    minute,
                    PlayerLocationIds.StarweaverTeaHouse,
                    new GridPosition(20, 18)
                ).Count;
                if (count >= TeaHouseSystem.RequiredGuestCount)
                {
                    return (day, minute);
                }
            }
        }

        throw new InvalidOperationException(
            "No tea-house gathering slot has at least two projected guests."
        );
    }

    private static void AssertUnchanged(
        GameSession session,
        Func<ActionResult> action,
        string expectedMessageKey
    )
    {
        var before = JsonSerializer.Serialize(session.Capture());
        var result = action();
        Assert.False(result.Succeeded);
        Assert.Equal(expectedMessageKey, result.MessageKey);
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }
}
