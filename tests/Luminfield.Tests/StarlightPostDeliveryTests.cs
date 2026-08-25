using System.Text.Json;
using Luminfield.Core;
using Luminfield.Game;
using Xunit;

namespace Luminfield.Tests;

public sealed class StarlightPostDeliveryTests
{
    private static readonly string[] DeliveryLocations =
    [
        PlayerLocationIds.World,
        PlayerLocationIds.MoonlitArchive,
        PlayerLocationIds.MoonstoneWorkshop,
        PlayerLocationIds.StarweaverTeaHouse,
        PlayerLocationIds.TwilightEmporium,
        PlayerLocationIds.StarlightPost,
        PlayerLocationIds.StarfallWatch
    ];

    [Fact]
    public void DailyBoardOffersTwoRoutesAndCompletesAtDailyLimit()
    {
        var system = new PostDeliverySystem();
        system.Reset(1);

        var offers = PostDeliverySystem.OffersForDay(1);

        Assert.Equal(PostDeliverySystem.DailyOfferCount, offers.Count);
        Assert.Equal(
            offers.Select(route => route.Id).Distinct(StringComparer.Ordinal),
            offers.Select(route => route.Id)
        );
        Assert.All(offers, route =>
        {
            Assert.True(VillageCatalog.Npcs.ContainsKey(route.TargetNpcId));
            Assert.True(route.RewardCoins > 0);
            Assert.False(string.IsNullOrWhiteSpace(route.NameKey));
            Assert.False(string.IsNullOrWhiteSpace(route.ResponseKey));
        });

        foreach (var route in offers)
        {
            var accepted = system.Accept(route.Id, 1);
            Assert.True(accepted.Succeeded);
            Assert.Equal(route.Id, system.ActiveRouteId);

            var delivered = system.Complete(
                route.TargetNpcId,
                1,
                out var completion
            );

            Assert.True(delivered.Succeeded);
            Assert.NotNull(completion);
            Assert.Equal(route, completion.Route);
            Assert.Equal(route.RewardCoins, completion.RewardCoins);
            Assert.Equal(
                PostDeliverySystem.RelationshipRewardPoints,
                completion.RelationshipPoints
            );
            Assert.Empty(system.ActiveRouteId);
        }

        var board = system.BoardForDay(1);
        Assert.Equal(PostDeliverySystem.DailyCompletionLimit, board.DailyLimit);
        Assert.Equal(board.DailyLimit, board.CompletedCount);
        Assert.Equal(
            offers.Select(route => route.Id).Order(StringComparer.Ordinal),
            board.CompletedRouteIds.Order(StringComparer.Ordinal)
        );
        Assert.Equal(
            "post.delivery.already_completed",
            system.CheckAccept(offers[0].Id, 1).MessageKey
        );
    }

    [Fact]
    public void AcceptRouteRequiresOpenPostCounterHandAndSingleActiveRoute()
    {
        var session = StarlightPostSession();
        var board = session.TodayPostDeliveryBoard;
        var first = board.Offers[0];
        var second = board.Offers[1];

        var accepted = session.AcceptPostDelivery(first.Id);

        Assert.True(accepted.Succeeded);
        Assert.Equal("post.delivery.accepted", accepted.MessageKey);
        Assert.Equal(first.Id, session.ActivePostDeliveryRoute?.Id);
        Assert.Equal(
            first.Id,
            session.TodayPostDeliveryBoard.ActiveRouteId
        );

        var singleActive = session.AcceptPostDelivery(second.Id);

        Assert.False(singleActive.Succeeded);
        Assert.Equal(
            "post.delivery.active_route_exists",
            singleActive.MessageKey
        );
        Assert.Equal(first.Id, session.ActivePostDeliveryRoute?.Id);

        AssertAcceptFailureKeepsPostState(
            StarlightPostSession(locationId: PlayerLocationIds.World),
            first.Id,
            "notice.nothing_to_interact"
        );
        AssertAcceptFailureKeepsPostState(
            StarlightPostSession(selectedSlot: 1),
            first.Id,
            "notice.needs_hand"
        );
        AssertAcceptFailureKeepsPostState(
            StarlightPostSession(
                minuteOfDay: VillageCatalog.StarlightPostCloseMinute
            ),
            first.Id,
            "notice.starlight_post_closed"
        );
    }

    [Fact]
    public void DeliveryUsesRealNpcProjectionAndRewardsWithoutTouchingMail()
    {
        var session = StarlightPostSession();
        var route = session.TodayPostDeliveryBoard.Offers[0];
        Assert.True(session.AcceptPostDelivery(route.Id).Succeeded);

        var scene = FindDeliveryScene(session, route.TargetNpcId);
        var wrongRecipient = PlacePlayerAdjacent(
            session,
            scene.WrongRecipient,
            scene.MinuteOfDay
        );
        var failureBefore = AtomicSnapshot(
            session,
            route.TargetNpcId
        );
        var wrongRecipientPreview = session.PreviewSelectedTarget(
            wrongRecipient.Position
        );

        var wrongCheck = session.CheckPostDeliveryToVillager(
            wrongRecipient.Position
        );
        var wrongDelivery = session.DeliverPostToVillager(
            wrongRecipient.Position,
            out var wrongCompletion
        );

        Assert.Equal(
            TargetPreviewState.Blocked,
            wrongRecipientPreview.State
        );
        Assert.Equal(
            TargetPreviewKind.Character,
            wrongRecipientPreview.Kind
        );
        Assert.Equal(
            "post.delivery.wrong_recipient",
            wrongRecipientPreview.LabelKey
        );
        Assert.Equal(
            wrongRecipient.Position,
            wrongRecipientPreview.Target
        );
        Assert.False(wrongCheck.Succeeded);
        Assert.Equal("post.delivery.wrong_recipient", wrongCheck.MessageKey);
        Assert.False(wrongDelivery.Succeeded);
        Assert.Equal(
            "post.delivery.wrong_recipient",
            wrongDelivery.MessageKey
        );
        Assert.Null(wrongCompletion);
        Assert.Equal(
            failureBefore,
            AtomicSnapshot(session, route.TargetNpcId)
        );

        var target = PlacePlayerAdjacent(
            session,
            scene.Target,
            scene.MinuteOfDay
        );
        var preview = session.PreviewSelectedTarget(target.Position);
        Assert.Equal(TargetPreviewState.Available, preview.State);
        Assert.Equal(TargetPreviewKind.Character, preview.Kind);
        Assert.Equal("target.action.deliver_post", preview.LabelKey);
        Assert.Equal(target.Position, preview.Target);

        session.Inventory.Select(1);
        var wrongToolPreview = session.PreviewSelectedTarget(target.Position);
        var wrongToolBefore = AtomicSnapshot(session, route.TargetNpcId);

        var wrongToolDelivery = session.DeliverPostToVillager(
            target.Position,
            out var wrongToolCompletion
        );

        Assert.Equal(TargetPreviewState.NeedsTool, wrongToolPreview.State);
        Assert.Equal("target.need.hand", wrongToolPreview.LabelKey);
        Assert.False(wrongToolDelivery.Succeeded);
        Assert.Equal("notice.needs_hand", wrongToolDelivery.MessageKey);
        Assert.Null(wrongToolCompletion);
        Assert.Equal(
            wrongToolBefore,
            AtomicSnapshot(session, route.TargetNpcId)
        );

        session.Inventory.Select(0);
        var coinsBefore = session.Coins;
        var relationshipBefore = session.Village
            .Relationship(route.TargetNpcId)
            .Points;
        var mailBefore = Serialize(session.Mail.Capture());

        var delivered = session.DeliverPostToVillager(
            target.Position,
            out var completion
        );

        Assert.True(delivered.Succeeded);
        Assert.Equal("post.delivery.completed", delivered.MessageKey);
        Assert.NotNull(completion);
        Assert.Equal(route.Id, completion.Route.Id);
        Assert.Equal(route.RewardCoins, completion.RewardCoins);
        Assert.Equal(
            PostDeliverySystem.RelationshipRewardPoints,
            completion.RelationshipPoints
        );
        Assert.Equal(coinsBefore + route.RewardCoins, session.Coins);
        Assert.Equal(
            relationshipBefore +
                PostDeliverySystem.RelationshipRewardPoints,
            session.Village.Relationship(route.TargetNpcId).Points
        );
        Assert.Equal(mailBefore, Serialize(session.Mail.Capture()));
        Assert.Null(session.ActivePostDeliveryRoute);
        Assert.Contains(
            route.Id,
            session.TodayPostDeliveryBoard.CompletedRouteIds
        );

        var repeatBefore = AtomicSnapshot(session, route.TargetNpcId);
        var repeated = session.DeliverPostToVillager(
            target.Position,
            out var repeatCompletion
        );

        Assert.False(repeated.Succeeded);
        Assert.Equal("post.delivery.wrong_recipient", repeated.MessageKey);
        Assert.Null(repeatCompletion);
        Assert.Equal(
            repeatBefore,
            AtomicSnapshot(session, route.TargetNpcId)
        );
    }

    [Fact]
    public void PostDeliverySaveRestoresSameDayAndResetsAcrossDays()
    {
        var session = StarlightPostSession(day: 3);
        var route = session.TodayPostDeliveryBoard.Offers[0];
        Assert.True(session.AcceptPostDelivery(route.Id).Succeeded);

        var restored = new GameSession();
        restored.Restore(session.Capture());

        Assert.Equal(3, restored.Clock.Day);
        Assert.Equal(route.Id, restored.ActivePostDeliveryRoute?.Id);
        Assert.Equal(
            Serialize(session.PostDelivery.Capture()),
            Serialize(restored.PostDelivery.Capture())
        );

        restored.EndDay();

        Assert.Equal(4, restored.Clock.Day);
        Assert.Null(restored.ActivePostDeliveryRoute);
        Assert.Empty(restored.TodayPostDeliveryBoard.ActiveRouteId);
        Assert.Empty(restored.TodayPostDeliveryBoard.CompletedRouteIds);
        Assert.Equal(
            PostDeliverySystem.OffersForDay(4).Select(offer => offer.Id),
            restored.TodayPostDeliveryBoard.Offers.Select(offer => offer.Id)
        );
    }

    [Fact]
    public void InvalidPostDeliverySaveNormalizesToCurrentDailyBoard()
    {
        var day = 5;
        var offers = PostDeliverySystem.OffersForDay(day);
        var unavailable = PostDeliverySystem.Routes
            .First(route => offers.All(offer => offer.Id != route.Id));
        var normalized = PostDeliverySystem.NormalizeSave(
            new PostDeliverySave
            {
                Day = day,
                ActiveRouteId = unavailable.Id,
                CompletedRouteIds =
                [
                    offers[0].Id,
                    "unknown_route",
                    offers[0].Id,
                    offers[1].Id,
                    unavailable.Id
                ]
            },
            day
        );

        Assert.Equal(day, normalized.Day);
        Assert.Empty(normalized.ActiveRouteId);
        Assert.Equal(
            offers.Select(route => route.Id).Order(StringComparer.Ordinal),
            normalized.CompletedRouteIds.Order(StringComparer.Ordinal)
        );

        var staleDay = PostDeliverySystem.NormalizeSave(
            new PostDeliverySave
            {
                Day = day - 1,
                ActiveRouteId = offers[0].Id,
                CompletedRouteIds = [offers[1].Id]
            },
            day
        );

        Assert.Equal(day, staleDay.Day);
        Assert.Empty(staleDay.ActiveRouteId);
        Assert.Empty(staleDay.CompletedRouteIds);

        var fullDay = PostDeliverySystem.NormalizeSave(
            new PostDeliverySave
            {
                Day = day,
                ActiveRouteId = offers[0].Id,
                CompletedRouteIds = [offers[0].Id, offers[1].Id]
            },
            day
        );

        Assert.Empty(fullDay.ActiveRouteId);
        Assert.Equal(
            PostDeliverySystem.DailyCompletionLimit,
            fullDay.CompletedRouteIds.Count
        );
    }

    [Fact]
    public void PostDeliveryOverlayLocalizationKeysCoverRoutesAndActions()
    {
        var locale = new LocaleService();
        locale.LoadJson(LocaleService.English, ReadLocale("en.json"));
        locale.LoadJson(
            LocaleService.SimplifiedChinese,
            ReadLocale("zh_CN.json")
        );

        var requiredKeys = PostDeliveryOverlay.RequiredLocalizationKeys;
        Assert.Contains("post.delivery.completed.notice", requiredKeys);
        Assert.Contains("post.delivery.ui.action.accept", requiredKeys);
        Assert.All(
            PostDeliverySystem.Routes,
            route =>
            {
                Assert.Contains(route.NameKey, requiredKeys);
                Assert.Contains(route.ResponseKey, requiredKeys);
            }
        );

        foreach (var language in new[]
                 {
                     LocaleService.English,
                     LocaleService.SimplifiedChinese
                 })
        {
            locale.SetLocale(language);
            Assert.All(
                requiredKeys,
                key => Assert.False(locale.Tr(key).StartsWith('['))
            );
        }
    }

    private static GameSession StarlightPostSession(
        int day = 1,
        int minuteOfDay = VillageCatalog.StarlightPostOpenMinute,
        int selectedSlot = 0,
        string locationId = PlayerLocationIds.StarlightPost
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = day;
        save.MinuteOfDay = minuteOfDay;
        save.Player.SelectedSlot = selectedSlot;
        save.Player.LocationId = locationId;
        save.Player.X = VillageCatalog.RouteSortingCounterCell.X * 16 + 8;
        save.Player.Y =
            (VillageCatalog.RouteSortingCounterCell.Y + 1) * 16 + 8;
        session.Restore(save);
        return session;
    }

    private static void AssertAcceptFailureKeepsPostState(
        GameSession session,
        string routeId,
        string messageKey
    )
    {
        var postBefore = Serialize(session.PostDelivery.Capture());
        var mailBefore = Serialize(session.Mail.Capture());
        var coinsBefore = session.Coins;

        var result = session.AcceptPostDelivery(routeId);

        Assert.False(result.Succeeded);
        Assert.Equal(messageKey, result.MessageKey);
        Assert.Equal(postBefore, Serialize(session.PostDelivery.Capture()));
        Assert.Equal(mailBefore, Serialize(session.Mail.Capture()));
        Assert.Equal(coinsBefore, session.Coins);
    }

    private static DeliveryScene FindDeliveryScene(
        GameSession session,
        string targetNpcId
    )
    {
        for (var minute = GameClock.StartMinute; minute <= 21 * 60; minute += 10)
        {
            foreach (var locationId in DeliveryLocations)
            {
                var npcs = session.Village.CurrentNpcs(
                        session.Clock.Day,
                        minute,
                        locationId,
                        new GridPosition(-999, -999)
                    )
                    .ToList();
                var target = npcs.FirstOrDefault(npc =>
                    npc.Definition.Id == targetNpcId
                );
                if (target is null)
                {
                    continue;
                }

                var wrongRecipient = npcs.FirstOrDefault(npc =>
                    npc.Definition.Id != targetNpcId
                );
                if (wrongRecipient is null)
                {
                    continue;
                }

                if (ApproachCell(
                    locationId,
                    target.Position,
                    npcs,
                    target.Definition.Id
                ) is null)
                {
                    continue;
                }

                if (ApproachCell(
                    locationId,
                    wrongRecipient.Position,
                    npcs,
                    wrongRecipient.Definition.Id
                ) is null)
                {
                    continue;
                }

                return new DeliveryScene(
                    minute,
                    locationId,
                    target,
                    wrongRecipient
                );
            }
        }

        throw new InvalidOperationException(
            $"Could not find delivery scene for {targetNpcId}."
        );
    }

    private static VillageNpcState PlacePlayerAdjacent(
        GameSession session,
        VillageNpcState npc,
        int minuteOfDay
    )
    {
        session.Clock.Reset(session.Clock.Day, minuteOfDay);

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var currentNpcs = session.Village.CurrentNpcs(
                    session.Clock.Day,
                    session.Clock.MinuteOfDay,
                    npc.LocationId,
                    session.PlayerCell
                )
                .ToList();
            var current = currentNpcs.Single(state =>
                state.Definition.Id == npc.Definition.Id
            );
            var approach = ApproachCell(
                npc.LocationId,
                current.Position,
                currentNpcs,
                npc.Definition.Id
            );
            Assert.NotNull(approach);

            session.SetPlayerLocation(
                approach.Value.X * 16 + 8,
                approach.Value.Y * 16 + 8,
                npc.LocationId
            );
            var projected = session.Village.CurrentNpcs(
                    session.Clock.Day,
                    session.Clock.MinuteOfDay,
                    npc.LocationId,
                    session.PlayerCell
                )
                .Single(state => state.Definition.Id == npc.Definition.Id);
            if (Distance(session.PlayerCell, projected.Position) == 1)
            {
                return projected;
            }
        }

        throw new InvalidOperationException(
            $"Could not place player adjacent to {npc.Definition.Id}."
        );
    }

    private static GridPosition? ApproachCell(
        string locationId,
        GridPosition npcPosition,
        IEnumerable<VillageNpcState> npcs,
        string npcId
    )
    {
        var occupied = npcs
            .Where(npc => npc.Definition.Id != npcId)
            .Select(npc => npc.Position)
            .ToHashSet();
        foreach (var candidate in ApproachCandidates(npcPosition))
        {
            if (NpcNavigationMap.IsWalkableGeometry(locationId, candidate) &&
                !NpcNavigationMap.IsCriticalEntranceCell(locationId, candidate) &&
                !occupied.Contains(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<GridPosition> ApproachCandidates(
        GridPosition position
    )
    {
        yield return new GridPosition(position.X, position.Y + 1);
        yield return new GridPosition(position.X - 1, position.Y);
        yield return new GridPosition(position.X + 1, position.Y);
        yield return new GridPosition(position.X, position.Y - 1);
    }

    private static AtomicDeliverySnapshot AtomicSnapshot(
        GameSession session,
        string npcId
    ) => new(
        session.Coins,
        session.Village.Relationship(npcId).Points,
        Serialize(session.PostDelivery.Capture()),
        Serialize(session.Mail.Capture()),
        Serialize(session.Inventory.Capture())
    );

    private static int Distance(GridPosition first, GridPosition second) =>
        Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value);

    private static string ReadLocale(string name) => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "localization", name)
    );

    private sealed record DeliveryScene(
        int MinuteOfDay,
        string LocationId,
        VillageNpcState Target,
        VillageNpcState WrongRecipient
    );

    private sealed record AtomicDeliverySnapshot(
        int Coins,
        int RelationshipPoints,
        string PostDeliveryJson,
        string MailJson,
        string InventoryJson
    );
}
