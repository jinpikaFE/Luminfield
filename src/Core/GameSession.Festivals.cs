namespace Luminfield.Core;

public sealed partial class GameSession
{
    public ActionResult CheckFestivalEntrance(
        string festivalId,
        GridPosition target
    )
    {
        if (!FestivalSpatialCatalog.TryByFestivalId(
                festivalId,
                out var spatial
            ) || PlayerLocationId != PlayerLocationIds.World ||
            target != spatial.WorldEntryCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return FestivalCatalog.IsOpen(
            festivalId,
            Clock.Day,
            Clock.MinuteOfDay
        )
            ? ActionResult.Success(
                messageKey: FestivalCatalog.EnterNoticeKey(festivalId)
            )
            : ActionResult.Fail(FestivalCatalog.ClosedKey(festivalId));
    }

    public ActionResult TryEnterFestival(
        string festivalId,
        GridPosition target
    )
    {
        var result = CheckFestivalEntrance(festivalId, target);
        if (result.Succeeded &&
            festivalId == FestivalCatalog.GleamrisePlantingFestivalId)
        {
            GleamriseSeason.RecordMilestone(
                GleamriseSeasonGoalSystem.CounterFestivalJoined
            );
        }

        return result;
    }

    public ActionResult CheckStarharvestMarketEntrance(GridPosition target) =>
        CheckFestivalEntrance(
            FestivalCatalog.StarharvestMarketFestivalId,
            target
        );

    public ActionResult TryEnterStarharvestMarket(GridPosition target) =>
        TryEnterFestival(
            FestivalCatalog.StarharvestMarketFestivalId,
            target
        );

    public ActionResult CheckFestivalExit(GridPosition target)
    {
        if (!FestivalSpatialCatalog.TryByLocationId(
                PlayerLocationId,
                out var spatial
            ) || target != spatial.ExitCell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return Inventory.Selected.ItemId == DataCatalog.HandId
            ? ActionResult.Success(
                messageKey: FestivalCatalog.LeaveNoticeKey(
                    spatial.FestivalId
                )
            )
            : ActionResult.Fail("notice.needs_hand");
    }

    public ActionResult TryExitFestival(GridPosition target) =>
        CheckFestivalExit(target);

    public ActionResult CheckStarharvestMarketExit(GridPosition target) =>
        InsideStarharvestMarket
            ? CheckFestivalExit(target)
            : ActionResult.Fail("notice.nothing_to_interact");

    public ActionResult TryExitStarharvestMarket(GridPosition target) =>
        CheckStarharvestMarketExit(target);

    public ActionResult CheckFestivalStation(
        string stationId,
        GridPosition target
    )
    {
        if (!FestivalSpatialCatalog.TryByLocationId(
                PlayerLocationId,
                out var spatial
            ))
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var station = spatial.Stations.FirstOrDefault(entry =>
            entry.Id == stationId);
        if (station is null || target != station.Cell ||
            Distance(PlayerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        if (!FestivalCatalog.IsOpen(
                spatial.FestivalId,
                Clock.Day,
                Clock.MinuteOfDay
            ))
        {
            return ActionResult.Fail(
                FestivalCatalog.ClosedKey(spatial.FestivalId)
            );
        }

        return ActionResult.Success(messageKey: station.Id switch
        {
            FestivalCatalog.StarharvestShopId => "festival.shop.opened",
            FestivalCatalog.GleamriseSharedBloomfieldActivityId =>
                "festival.gleamrise.activity.opened",
            FestivalCatalog.GleamriseSeedExchangeId =>
                "festival.gleamrise.exchange.opened",
            FestivalCatalog.LongnightSharedTableId =>
                "festival.longnight.activity.opened",
            FestivalCatalog.LongnightGiftExchangeId =>
                "festival.longnight.exchange.opened",
            FestivalCatalog.LongnightStarlightRiteId =>
                "festival.longnight.rite.opened",
            FestivalCatalog.LongnightLanternStallId =>
                "festival.longnight.stall.opened",
            FestivalCatalog.FireflyLanternLaunchId =>
                "festival.firefly.activity.opened",
            FestivalCatalog.FireflyFishBasinId =>
                "festival.firefly.basin.opened",
            FestivalCatalog.FireflyTideAltarId =>
                "festival.firefly.altar.opened",
            FestivalCatalog.FireflyGlowshopId =>
                "festival.firefly.shop.opened",
            _ => "festival.showcase.opened"
        });
    }

    public ActionResult CheckFestivalStation(GridPosition target)
    {
        if (!InsideStarharvestMarket)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var station = FestivalSpatialCatalog.StarharvestMarket.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        return station is null
            ? ActionResult.Fail("notice.nothing_to_interact")
            : CheckFestivalStation(station.Id, target);
    }

    public FestivalSubmissionPreview PreviewFestivalSubmission(
        IReadOnlyList<string>? itemIds
    )
    {
        if (!InsideStarharvestMarket ||
            !FestivalCatalog.IsOpen(
                FestivalCatalog.StarharvestMarketFestivalId,
                Clock.Day,
                Clock.MinuteOfDay
            ))
        {
            return new FestivalSubmissionPreview(
                false,
                "festival.starharvest.closed",
                itemIds?.ToArray() ?? [],
                0,
                string.Empty,
                0,
                0
            );
        }

        return Festival.CheckSubmission(
            FestivalCatalog.StarharvestMarketFestivalId,
            CalendarSystem.YearNumber(Clock.Day),
            itemIds,
            Inventory
        );
    }

    public FestivalSubmissionResult SubmitFestivalExhibit(
        IReadOnlyList<string> itemIds
    )
    {
        var preview = PreviewFestivalSubmission(itemIds);
        if (!preview.CanSubmit)
        {
            return new FestivalSubmissionResult(false, preview.FailureKey);
        }

        BeginChangedBatch();
        try
        {
            var result = Festival.Submit(
                FestivalCatalog.StarharvestMarketFestivalId,
                CalendarSystem.YearNumber(Clock.Day),
                itemIds,
                Inventory
            );
            if (!result.Succeeded || result.Result is null)
            {
                return result;
            }

            Coins += result.Result.AuctionCoins;
            Starlight.RefreshRewardUnlocks(StarlightProgress());
            NotifyChanged();
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPurchaseCheck CheckFestivalPurchase(string offerId)
    {
        if (!InsideStarharvestMarket ||
            !FestivalCatalog.IsOpen(
                FestivalCatalog.StarharvestMarketFestivalId,
                Clock.Day,
                Clock.MinuteOfDay
            ))
        {
            return new FestivalPurchaseCheck(
                false,
                "festival.starharvest.closed",
                null
            );
        }

        return Festival.CheckPurchase(offerId, Inventory);
    }

    public ActionResult BuyFestivalItem(string offerId)
    {
        var check = CheckFestivalPurchase(offerId);
        if (!check.CanPurchase)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        BeginChangedBatch();
        try
        {
            return Festival.Purchase(offerId, Inventory);
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalLongnightPreview CheckLongnightFeastParticipation(
        GridPosition target,
        IReadOnlyList<string>? dishItemIds,
        string exchangeId
    )
    {
        var station = FestivalSpatialCatalog.LongnightLanternFeast.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        if (station is null || station.Id is not (
                FestivalCatalog.LongnightSharedTableId or
                FestivalCatalog.LongnightGiftExchangeId or
                FestivalCatalog.LongnightStarlightRiteId))
        {
            return InvalidLongnightPreview(
                "notice.nothing_to_interact",
                dishItemIds
            );
        }

        var access = CheckFestivalStation(station.Id, target);
        if (!access.Succeeded)
        {
            return InvalidLongnightPreview(
                access.MessageKey,
                dishItemIds
            );
        }

        return Festival.CheckLongnightContribution(
            CalendarSystem.YearNumber(Clock.Day),
            dishItemIds,
            exchangeId,
            Inventory
        );
    }

    public FestivalLongnightResult CompleteLongnightFeast(
        GridPosition target,
        IReadOnlyList<string> dishItemIds,
        string exchangeId
    )
    {
        var preview = CheckLongnightFeastParticipation(
            target,
            dishItemIds,
            exchangeId
        );
        if (!preview.CanComplete)
        {
            return new FestivalLongnightResult(
                false,
                preview.FailureKey
            );
        }

        BeginChangedBatch();
        try
        {
            var result = Festival.SubmitLongnightContribution(
                CalendarSystem.YearNumber(Clock.Day),
                dishItemIds,
                exchangeId,
                Inventory
            );
            if (result.Succeeded)
            {
                Starlight.RefreshRewardUnlocks(StarlightProgress());
                NotifyChanged();
            }
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPurchaseCheck CheckLongnightStallPurchase(
        GridPosition target,
        string offerId
    )
    {
        var access = CheckFestivalStation(
            FestivalCatalog.LongnightLanternStallId,
            target
        );
        return access.Succeeded
            ? Festival.CheckLongnightPurchase(offerId, Inventory)
            : new FestivalPurchaseCheck(false, access.MessageKey, null);
    }

    public ActionResult BuyLongnightStallItem(
        GridPosition target,
        string offerId
    )
    {
        var check = CheckLongnightStallPurchase(target, offerId);
        if (!check.CanPurchase)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        BeginChangedBatch();
        try
        {
            return Festival.PurchaseLongnightOffer(offerId, Inventory);
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalSubmissionPreview CheckFireflyTideParticipation(
        GridPosition target,
        IReadOnlyList<string>? fishItemIds
    )
    {
        var station = FestivalSpatialCatalog.FireflyTide.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        if (station is null || station.Id is not (
                FestivalCatalog.FireflyLanternLaunchId or
                FestivalCatalog.FireflyFishBasinId or
                FestivalCatalog.FireflyTideAltarId))
        {
            return new FestivalSubmissionPreview(
                false,
                "notice.nothing_to_interact",
                fishItemIds?.ToArray() ?? [],
                0,
                string.Empty,
                0,
                0
            );
        }

        var access = CheckFestivalStation(station.Id, target);
        if (!access.Succeeded)
        {
            return new FestivalSubmissionPreview(
                false,
                access.MessageKey,
                fishItemIds?.ToArray() ?? [],
                0,
                string.Empty,
                0,
                0
            );
        }

        return Festival.CheckFireflyTideContribution(
            CalendarSystem.YearNumber(Clock.Day),
            fishItemIds,
            Inventory
        );
    }

    public FestivalSubmissionResult CompleteFireflyTide(
        GridPosition target,
        IReadOnlyList<string> fishItemIds
    )
    {
        var preview = CheckFireflyTideParticipation(target, fishItemIds);
        if (!preview.CanSubmit)
        {
            return new FestivalSubmissionResult(
                false,
                preview.FailureKey
            );
        }

        BeginChangedBatch();
        try
        {
            var result = Festival.SubmitFireflyTideContribution(
                CalendarSystem.YearNumber(Clock.Day),
                fishItemIds,
                Inventory
            );
            if (result.Succeeded)
            {
                Starlight.RefreshRewardUnlocks(StarlightProgress());
                NotifyChanged();
            }
            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPurchaseCheck CheckFireflyShopPurchase(
        GridPosition target,
        string offerId
    )
    {
        var access = CheckFestivalStation(
            FestivalCatalog.FireflyGlowshopId,
            target
        );
        return access.Succeeded
            ? Festival.CheckFireflyPurchase(offerId, Inventory)
            : new FestivalPurchaseCheck(false, access.MessageKey, null);
    }

    public ActionResult BuyFireflyShopItem(
        GridPosition target,
        string offerId
    )
    {
        var check = CheckFireflyShopPurchase(target, offerId);
        if (!check.CanPurchase)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        BeginChangedBatch();
        try
        {
            return Festival.PurchaseFireflyOffer(offerId, Inventory);
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPlantingStartCheck CheckStartGleamriseChallenge(
        IReadOnlyList<string>? selectedSeedItemIds
    )
    {
        var station = CheckFestivalStation(
            FestivalCatalog.GleamriseSharedBloomfieldActivityId,
            GleamrisePlantingFestivalLayout.ActivityTableCell
        );
        if (!station.Succeeded)
        {
            return new FestivalPlantingStartCheck(
                false,
                station.MessageKey,
                selectedSeedItemIds?.ToArray() ?? []
            );
        }

        return Festival.CheckStartPlantingChallenge(
            CalendarSystem.YearNumber(Clock.Day),
            Clock.MinuteOfDay,
            selectedSeedItemIds
        );
    }

    public ActionResult StartGleamriseChallenge(
        IReadOnlyList<string> selectedSeedItemIds
    )
    {
        var check = CheckStartGleamriseChallenge(selectedSeedItemIds);
        if (!check.CanStart)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        BeginChangedBatch();
        try
        {
            return Festival.StartPlantingChallenge(
                CalendarSystem.YearNumber(Clock.Day),
                Clock.MinuteOfDay,
                selectedSeedItemIds
            );
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public ActionResult CheckSelectGleamriseSeed(string seedItemId)
    {
        var station = CheckFestivalStation(
            FestivalCatalog.GleamriseSharedBloomfieldActivityId,
            GleamrisePlantingFestivalLayout.ActivityTableCell
        );
        return station.Succeeded
            ? Festival.CheckSelectPlantingSeed(
                CalendarSystem.YearNumber(Clock.Day),
                seedItemId
            )
            : station;
    }

    public ActionResult SelectGleamriseSeed(string seedItemId)
    {
        var check = CheckSelectGleamriseSeed(seedItemId);
        if (!check.Succeeded)
        {
            return check;
        }

        BeginChangedBatch();
        try
        {
            return Festival.SelectPlantingSeed(
                CalendarSystem.YearNumber(Clock.Day),
                seedItemId
            );
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPlantingCheck CheckGleamrisePlot(GridPosition target)
    {
        if (!InsideGleamrisePlantingFestival ||
            !GleamrisePlantingFestivalLayout.PlotIdsByCell.TryGetValue(
                target,
                out var plotId
            ) || Distance(PlayerCell, target) != 1)
        {
            return new FestivalPlantingCheck(
                false,
                "notice.nothing_to_interact",
                string.Empty,
                string.Empty
            );
        }

        if (Inventory.Selected.ItemId != DataCatalog.HandId)
        {
            return new FestivalPlantingCheck(
                false,
                "notice.needs_hand",
                plotId,
                string.Empty
            );
        }

        if (!FestivalCatalog.IsOpen(
                FestivalCatalog.GleamrisePlantingFestivalId,
                Clock.Day,
                Clock.MinuteOfDay
            ))
        {
            return new FestivalPlantingCheck(
                false,
                "festival.gleamrise.closed",
                plotId,
                string.Empty
            );
        }

        return Festival.CheckPlantingPlot(
            CalendarSystem.YearNumber(Clock.Day),
            Clock.MinuteOfDay,
            plotId
        );
    }

    public FestivalPlantingResolution PlantGleamrisePlot(
        GridPosition target
    )
    {
        var check = CheckGleamrisePlot(target);
        if (!check.CanPlant)
        {
            return new FestivalPlantingResolution(
                false,
                false,
                check.FailureKey
            );
        }

        BeginChangedBatch();
        try
        {
            var result = Festival.PlantPlot(
                CalendarSystem.YearNumber(Clock.Day),
                Clock.MinuteOfDay,
                check.PlotId
            );
            if (result.Completed)
            {
                Starlight.RefreshRewardUnlocks(StarlightProgress());
            }

            return result;
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPurchaseCheck CheckGleamriseSeedPurchase(
        string offerId
    )
    {
        var station = CheckFestivalStation(
            FestivalCatalog.GleamriseSeedExchangeId,
            GleamrisePlantingFestivalLayout.SeedExchangeCell
        );
        return station.Succeeded
            ? Festival.CheckGleamrisePurchase(offerId, Inventory)
            : new FestivalPurchaseCheck(false, station.MessageKey, null);
    }

    public ActionResult BuyGleamriseSeeds(string offerId)
    {
        var check = CheckGleamriseSeedPurchase(offerId);
        if (!check.CanPurchase)
        {
            return ActionResult.Fail(check.FailureKey);
        }

        BeginChangedBatch();
        try
        {
            return Festival.PurchaseGleamriseSeeds(offerId, Inventory);
        }
        finally
        {
            EndChangedBatch();
        }
    }

    public FestivalPlantingResolution ResolveGleamriseChallengeDeadline(
        bool force = false
    )
    {
        var result = Festival.ResolvePlantingAttempt(
            CalendarSystem.YearNumber(Clock.Day),
            Clock.MinuteOfDay,
            force
        );
        if (result.Completed)
        {
            Starlight.RefreshRewardUnlocks(StarlightProgress());
        }

        return result;
    }

    public bool LeaveFestivalIfClosed()
    {
        if (!FestivalSpatialCatalog.TryByLocationId(
                PlayerLocationId,
                out var spatial
            ) ||
            FestivalCatalog.IsOpen(
                spatial.FestivalId,
                Clock.Day,
                Clock.MinuteOfDay
            ))
        {
            return false;
        }

        if (spatial.FestivalId ==
            FestivalCatalog.GleamrisePlantingFestivalId)
        {
            _ = ResolveGleamriseChallengeDeadline(true);
        }

        SetPlayerLocation(
            spatial.WorldReturnCell.X * 16 + 8,
            spatial.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        return true;
    }

    private ActionResult UseStarharvestMarketSelected(GridPosition target)
    {
        if (target == StarharvestMarketLayout.ExitCell)
        {
            return TryExitStarharvestMarket(target);
        }

        if (target == StarharvestMarketLayout.ExhibitCell ||
            target == StarharvestMarketLayout.BidBoardCell ||
            target == StarharvestMarketLayout.ShopCell)
        {
            return CheckFestivalStation(target);
        }

        return ActionResult.Fail("notice.nothing_to_interact");
    }

    private ActionResult UseGleamrisePlantingFestivalSelected(
        GridPosition target
    )
    {
        if (target == GleamrisePlantingFestivalLayout.ExitCell)
        {
            return TryExitFestival(target);
        }

        if (GleamrisePlantingFestivalLayout.PlotIdsByCell.ContainsKey(target))
        {
            var planted = PlantGleamrisePlot(target);
            return planted.Succeeded
                ? ActionResult.Success(messageKey: planted.MessageKey)
                : ActionResult.Fail(planted.MessageKey);
        }

        var station = FestivalSpatialCatalog.GleamrisePlanting.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        return station is null
            ? ActionResult.Fail("notice.nothing_to_interact")
            : CheckFestivalStation(station.Id, target);
    }

    private ActionResult UseLongnightLanternFeastSelected(
        GridPosition target
    )
    {
        if (target == LongnightLanternFeastLayout.ExitCell)
        {
            return TryExitFestival(target);
        }

        var station = FestivalSpatialCatalog.LongnightLanternFeast.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        return station is null
            ? ActionResult.Fail("notice.nothing_to_interact")
            : CheckFestivalStation(station.Id, target);
    }

    private ActionResult UseFireflyTideSelected(GridPosition target)
    {
        if (target == FireflyTideLayout.ExitCell)
        {
            return TryExitFestival(target);
        }

        var station = FestivalSpatialCatalog.FireflyTide.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        return station is null
            ? ActionResult.Fail("notice.nothing_to_interact")
            : CheckFestivalStation(station.Id, target);
    }

    private TargetPreview PreviewFestivalEntrance(
        string festivalId,
        GridPosition target
    )
    {
        if (!FestivalSpatialCatalog.TryByFestivalId(
                festivalId,
                out var spatial
            ))
        {
            return TargetPreview.Neutral(target);
        }

        var check = CheckFestivalEntrance(festivalId, target);
        var actionKey = FestivalCatalog.EnterActionKey(festivalId);
        var blockedKey = FestivalCatalog.ClosedKey(festivalId);
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                spatial.WorldEntryCell,
                TargetPreviewKind.FestivalPortal,
                actionKey
            );
        }

        if (check.MessageKey == "notice.needs_hand")
        {
            return TargetPreview.NeedsTool(
                spatial.WorldEntryCell,
                TargetPreviewKind.FestivalPortal,
                "target.need.hand"
            );
        }

        return TargetPreview.Blocked(
            spatial.WorldEntryCell,
            TargetPreviewKind.FestivalPortal,
            blockedKey
        );
    }

    private TargetPreview PreviewStarharvestMarketEntrance(
        GridPosition target
    ) => PreviewFestivalEntrance(
        FestivalCatalog.StarharvestMarketFestivalId,
        target
    );

    private TargetPreview PreviewStarharvestMarketTarget(GridPosition target)
    {
        if (!StarharvestMarketLayout.IsInBounds(target))
        {
            return TargetPreview.Neutral(target);
        }

        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.StarharvestMarket,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(
                villager,
                Inventory.Selected.IsEmpty
                    ? string.Empty
                    : Inventory.Selected.ItemId
            );
        }

        if (target == StarharvestMarketLayout.ExitCell)
        {
            var check = CheckStarharvestMarketExit(target);
            return check.Succeeded
                ? TargetPreview.Available(
                    target,
                    TargetPreviewKind.FestivalExit,
                    "target.action.exit_starharvest_market"
                )
                : check.MessageKey == "notice.needs_hand"
                    ? TargetPreview.NeedsTool(
                        target,
                        TargetPreviewKind.FestivalExit,
                        "target.need.hand"
                    )
                    : TargetPreview.Neutral(target);
        }

        var kind = target == StarharvestMarketLayout.ExhibitCell
            ? TargetPreviewKind.FestivalExhibit
            : target == StarharvestMarketLayout.BidBoardCell
                ? TargetPreviewKind.FestivalBidBoard
                : target == StarharvestMarketLayout.ShopCell
                    ? TargetPreviewKind.FestivalShop
                    : TargetPreviewKind.None;
        if (kind == TargetPreviewKind.None)
        {
            return TargetPreview.Neutral(target);
        }

        var station = CheckFestivalStation(target);
        if (station.Succeeded)
        {
            var actionKey = kind switch
            {
                TargetPreviewKind.FestivalShop =>
                    "target.action.open_festival_shop",
                TargetPreviewKind.FestivalBidBoard =>
                    "target.action.review_festival_bid",
                _ => "target.action.open_festival_showcase"
            };
            return TargetPreview.Available(target, kind, actionKey);
        }

        if (station.MessageKey == "notice.needs_hand")
        {
            return TargetPreview.NeedsTool(
                target,
                kind,
                "target.need.hand"
            );
        }

        return TargetPreview.Blocked(
            target,
            kind,
            station.MessageKey
        );
    }

    private TargetPreview PreviewGleamrisePlantingFestivalTarget(
        GridPosition target
    )
    {
        if (!GleamrisePlantingFestivalLayout.IsInBounds(target))
        {
            return TargetPreview.Neutral(target);
        }

        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.GleamrisePlantingFestival,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(
                villager,
                Inventory.Selected.IsEmpty
                    ? string.Empty
                    : Inventory.Selected.ItemId
            );
        }

        if (target == GleamrisePlantingFestivalLayout.ExitCell)
        {
            var exit = CheckFestivalExit(target);
            return exit.Succeeded
                ? TargetPreview.Available(
                    target,
                    TargetPreviewKind.FestivalExit,
                    "target.action.exit_gleamrise_festival"
                )
                : exit.MessageKey == "notice.needs_hand"
                    ? TargetPreview.NeedsTool(
                        target,
                        TargetPreviewKind.FestivalExit,
                        "target.need.hand"
                    )
                    : TargetPreview.Neutral(target);
        }

        if (GleamrisePlantingFestivalLayout.PlotIdsByCell.ContainsKey(target))
        {
            var planting = CheckGleamrisePlot(target);
            if (planting.CanPlant)
            {
                return TargetPreview.Available(
                    target,
                    TargetPreviewKind.FestivalPlantingPlot,
                    "target.action.plant_festival_seed"
                );
            }

            return planting.FailureKey == "notice.needs_hand"
                ? TargetPreview.NeedsTool(
                    target,
                    TargetPreviewKind.FestivalPlantingPlot,
                    "target.need.hand"
                )
                : TargetPreview.Blocked(
                    target,
                    TargetPreviewKind.FestivalPlantingPlot,
                    planting.FailureKey
                );
        }

        var station = FestivalSpatialCatalog.GleamrisePlanting.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        if (station is null)
        {
            return TargetPreview.Neutral(target);
        }

        var check = CheckFestivalStation(station.Id, target);
        var actionKey = station.PreviewKind ==
            TargetPreviewKind.FestivalSeedExchange
                ? "target.action.open_sowing_seed_exchange"
                : "target.action.open_sowing_activity";
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                target,
                station.PreviewKind,
                actionKey
            );
        }

        return check.MessageKey == "notice.needs_hand"
            ? TargetPreview.NeedsTool(
                target,
                station.PreviewKind,
                "target.need.hand"
            )
            : TargetPreview.Blocked(
                target,
                station.PreviewKind,
                check.MessageKey
            );
    }

    private TargetPreview PreviewLongnightLanternFeastTarget(
        GridPosition target
    )
    {
        if (!LongnightLanternFeastLayout.IsInBounds(target))
        {
            return TargetPreview.Neutral(target);
        }

        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.LongnightLanternFeast,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(
                villager,
                Inventory.Selected.IsEmpty
                    ? string.Empty
                    : Inventory.Selected.ItemId
            );
        }

        if (target == LongnightLanternFeastLayout.ExitCell)
        {
            var exit = CheckFestivalExit(target);
            return exit.Succeeded
                ? TargetPreview.Available(
                    target,
                    TargetPreviewKind.FestivalExit,
                    FestivalCatalog.ExitActionKey(
                        FestivalCatalog.LongnightLanternFeastFestivalId
                    )
                )
                : exit.MessageKey == "notice.needs_hand"
                    ? TargetPreview.NeedsTool(
                        target,
                        TargetPreviewKind.FestivalExit,
                        "target.need.hand"
                    )
                    : TargetPreview.Neutral(target);
        }

        var station = FestivalSpatialCatalog.LongnightLanternFeast.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        if (station is null)
        {
            return TargetPreview.Neutral(target);
        }

        var check = CheckFestivalStation(station.Id, target);
        var completed = Festival.HasParticipated(
            FestivalCatalog.LongnightLanternFeastFestivalId,
            CalendarSystem.YearNumber(Clock.Day)
        );
        var actionKey = station.Id switch
        {
            FestivalCatalog.LongnightLanternStallId =>
                "target.action.open_longnight_stall",
            FestivalCatalog.LongnightGiftExchangeId => completed
                ? "target.action.view_longnight_result"
                : "target.action.open_longnight_exchange",
            FestivalCatalog.LongnightStarlightRiteId => completed
                ? "target.action.view_longnight_result"
                : "target.action.open_longnight_rite",
            _ => completed
                ? "target.action.view_longnight_result"
                : "target.action.open_longnight_feast"
        };
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                target,
                station.PreviewKind,
                actionKey
            );
        }

        return check.MessageKey == "notice.needs_hand"
            ? TargetPreview.NeedsTool(
                target,
                station.PreviewKind,
                "target.need.hand"
            )
            : TargetPreview.Blocked(
                target,
                station.PreviewKind,
                check.MessageKey
            );
    }

    private TargetPreview PreviewFireflyTideTarget(GridPosition target)
    {
        if (!FireflyTideLayout.IsInBounds(target))
        {
            return TargetPreview.Neutral(target);
        }

        var villager = Village.NpcAt(
            target,
            Clock.Day,
            Clock.MinuteOfDay,
            PlayerLocationIds.FireflyTide,
            PlayerCell
        );
        if (villager is not null)
        {
            return PreviewVillagerInteraction(
                villager,
                Inventory.Selected.IsEmpty
                    ? string.Empty
                    : Inventory.Selected.ItemId
            );
        }

        if (target == FireflyTideLayout.ExitCell)
        {
            var exit = CheckFestivalExit(target);
            return exit.Succeeded
                ? TargetPreview.Available(
                    target,
                    TargetPreviewKind.FestivalExit,
                    FestivalCatalog.ExitActionKey(
                        FestivalCatalog.FireflyTideFestivalId
                    )
                )
                : exit.MessageKey == "notice.needs_hand"
                    ? TargetPreview.NeedsTool(
                        target,
                        TargetPreviewKind.FestivalExit,
                        "target.need.hand"
                    )
                    : TargetPreview.Neutral(target);
        }

        var station = FestivalSpatialCatalog.FireflyTide.Stations
            .FirstOrDefault(entry => entry.Cell == target);
        if (station is null)
        {
            return TargetPreview.Neutral(target);
        }

        var check = CheckFestivalStation(station.Id, target);
        var completed = Festival.HasParticipated(
            FestivalCatalog.FireflyTideFestivalId,
            CalendarSystem.YearNumber(Clock.Day)
        );
        var actionKey = station.Id switch
        {
            FestivalCatalog.FireflyGlowshopId =>
                "target.action.open_firefly_shop",
            FestivalCatalog.FireflyFishBasinId => completed
                ? "target.action.view_firefly_result"
                : "target.action.open_firefly_basin",
            FestivalCatalog.FireflyTideAltarId => completed
                ? "target.action.view_firefly_result"
                : "target.action.open_firefly_altar",
            _ => completed
                ? "target.action.view_firefly_result"
                : "target.action.open_firefly_launch"
        };
        if (check.Succeeded)
        {
            return TargetPreview.Available(
                target,
                station.PreviewKind,
                actionKey
            );
        }

        return check.MessageKey == "notice.needs_hand"
            ? TargetPreview.NeedsTool(
                target,
                station.PreviewKind,
                "target.need.hand"
            )
            : TargetPreview.Blocked(
                target,
                station.PreviewKind,
                check.MessageKey
            );
    }

    public IReadOnlyList<AnimalProjection> VisibleAnimalProjections
    {
        get
        {
            var projections = new List<AnimalProjection>();
            foreach (var spatial in AnimalBuildingSpatialCatalog.Definitions)
            {
                var residents = Animals.AnimalsInBuilding(spatial.BuildingId);
                if (residents.Count == 0)
                {
                    continue;
                }

                var outdoor = OutdoorAnimalAssignmentsFor(spatial.BuildingId);
                var physicallyOutside =
                    Clock.MinuteOfDay >= StarfeatherCoopLayout.GrazingStartMinute &&
                    Clock.MinuteOfDay < StarfeatherCoopLayout.GrazingEndMinute;
                if (PlayerLocationId == PlayerLocationIds.World &&
                    physicallyOutside)
                {
                    projections.AddRange(residents
                        .Where(animal => outdoor.ContainsKey(animal.InstanceId))
                        .Select(animal => new AnimalProjection(
                            animal.InstanceId,
                            animal.SpeciesId,
                            animal.BuildingId,
                            PlayerLocationIds.World,
                            outdoor[animal.InstanceId],
                            true
                        )));
                    continue;
                }

                if (PlayerLocationId != spatial.LocationId)
                {
                    continue;
                }

                var indoors = residents
                    .Where(animal =>
                        !physicallyOutside ||
                        !outdoor.ContainsKey(animal.InstanceId)
                    )
                    .Zip(
                        spatial.IndoorAnimalCells,
                        (animal, cell) => new AnimalProjection(
                            animal.InstanceId,
                            animal.SpeciesId,
                            animal.BuildingId,
                            spatial.LocationId,
                            cell,
                            false
                        )
                    );
                projections.AddRange(indoors);
            }

            return projections;
        }
    }

}
