namespace Luminfield.Core;

public sealed partial class GameSession
{
    public const int StarfallWatchFieldRationEnergyRestore = 15;

    public StarfallWatchBoardSnapshot TodayStarfallWatchBoard =>
        StarfallWatch.BoardForDay(Clock.Day);

    public float EffectiveIncomingDamageMultiplier => Math.Clamp(
        IncomingDamageMultiplier *
            StarfallWatch.IncomingDamageMultiplierForDay(Clock.Day),
        0.5f,
        1f
    );

    public float EffectiveEnemySpeedMultiplier => Math.Clamp(
        EnemySpeedMultiplier *
            StarfallWatch.EnemySpeedMultiplierForDay(Clock.Day),
        0.5f,
        1f
    );

    public ActionResult AcceptStarfallWatchPatrol(string patrolId)
    {
        var access = CheckStarfallWatchTableAccess();
        return access.Succeeded
            ? StarfallWatch.AcceptPatrol(patrolId, Clock.Day)
            : access;
    }

    public ActionResult ClaimStarfallWatchPatrolReward(
        out StarfallWatchReward? reward
    )
    {
        reward = null;
        var access = CheckStarfallWatchTableAccess();
        if (!access.Succeeded)
        {
            return access;
        }

        var check = StarfallWatch.CheckPatrolClaim(Clock.Day);
        var board = TodayStarfallWatchBoard;
        if (!check.Succeeded ||
            !StarfallWatchSystem.PatrolsById.TryGetValue(
                board.ActivePatrolId,
                out var patrol
            ))
        {
            return check;
        }

        if (!Inventory.CanAdd(patrol.RewardItemId, patrol.RewardItemCount))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        BeginChangedBatch();
        try
        {
            var result = StarfallWatch.ClaimPatrol(Clock.Day, out reward);
            if (!result.Succeeded || reward is null)
            {
                return result;
            }

            Inventory.Add(reward.RewardItemId, reward.RewardItemCount);
            Coins += reward.RewardCoins;
            Village.AddRelationshipPoints(
                [VillageCatalog.KaelId],
                reward.RelationshipPoints
            );
            NotifyChanged();
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public ActionResult AcceptStarfallWatchBounty(string bountyId)
    {
        var access = CheckStarfallWatchTableAccess();
        return access.Succeeded
            ? StarfallWatch.AcceptBounty(bountyId, Clock.Day)
            : access;
    }

    public ActionResult ClaimStarfallWatchBountyReward(
        out StarfallWatchReward? reward
    )
    {
        reward = null;
        var access = CheckStarfallWatchTableAccess();
        if (!access.Succeeded)
        {
            return access;
        }

        var check = StarfallWatch.CheckBountyClaim(Clock.Day);
        var board = TodayStarfallWatchBoard;
        if (!check.Succeeded ||
            !StarfallWatchSystem.BountiesById.TryGetValue(
                board.ActiveBountyId,
                out var bounty
            ))
        {
            return check;
        }

        if (!Inventory.CanAdd(bounty.RewardItemId, bounty.RewardItemCount))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        BeginChangedBatch();
        try
        {
            var result = StarfallWatch.ClaimBounty(Clock.Day, out reward);
            if (!result.Succeeded || reward is null)
            {
                return result;
            }

            Inventory.Add(reward.RewardItemId, reward.RewardItemCount);
            Coins += reward.RewardCoins;
            Village.AddRelationshipPoints(
                [VillageCatalog.KaelId],
                reward.RelationshipPoints
            );
            NotifyChanged();
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public ActionResult SelectStarfallWatchPreparation(
        string preparationId
    )
    {
        var access = CheckStarfallWatchTableAccess();
        return access.Succeeded
            ? StarfallWatch.SelectPreparation(preparationId, Clock.Day)
            : access;
    }

    private ActionResult CheckStarfallWatchTableAccess()
    {
        if (!InsideStarfallWatch)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return VillageCatalog.IsStarfallWatchOpen(Clock.MinuteOfDay)
            ? ActionResult.Success(messageKey: "watch.board.opened")
            : ActionResult.Fail("notice.starfall_watch_closed");
    }

    private void ApplyStarfallWatchFieldRation(
        string previousLocationId,
        bool enteringDeepMine = false
    )
    {
        var enteringRuins =
            previousLocationId != PlayerLocationIds.StarfallRuinsTrial &&
            PlayerLocationId == PlayerLocationIds.StarfallRuinsTrial;
        if (!enteringRuins && !enteringDeepMine)
        {
            return;
        }

        var result = StarfallWatch.ConsumeFieldRation(Clock.Day);
        if (!result.Succeeded)
        {
            return;
        }

        var restoredEnergy = Math.Min(
            MaxEnergy,
            Energy + StarfallWatchFieldRationEnergyRestore
        );
        if (restoredEnergy == Energy)
        {
            return;
        }

        Energy = restoredEnergy;
        EnergyChanged?.Invoke();
        NotifyChanged();
    }
}
