namespace Luminfield.Core;

public sealed partial class GameSession
{
    public PostDeliveryBoardSnapshot TodayPostDeliveryBoard =>
        PostDelivery.BoardForDay(Clock.Day);

    public PostDeliveryRouteDefinition? ActivePostDeliveryRoute =>
        PostDelivery.ActiveRouteForDay(Clock.Day);

    public ActionResult AcceptPostDelivery(string routeId)
    {
        var access = CheckPostDeliveryCounterAccess();
        if (!access.Succeeded)
        {
            return access;
        }

        return PostDelivery.Accept(routeId, Clock.Day);
    }

    public PostDeliveryRouteDefinition? PostDeliveryRouteAt(
        GridPosition target
    )
    {
        var route = ActivePostDeliveryRoute;
        if (route is null)
        {
            return null;
        }

        var npc = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationId,
            PlayerCell
        );
        return npc?.Definition.Id == route.TargetNpcId ? route : null;
    }

    public ActionResult CheckPostDeliveryToVillager(GridPosition target)
    {
        var route = PostDeliveryRouteAt(target);
        if (route is null)
        {
            return ActionResult.Fail("post.delivery.wrong_recipient");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        var interaction = Village.CheckInteraction(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationId,
            DataCatalog.HandId,
            PlayerCell
        );
        if (!interaction.IsAvailable || interaction.Npc is null)
        {
            return ActionResult.Fail(interaction.FailureKey);
        }

        return PostDelivery.CheckDelivery(
            interaction.Npc.Definition.Id,
            Clock.Day
        );
    }

    public ActionResult DeliverPostToVillager(
        GridPosition target,
        out PostDeliveryCompletion? completion
    )
    {
        completion = null;
        var check = CheckPostDeliveryToVillager(target);
        if (!check.Succeeded)
        {
            return check;
        }

        var npc = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationId,
            PlayerCell
        );
        if (npc is null)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        BeginChangedBatch();
        try
        {
            var result = PostDelivery.Complete(
                npc.Definition.Id,
                Clock.Day,
                out completion
            );
            if (!result.Succeeded || completion is null)
            {
                return result;
            }

            Coins += completion.RewardCoins;
            Village.AddRelationshipPoints(
                [npc.Definition.Id],
                completion.RelationshipPoints
            );
            NotifyChanged();
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    private ActionResult CheckPostDeliveryCounterAccess()
    {
        if (!InsideStarlightPost)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return VillageCatalog.IsStarlightPostOpen(Clock.MinuteOfDay)
            ? ActionResult.Success(messageKey: "post.delivery.board.opened")
            : ActionResult.Fail("notice.starlight_post_closed");
    }
}
