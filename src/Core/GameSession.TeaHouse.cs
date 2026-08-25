namespace Luminfield.Core;

public sealed partial class GameSession
{
    public TeaHouseOfferDefinition TodayTeaHouseOffer =>
        TeaHouseSystem.OfferForDay(Clock.Day);

    public TeaHouseEffectSnapshot? ActiveTeaHouseEffect =>
        TeaHouse.ActiveEffect(Clock.Day, Clock.MinuteOfDay);

    public IReadOnlyList<string> PresentTeaHouseNpcIds() =>
        Village.CurrentNpcs(
                Clock.Day,
                Clock.MinuteOfDay,
                PlayerLocationIds.StarweaverTeaHouse,
                PlayerCell
            )
            .Select(npc => npc.Definition.Id)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    public TeaHouseGatheringSnapshot TeaHouseGathering() =>
        TeaHouse.GatheringSnapshot(
            Clock.Day,
            Clock.MinuteOfDay,
            PresentTeaHouseNpcIds()
        );

    public ActionResult BuyTeaHouseOffer(string offerId)
    {
        var access = CheckTeaHouseCounterAccess();
        if (!access.Succeeded)
        {
            return access;
        }

        var check = TeaHouse.CheckPurchase(
            offerId,
            Clock.Day,
            Coins,
            Inventory
        );
        if (!check.Succeeded ||
            !TeaHouseSystem.TryOffer(offerId, out var offer))
        {
            return check;
        }

        BeginChangedBatch();
        try
        {
            if (!Inventory.Add(offer.ItemId, 1))
            {
                return ActionResult.Fail("notice.inventory_full");
            }

            Coins -= offer.Price;
            TeaHouse.RecordPurchase(
                offer.Id,
                Clock.Day,
                Clock.MinuteOfDay
            );
            var restoredEnergy = Math.Min(
                MaxEnergy,
                Energy + offer.EnergyRestore
            );
            if (restoredEnergy != Energy)
            {
                Energy = restoredEnergy;
                EnergyChanged?.Invoke();
            }
            NotifyChanged();
        }
        finally
        {
            EndChangedBatch();
        }

        return ActionResult.Grant(
            offer.ItemId,
            1,
            0,
            "tea_house.offer.purchased"
        );
    }

    public ActionResult HostTeaHouseGathering()
    {
        var access = CheckTeaHouseCounterAccess();
        if (!access.Succeeded)
        {
            return access;
        }

        var presentNpcIds = PresentTeaHouseNpcIds();
        var snapshot = TeaHouse.GatheringSnapshot(
            Clock.Day,
            Clock.MinuteOfDay,
            presentNpcIds
        );
        if (!snapshot.CanHost)
        {
            return ActionResult.Fail(snapshot.ReasonKey);
        }

        BeginChangedBatch();
        try
        {
            var result = TeaHouse.RecordGathering(
                Clock.Day,
                Clock.MinuteOfDay,
                presentNpcIds,
                out var guestNpcIds
            );
            if (!result.Succeeded)
            {
                return result;
            }

            Village.AddRelationshipPoints(
                guestNpcIds,
                TeaHouseSystem.GatheringRelationshipPoints
            );
            NotifyChanged();
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    private ActionResult CheckTeaHouseCounterAccess()
    {
        if (!InsideTeaHouse)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return VillageCatalog.IsStarweaverTeaHouseOpen(Clock.MinuteOfDay)
            ? ActionResult.Success()
            : ActionResult.Fail("notice.tea_house_closed");
    }
}
