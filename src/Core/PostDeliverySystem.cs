namespace Luminfield.Core;

public sealed record PostDeliveryRouteDefinition(
    string Id,
    string TargetNpcId,
    int RewardCoins,
    string NameKey,
    string ResponseKey
);

public sealed record PostDeliveryBoardSnapshot(
    int Day,
    IReadOnlyList<PostDeliveryRouteDefinition> Offers,
    string ActiveRouteId,
    IReadOnlySet<string> CompletedRouteIds,
    int DailyLimit
)
{
    public bool HasActiveRoute => !string.IsNullOrWhiteSpace(ActiveRouteId);
    public int CompletedCount => CompletedRouteIds.Count;
}

public sealed record PostDeliveryCompletion(
    PostDeliveryRouteDefinition Route,
    int RewardCoins,
    int RelationshipPoints
);

public sealed class PostDeliverySystem
{
    public const int DailyOfferCount = 2;
    public const int DailyCompletionLimit = 2;
    public const int RelationshipRewardPoints = 2;

    public const string ArchiveReturnRouteId = "post_archive_return";
    public const string MoonstoneSealRouteId = "post_moonstone_seal";
    public const string TeaLeafReplyRouteId = "post_tea_leaf_reply";
    public const string TwilightInvoiceRouteId = "post_twilight_invoice";
    public const string WatchRouteNoteRouteId = "post_watch_route_note";
    public const string ForgeThreadRouteId = "post_forge_thread";
    public const string WetlandObservationRouteId = "post_wetland_observation";
    public const string RestingRouteId = "post_resting_route";

    private int _day = 1;
    private string _activeRouteId = string.Empty;
    private readonly HashSet<string> _completedRouteIds =
        new(StringComparer.Ordinal);

    public static IReadOnlyList<PostDeliveryRouteDefinition> Routes { get; } =
        BuildRoutes();

    public static IReadOnlyDictionary<string, PostDeliveryRouteDefinition>
        ById { get; } = Routes.ToDictionary(
            route => route.Id,
            StringComparer.Ordinal
        );

    public event Action? Changed;

    public int Day => _day;
    public string ActiveRouteId => _activeRouteId;
    public IReadOnlySet<string> CompletedRouteIds => _completedRouteIds;

    public void Reset(int day)
    {
        _day = Math.Max(1, day);
        _activeRouteId = string.Empty;
        _completedRouteIds.Clear();
        Changed?.Invoke();
    }

    public void Restore(PostDeliverySave? save, int currentDay)
    {
        var normalized = NormalizeSave(save, currentDay);
        _day = normalized.Day;
        _activeRouteId = normalized.ActiveRouteId;
        _completedRouteIds.Clear();
        foreach (var routeId in normalized.CompletedRouteIds)
        {
            _completedRouteIds.Add(routeId);
        }
        Changed?.Invoke();
    }

    public void AdvanceToDay(int day)
    {
        var normalizedDay = Math.Max(1, day);
        if (_day == normalizedDay)
        {
            return;
        }

        Reset(normalizedDay);
    }

    public PostDeliverySave Capture() => new()
    {
        Day = _day,
        ActiveRouteId = _activeRouteId,
        CompletedRouteIds = _completedRouteIds
            .Order(StringComparer.Ordinal)
            .ToList()
    };

    public static IReadOnlyList<PostDeliveryRouteDefinition> OffersForDay(
        int day
    )
    {
        var normalizedDay = Math.Max(1, day);
        var start = ((normalizedDay - 1) * DailyOfferCount) % Routes.Count;
        return Enumerable.Range(0, DailyOfferCount)
            .Select(offset => Routes[(start + offset) % Routes.Count])
            .ToArray();
    }

    public PostDeliveryBoardSnapshot BoardForDay(int day)
    {
        var normalizedDay = Math.Max(1, day);
        var stateMatchesDay = _day == normalizedDay;
        return new PostDeliveryBoardSnapshot(
            normalizedDay,
            OffersForDay(normalizedDay),
            stateMatchesDay ? _activeRouteId : string.Empty,
            stateMatchesDay
                ? _completedRouteIds.ToHashSet(StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal),
            DailyCompletionLimit
        );
    }

    public PostDeliveryRouteDefinition? ActiveRouteForDay(int day)
    {
        if (_day != Math.Max(1, day) ||
            string.IsNullOrWhiteSpace(_activeRouteId))
        {
            return null;
        }

        return ById.GetValueOrDefault(_activeRouteId);
    }

    public ActionResult CheckAccept(string routeId, int day)
    {
        var board = BoardForDay(day);
        if (!board.Offers.Any(route => route.Id == routeId))
        {
            return ActionResult.Fail("post.delivery.unavailable");
        }

        if (board.CompletedRouteIds.Contains(routeId))
        {
            return ActionResult.Fail("post.delivery.already_completed");
        }

        if (board.CompletedCount >= board.DailyLimit)
        {
            return ActionResult.Fail("post.delivery.daily_limit_reached");
        }

        if (board.HasActiveRoute)
        {
            return ActionResult.Fail("post.delivery.active_route_exists");
        }

        return ActionResult.Success(messageKey: "post.delivery.ready");
    }

    public ActionResult Accept(string routeId, int day)
    {
        AdvanceToDay(day);
        var check = CheckAccept(routeId, day);
        if (!check.Succeeded)
        {
            return check;
        }

        _activeRouteId = routeId;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "post.delivery.accepted");
    }

    public ActionResult CheckDelivery(string npcId, int day)
    {
        var route = ActiveRouteForDay(day);
        if (route is null)
        {
            return ActionResult.Fail("post.delivery.none_active");
        }

        return route.TargetNpcId == npcId
            ? ActionResult.Success(messageKey: "post.delivery.ready_to_deliver")
            : ActionResult.Fail("post.delivery.wrong_recipient");
    }

    public ActionResult Complete(
        string npcId,
        int day,
        out PostDeliveryCompletion? completion
    )
    {
        completion = null;
        AdvanceToDay(day);
        var check = CheckDelivery(npcId, day);
        if (!check.Succeeded)
        {
            return check;
        }

        var route = ById[_activeRouteId];
        _completedRouteIds.Add(route.Id);
        _activeRouteId = string.Empty;
        completion = new PostDeliveryCompletion(
            route,
            route.RewardCoins,
            RelationshipRewardPoints
        );
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "post.delivery.completed");
    }

    public static PostDeliverySave NormalizeSave(
        PostDeliverySave? save,
        int currentDay
    )
    {
        var day = Math.Max(1, currentDay);
        if (save is null || save.Day != day)
        {
            return new PostDeliverySave { Day = day };
        }

        var offeredIds = OffersForDay(day)
            .Select(route => route.Id)
            .ToHashSet(StringComparer.Ordinal);
        var completedRouteIds = (save.CompletedRouteIds ?? [])
            .Where(offeredIds.Contains)
            .Distinct(StringComparer.Ordinal)
            .Take(DailyCompletionLimit)
            .Order(StringComparer.Ordinal)
            .ToList();
        var activeRouteId = save.ActiveRouteId;
        if (!offeredIds.Contains(activeRouteId) ||
            completedRouteIds.Contains(activeRouteId) ||
            completedRouteIds.Count >= DailyCompletionLimit)
        {
            activeRouteId = string.Empty;
        }

        return new PostDeliverySave
        {
            Day = day,
            ActiveRouteId = activeRouteId,
            CompletedRouteIds = completedRouteIds
        };
    }

    private static IReadOnlyList<PostDeliveryRouteDefinition> BuildRoutes()
    {
        var routes = new PostDeliveryRouteDefinition[]
        {
            new(
                ArchiveReturnRouteId,
                VillageCatalog.LioraId,
                56,
                "post.delivery.route.archive_return.name",
                "post.delivery.route.archive_return.response"
            ),
            new(
                MoonstoneSealRouteId,
                VillageCatalog.TaviId,
                64,
                "post.delivery.route.moonstone_seal.name",
                "post.delivery.route.moonstone_seal.response"
            ),
            new(
                TeaLeafReplyRouteId,
                VillageCatalog.VessaId,
                58,
                "post.delivery.route.tea_leaf_reply.name",
                "post.delivery.route.tea_leaf_reply.response"
            ),
            new(
                TwilightInvoiceRouteId,
                VillageCatalog.OrinId,
                70,
                "post.delivery.route.twilight_invoice.name",
                "post.delivery.route.twilight_invoice.response"
            ),
            new(
                WatchRouteNoteRouteId,
                VillageCatalog.KaelId,
                74,
                "post.delivery.route.watch_route_note.name",
                "post.delivery.route.watch_route_note.response"
            ),
            new(
                ForgeThreadRouteId,
                VillageCatalog.SelaId,
                68,
                "post.delivery.route.forge_thread.name",
                "post.delivery.route.forge_thread.response"
            ),
            new(
                WetlandObservationRouteId,
                VillageCatalog.ElowenId,
                62,
                "post.delivery.route.wetland_observation.name",
                "post.delivery.route.wetland_observation.response"
            ),
            new(
                RestingRouteId,
                VillageCatalog.RovenId,
                66,
                "post.delivery.route.resting_route.name",
                "post.delivery.route.resting_route.response"
            )
        };

        if (routes.Length < DailyOfferCount ||
            routes.Any(route =>
                string.IsNullOrWhiteSpace(route.Id) ||
                !VillageCatalog.Npcs.ContainsKey(route.TargetNpcId) ||
                route.RewardCoins <= 0 ||
                string.IsNullOrWhiteSpace(route.NameKey) ||
                string.IsNullOrWhiteSpace(route.ResponseKey)
            ) ||
            routes.Select(route => route.Id).Distinct(StringComparer.Ordinal)
                .Count() != routes.Length)
        {
            throw new InvalidOperationException(
                "Invalid starlight post delivery route catalog."
            );
        }

        return routes;
    }
}
