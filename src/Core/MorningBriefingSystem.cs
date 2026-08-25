namespace Luminfield.Core;

public enum MorningBriefingCardKind
{
    Weather,
    Mail,
    Festival,
    CharacterEvent,
    DailyCommission,
    WeeklyCommission,
    RegionSuggestion
}

public enum MorningBriefingPriority
{
    Primary,
    Secondary,
    Optional
}

public enum MorningBriefingArgumentKind
{
    LocalizationKey,
    Number
}

public readonly record struct MorningBriefingArgument(
    MorningBriefingArgumentKind Kind,
    string LocalizationKey,
    int Number
)
{
    public static MorningBriefingArgument Key(string localizationKey) =>
        new(MorningBriefingArgumentKind.LocalizationKey, localizationKey, 0);

    public static MorningBriefingArgument Count(int number) =>
        new(MorningBriefingArgumentKind.Number, string.Empty, number);
}

public sealed record MorningBriefingCard(
    string Id,
    MorningBriefingCardKind Kind,
    MorningBriefingPriority Priority,
    int SortOrder,
    string TitleKey,
    string BodyKey,
    IReadOnlyList<MorningBriefingArgument> Arguments,
    string ActionKey = "",
    string ReferenceId = ""
);

public sealed record MorningBriefing(IReadOnlyList<MorningBriefingCard> Cards)
{
    public bool IsEmpty => Cards.Count == 0;
}

public static class MorningBriefingSystem
{
    private const int TileSize = 16;

    public const string WeatherCardId = "morning.weather";
    public const string MailCardId = "morning.mail";
    public const string FestivalCardId = "morning.festival";
    public const string CharacterEventCardId = "morning.character_event";
    public const string DailyCommissionCardId = "morning.daily_commission";
    public const string WeeklyCommissionCardId = "morning.weekly_commission";
    public const string RegionSuggestionCardId = "morning.region";

    public static MorningBriefing Create(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var cards = new List<MorningBriefingCard>
        {
            WeatherCard(session),
            MailCard(session),
            FestivalCard(session),
            CharacterEventCard(session),
            DailyCommissionCard(session),
            WeeklyCommissionCard(session),
            RegionSuggestionCard(session)
        };

        return new MorningBriefing(
            cards
                .OrderBy(card => card.SortOrder)
                .ThenBy(card => card.Id, StringComparer.Ordinal)
                .ToList()
        );
    }

    private static MorningBriefingCard WeatherCard(GameSession session)
    {
        var bodyKey = session.Weather.CurrentId switch
        {
            DataCatalog.RainWeatherId => "morning.weather.rain",
            DataCatalog.StardustWindWeatherId =>
                "morning.weather.stardust_wind",
            DataCatalog.LongnightSnowWeatherId =>
                "morning.weather.longnight_snow",
            _ => "morning.weather.clear"
        };

        var priority = session.Weather.CurrentId == DataCatalog.ClearWeatherId
            ? MorningBriefingPriority.Secondary
            : MorningBriefingPriority.Primary;

        return new MorningBriefingCard(
            WeatherCardId,
            MorningBriefingCardKind.Weather,
            priority,
            10,
            "morning.weather.title",
            bodyKey,
            [
                MorningBriefingArgument.Key(session.Weather.Current.NameKey),
                MorningBriefingArgument.Key(session.Weather.Forecast.NameKey)
            ]
        );
    }

    private static MorningBriefingCard MailCard(GameSession session)
    {
        var unread = session.Mail.UnreadCount;
        if (unread <= 0)
        {
            return new MorningBriefingCard(
                MailCardId,
                MorningBriefingCardKind.Mail,
                MorningBriefingPriority.Optional,
                20,
                "morning.mail.title",
                "morning.mail.none",
                []
            );
        }

        return new MorningBriefingCard(
            MailCardId,
            MorningBriefingCardKind.Mail,
            MorningBriefingPriority.Primary,
            20,
            "morning.mail.title",
            "morning.mail.unread",
            [MorningBriefingArgument.Count(unread)],
            "morning.action.check_mail"
        );
    }

    private static MorningBriefingCard FestivalCard(GameSession session)
    {
        var day = session.Clock.Day;
        var minute = session.Clock.MinuteOfDay;
        var today = FestivalCatalog.FestivalOnDay(day);
        if (today is not null)
        {
            var bodyKey = FestivalCatalog.IsOpen(today.Id, day, minute)
                ? "morning.festival.open_today"
                : "morning.festival.today_later";
            var priority = FestivalCatalog.IsOpen(today.Id, day, minute)
                ? MorningBriefingPriority.Primary
                : MorningBriefingPriority.Secondary;
            return new MorningBriefingCard(
                FestivalCardId,
                MorningBriefingCardKind.Festival,
                priority,
                30,
                "morning.festival.title",
                bodyKey,
                [
                    MorningBriefingArgument.Key(today.NameKey),
                    MorningBriefingArgument.Count(today.OpenMinute / 60),
                    MorningBriefingArgument.Count(today.CloseMinute / 60)
                ],
                "morning.action.visit_festival",
                today.Id
            );
        }

        var tomorrow = FestivalCatalog.FestivalOnDay(day + 1);
        if (tomorrow is not null)
        {
            return new MorningBriefingCard(
                FestivalCardId,
                MorningBriefingCardKind.Festival,
                MorningBriefingPriority.Secondary,
                30,
                "morning.festival.title",
                "morning.festival.tomorrow",
                [
                    MorningBriefingArgument.Key(tomorrow.NameKey),
                    MorningBriefingArgument.Count(tomorrow.OpenMinute / 60)
                ],
                "morning.action.prepare_festival",
                tomorrow.Id
            );
        }

        return new MorningBriefingCard(
            FestivalCardId,
            MorningBriefingCardKind.Festival,
            MorningBriefingPriority.Optional,
            30,
            "morning.festival.title",
            "morning.festival.none",
            []
        );
    }

    private static MorningBriefingCard CharacterEventCard(GameSession session)
    {
        var ready = EligibleCharacterEvents(session);
        if (ready.Count == 0)
        {
            return new MorningBriefingCard(
                CharacterEventCardId,
                MorningBriefingCardKind.CharacterEvent,
                MorningBriefingPriority.Optional,
                40,
                "morning.character_event.title",
                "morning.character_event.none",
                []
            );
        }

        var first = ready[0];
        var npc = VillageCatalog.Npcs[first.NpcId];
        var bodyKey = ready.Count == 1
            ? "morning.character_event.ready_one"
            : "morning.character_event.ready_many";
        var args = ready.Count == 1
            ? new List<MorningBriefingArgument>
            {
                MorningBriefingArgument.Key(npc.NameKey)
            }
            : [
                MorningBriefingArgument.Count(ready.Count),
                MorningBriefingArgument.Key(npc.NameKey)
            ];

        return new MorningBriefingCard(
            CharacterEventCardId,
            MorningBriefingCardKind.CharacterEvent,
            MorningBriefingPriority.Primary,
            40,
            "morning.character_event.title",
            bodyKey,
            args,
            "morning.action.find_friend",
            first.Id
        );
    }

    private static MorningBriefingCard DailyCommissionCard(GameSession session)
    {
        var commission = session.Commission;
        var definition = commission.Current;
        var progress = commission.DisplayProgress(session.Inventory);
        var bodyKey = "morning.daily_commission.not_accepted";
        var priority = MorningBriefingPriority.Optional;
        var actionKey = "morning.action.open_commission_board";

        if (commission.Claimed)
        {
            bodyKey = "morning.daily_commission.claimed";
            actionKey = string.Empty;
        }
        else if (commission.IsReady(session.Inventory))
        {
            bodyKey = "morning.daily_commission.ready";
            priority = MorningBriefingPriority.Primary;
        }
        else if (commission.Accepted)
        {
            bodyKey = "morning.daily_commission.in_progress";
            priority = MorningBriefingPriority.Secondary;
        }

        return new MorningBriefingCard(
            DailyCommissionCardId,
            MorningBriefingCardKind.DailyCommission,
            priority,
            50,
            "morning.daily_commission.title",
            bodyKey,
            [
                MorningBriefingArgument.Key(definition.TitleKey),
                MorningBriefingArgument.Count(progress),
                MorningBriefingArgument.Count(definition.RequiredCount)
            ],
            actionKey,
            definition.Id
        );
    }

    private static MorningBriefingCard WeeklyCommissionCard(
        GameSession session
    )
    {
        var commission = session.WeeklyCommission;
        var stage = commission.CurrentStage;
        var progress = commission.DisplayProgress(session.Inventory);
        var bodyKey = "morning.weekly_commission.not_accepted";
        var priority = MorningBriefingPriority.Optional;
        var actionKey = "morning.action.open_commission_board";

        if (commission.Claimed)
        {
            bodyKey = "morning.weekly_commission.claimed";
            actionKey = string.Empty;
        }
        else if (commission.IsReady(session.Inventory))
        {
            bodyKey = commission.IsFinalStage
                ? "morning.weekly_commission.ready_final"
                : "morning.weekly_commission.ready_stage";
            priority = MorningBriefingPriority.Primary;
        }
        else if (commission.Accepted)
        {
            bodyKey = "morning.weekly_commission.in_progress";
            priority = MorningBriefingPriority.Secondary;
        }

        return new MorningBriefingCard(
            WeeklyCommissionCardId,
            MorningBriefingCardKind.WeeklyCommission,
            priority,
            60,
            "morning.weekly_commission.title",
            bodyKey,
            [
                MorningBriefingArgument.Key(stage.DescriptionKey),
                MorningBriefingArgument.Count(progress),
                MorningBriefingArgument.Count(stage.RequiredCount)
            ],
            actionKey,
            stage.Id
        );
    }

    private static MorningBriefingCard RegionSuggestionCard(
        GameSession session
    )
    {
        var suggestion = RegionSuggestion(session);
        return new MorningBriefingCard(
            RegionSuggestionCardId,
            MorningBriefingCardKind.RegionSuggestion,
            MorningBriefingPriority.Secondary,
            70,
            "morning.region.title",
            suggestion.BodyKey,
            [MorningBriefingArgument.Key(suggestion.RegionNameKey)],
            "morning.action.explore_region",
            suggestion.ReferenceId
        );
    }

    private static IReadOnlyList<CharacterEventDefinition>
        EligibleCharacterEvents(GameSession session) =>
        CharacterEventCatalog.Definitions
            .Where(definition => IsCharacterEventEligibleToday(session, definition))
            .OrderBy(definition =>
                VillageCatalog.Npcs[definition.NpcId].ScheduleOrder)
            .ThenBy(definition =>
                definition.RequiredRelationshipPoints)
            .ThenBy(definition => definition.Id, StringComparer.Ordinal)
            .ToList();

    private static bool IsCharacterEventEligibleToday(
        GameSession session,
        CharacterEventDefinition definition
    )
    {
        if (session.CharacterEvents.IsCompleted(definition.Id) ||
            !session.Village.MetNpcIds.Contains(definition.NpcId))
        {
            return false;
        }

        if (session.Village.Relationship(definition.NpcId).Points <
            definition.RequiredRelationshipPoints)
        {
            return false;
        }

        if (definition.RequiredPreviousEventId is not null)
        {
            var completedDay = session.CharacterEvents.CompletedDay(
                definition.RequiredPreviousEventId
            );

            if (completedDay is null || completedDay >= session.Clock.Day)
            {
                return false;
            }
        }

        return VillageCatalog.Npcs[definition.NpcId].Schedule.Any(entry =>
            entry.LocationId == definition.RequiredLocationId &&
            entry.Matches(
                session.Clock.Day,
                Math.Max(entry.StartMinute, GameClock.StartMinute),
                session.Weather.CurrentId
            ) &&
            (definition.RequiredNpcDialogueKey is null ||
                definition.RequiredNpcDialogueKey == entry.DialogueKey));
    }

    private static MorningBriefingRegionSuggestion RegionSuggestion(
        GameSession session
    )
    {
        var undiscovered = WorldDefinition.Landmarks
            .Where(landmark =>
                !session.Exploration.IsDiscovered(
                    WorldDefinition.GetChunk(landmark.Position)
                ))
            .OrderBy(landmark =>
                DistanceFromPlayer(session, landmark.Position))
            .ThenBy(landmark => landmark.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        if (undiscovered is not null)
        {
            var biome = WorldDefinition.GetBiome(undiscovered.Position);
            return new MorningBriefingRegionSuggestion(
                "morning.region.undiscovered_landmark",
                WorldDefinition.RegionNameKey(biome),
                undiscovered.Id
            );
        }

        var currentCell = new GridPosition(
            (int)MathF.Floor(session.PlayerX / TileSize),
            (int)MathF.Floor(session.PlayerY / TileSize)
        );
        var currentBiome = WorldDefinition.GetBiome(currentCell);
        return new MorningBriefingRegionSuggestion(
            "morning.region.all_known",
            WorldDefinition.RegionNameKey(currentBiome),
            currentBiome.ToString()
        );
    }

    private static int DistanceFromPlayer(
        GameSession session,
        GridPosition target
    )
    {
        var player = new GridPosition(
            (int)MathF.Floor(session.PlayerX / TileSize),
            (int)MathF.Floor(session.PlayerY / TileSize)
        );
        return Math.Abs(player.X - target.X) + Math.Abs(player.Y - target.Y);
    }

    private sealed record MorningBriefingRegionSuggestion(
        string BodyKey,
        string RegionNameKey,
        string ReferenceId
    );
}
