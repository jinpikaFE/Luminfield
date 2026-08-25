using Luminfield.Core;

namespace Luminfield.Game;

public static class MorningBriefingNavigationPresenter
{
    public static WorldBiome? DestinationFor(
        MorningBriefingDecisionSummaryItem item,
        GameSession session
    ) => TargetFor(item, session)?.Region;

    public static WorldNavigationDestination? TargetFor(
        MorningBriefingDecisionSummaryItem item,
        GameSession session
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(session);

        return item.Kind switch
        {
            MorningBriefingCardKind.Mail => HomeTarget(
                "morning:mailbox",
                FarmLayout.StarlightMailboxCell,
                WorldNavigationDestinationKind.Mailbox,
                "morning.mail.title"
            ),
            MorningBriefingCardKind.DailyCommission => HomeTarget(
                "morning:daily_commission",
                FarmLayout.CommissionBoardCell,
                WorldNavigationDestinationKind.CommissionBoard,
                "morning.daily_commission.title"
            ),
            MorningBriefingCardKind.WeeklyCommission => HomeTarget(
                "morning:weekly_commission",
                FarmLayout.CommissionBoardCell,
                WorldNavigationDestinationKind.CommissionBoard,
                "morning.weekly_commission.title"
            ),
            MorningBriefingCardKind.Festival =>
                TargetForFestival(item.ReferenceId),
            MorningBriefingCardKind.CharacterEvent =>
                TargetForCharacterEvent(item.ReferenceId, session),
            MorningBriefingCardKind.RegionSuggestion =>
                TargetForRegionSuggestion(item.ReferenceId),
            _ => null
        };
    }

    private static WorldNavigationDestination HomeTarget(
        string id,
        GridPosition position,
        WorldNavigationDestinationKind kind,
        string nameKey
    ) => WorldNavigationDestination.AdjacentTarget(
        id,
        WorldBiome.Home,
        position,
        kind,
        nameKey
    );

    private static WorldNavigationDestination? TargetForFestival(
        string referenceId
    )
    {
        if (string.IsNullOrWhiteSpace(referenceId) ||
            !FestivalSpatialCatalog.TryByFestivalId(
                referenceId,
                out var festival
            ) ||
            !FestivalCatalog.Festivals.TryGetValue(
                referenceId,
                out var definition
            ))
        {
            return null;
        }

        return WorldNavigationDestination.AdjacentTarget(
            $"festival:{definition.Id}",
            DestinationForWorldDoorPair(
                festival.WorldEntryCell,
                festival.WorldReturnCell
            ),
            festival.WorldEntryCell,
            WorldNavigationDestinationKind.FestivalEntrance,
            definition.NameKey,
            festival.LocationId
        );
    }

    private static WorldBiome DestinationForWorldDoorPair(
        GridPosition entryCell,
        GridPosition returnCell
    )
    {
        var entryRegion = WorldDefinition.GetBiome(entryCell);
        var returnRegion = WorldDefinition.GetBiome(returnCell);
        if (entryRegion == returnRegion)
        {
            return entryRegion;
        }

        return entryRegion;
    }

    private static WorldNavigationDestination? TargetForCharacterEvent(
        string referenceId,
        GameSession session
    )
    {
        if (string.IsNullOrWhiteSpace(referenceId) ||
            !CharacterEventCatalog.ById.TryGetValue(
                referenceId,
                out var definition
            ) ||
            !VillageCatalog.Npcs.TryGetValue(
                definition.NpcId,
                out var npc
            ))
        {
            return null;
        }

        var entry = MatchingScheduleEntry(definition, npc, session);
        if (entry is null)
        {
            return null;
        }

        var targetCell = TargetCellForLocation(
            definition.RequiredLocationId,
            entry.Position
        );
        GridPosition? locationTargetCell = null;
        if (definition.RequiredLocationId != PlayerLocationIds.World)
        {
            locationTargetCell = entry.Position;
        }
        return WorldNavigationDestination.AdjacentTarget(
            $"character:{definition.Id}",
            RouteGuidanceOriginPresenter.Resolve(
                definition.RequiredLocationId,
                entry.Position
            ),
            targetCell,
            WorldNavigationDestinationKind.Character,
            npc.NameKey,
            definition.RequiredLocationId,
            locationTargetCell
        );
    }

    private static NpcScheduleEntry? MatchingScheduleEntry(
        CharacterEventDefinition definition,
        VillageNpcDefinition npc,
        GameSession session
    ) => npc.Schedule
        .Where(entry => entry.LocationId == definition.RequiredLocationId)
        .Where(entry => definition.RequiredNpcDialogueKey is null ||
            entry.DialogueKey == definition.RequiredNpcDialogueKey)
        .Where(entry => entry.Matches(
            session.Clock.Day,
            Math.Max(entry.StartMinute, GameClock.StartMinute),
            session.Weather.CurrentId
        ))
        .OrderBy(entry => Math.Max(entry.StartMinute, GameClock.StartMinute))
        .ThenByDescending(entry => entry.Priority)
        .FirstOrDefault();

    private static WorldNavigationDestination? TargetForRegionSuggestion(
        string referenceId
    )
    {
        if (string.IsNullOrWhiteSpace(referenceId))
        {
            return null;
        }

        var landmark = WorldDefinition.Landmarks.FirstOrDefault(entry =>
            entry.Id == referenceId);
        if (landmark is not null)
        {
            return WorldNavigationDestination.AdjacentTarget(
                $"landmark:{landmark.Id}",
                WorldDefinition.GetBiome(landmark.Position),
                landmark.Position,
                WorldNavigationDestinationKind.Landmark,
                landmark.NameKey
            );
        }

        if (Enum.TryParse<WorldBiome>(
                referenceId,
                ignoreCase: false,
                out var region
            ) &&
            Enum.IsDefined(region))
        {
            return WorldNavigationDestination.RegionOnly(region);
        }

        return null;
    }

    private static GridPosition TargetCellForLocation(
        string locationId,
        GridPosition position
    ) => locationId switch
    {
        PlayerLocationIds.MoonlitArchive =>
            VillageCatalog.MoonlitArchiveDoorCell,
        PlayerLocationIds.MoonstoneWorkshop =>
            VillageCatalog.MoonstoneWorkshopDoorCell,
        PlayerLocationIds.StarweaverTeaHouse =>
            VillageCatalog.StarweaverTeaHouseDoorCell,
        PlayerLocationIds.TwilightEmporium =>
            VillageCatalog.TwilightEmporiumDoorCell,
        PlayerLocationIds.StarlightPost =>
            VillageCatalog.StarlightPostDoorCell,
        PlayerLocationIds.StarfallWatch =>
            VillageCatalog.StarfallWatchDoorCell,
        _ => position
    };
}
