namespace Luminfield.Core;

public static class NpcNavigationMap
{
    private static readonly IReadOnlyDictionary<string, GridPosition>
        IndoorArrivalCells = new Dictionary<string, GridPosition>(
            StringComparer.Ordinal
        )
        {
            [PlayerLocationIds.MoonlitArchive] = new(19, 17),
            [PlayerLocationIds.MoonstoneWorkshop] = new(19, 18),
            [PlayerLocationIds.StarweaverTeaHouse] = new(19, 18),
            [PlayerLocationIds.TwilightEmporium] = new(19, 18),
            [PlayerLocationIds.StarlightPost] = new(19, 18)
        };

    private static readonly IReadOnlyDictionary<string, GridPosition>
        WorldArrivalCells = new Dictionary<string, GridPosition>(
            StringComparer.Ordinal
        )
        {
            [PlayerLocationIds.MoonlitArchive] = new(86, 42),
            [PlayerLocationIds.MoonstoneWorkshop] = new(85, 55),
            [PlayerLocationIds.StarweaverTeaHouse] = new(107, 43),
            [PlayerLocationIds.TwilightEmporium] = new(110, 62),
            [PlayerLocationIds.StarlightPost] = new(77, 42)
        };

    public static bool IsWalkableGeometry(
        string locationId,
        GridPosition cell
    )
    {
        if (locationId == PlayerLocationIds.World)
        {
            return VillageCatalog.IsVillageCell(cell) &&
                !WorldDefinition.IsBlocked(cell);
        }

        if (cell.X is < 2 or > 37 || cell.Y is < 3 or > 20)
        {
            return false;
        }

        if (locationId == PlayerLocationIds.MoonlitArchive)
        {
            return !IsArchiveFurniture(cell);
        }

        if (locationId == PlayerLocationIds.MoonstoneWorkshop)
        {
            return !IsWorkshopFurniture(cell);
        }

        if (locationId == PlayerLocationIds.StarweaverTeaHouse)
        {
            return !IsTeaHouseFurniture(cell);
        }

        if (locationId == PlayerLocationIds.TwilightEmporium)
        {
            return !IsEmporiumFurniture(cell);
        }

        if (locationId == PlayerLocationIds.StarlightPost)
        {
            return !IsPostFurniture(cell);
        }

        return false;
    }

    public static bool IsNpcPassable(
        string locationId,
        GridPosition cell
    ) => IsWalkableGeometry(locationId, cell) &&
        !IsCriticalEntranceCell(locationId, cell);

    public static bool IsCriticalEntranceCell(
        string locationId,
        GridPosition cell
    )
    {
        if (locationId == PlayerLocationIds.World)
        {
            return cell == VillageCatalog.MoonlitArchiveDoorCell ||
                cell == VillageCatalog.MoonstoneWorkshopDoorCell ||
                cell == VillageCatalog.StarweaverTeaHouseDoorCell ||
                cell == VillageCatalog.TwilightEmporiumDoorCell ||
                cell == VillageCatalog.StarlightPostDoorCell ||
                cell == VillageCatalog.VillageGateCell;
        }

        if (locationId == PlayerLocationIds.MoonlitArchive)
        {
            return cell == VillageCatalog.MoonlitArchiveExitCell;
        }

        if (locationId == PlayerLocationIds.MoonstoneWorkshop)
        {
            return cell == VillageCatalog.MoonstoneWorkshopExitCell;
        }

        if (locationId == PlayerLocationIds.StarweaverTeaHouse)
        {
            return cell == VillageCatalog.StarweaverTeaHouseExitCell;
        }

        if (locationId == PlayerLocationIds.TwilightEmporium)
        {
            return cell == VillageCatalog.TwilightEmporiumExitCell;
        }

        if (locationId == PlayerLocationIds.StarlightPost)
        {
            return cell == VillageCatalog.StarlightPostExitCell;
        }

        return false;
    }

    public static GridPosition? SafeArrivalCell(
        string sourceLocationId,
        string destinationLocationId
    )
    {
        if (destinationLocationId == PlayerLocationIds.World)
        {
            if (WorldArrivalCells.TryGetValue(
                    sourceLocationId,
                    out var worldCell
                ))
            {
                return worldCell;
            }

            return null;
        }

        if (IndoorArrivalCells.TryGetValue(
                destinationLocationId,
                out var indoorCell
            ))
        {
            return indoorCell;
        }

        return null;
    }

    private static bool IsArchiveFurniture(GridPosition cell) =>
        (cell.X is >= 16 and <= 23 && cell.Y is >= 8 and <= 11) ||
        (cell.X is >= 15 and <= 24 && cell.Y is >= 3 and <= 6) ||
        cell.X is >= 2 and <= 5 ||
        cell.X is >= 34 and <= 37 ||
        (cell.X is >= 6 and <= 11 && cell.Y is >= 3 and <= 7) ||
        (cell.X is >= 28 and <= 33 && cell.Y is >= 3 and <= 7) ||
        (cell.X is >= 4 and <= 8 && cell.Y is >= 15 and <= 18) ||
        (cell.X is >= 31 and <= 35 && cell.Y is >= 15 and <= 18);

    private static bool IsWorkshopFurniture(GridPosition cell) =>
        (cell.X is >= 15 and <= 24 && cell.Y is >= 4 and <= 9) ||
        (cell.X is >= 3 and <= 10 && cell.Y is >= 3 and <= 9) ||
        (cell.X is >= 27 and <= 36 && cell.Y is >= 3 and <= 9) ||
        (cell.X is >= 2 and <= 7 && cell.Y is >= 10 and <= 18) ||
        (cell.X is >= 32 and <= 37 && cell.Y is >= 10 and <= 18) ||
        (cell.X is >= 2 and <= 12 && cell.Y is >= 17 and <= 20) ||
        (cell.X is >= 27 and <= 37 && cell.Y is >= 17 and <= 20);

    private static bool IsTeaHouseFurniture(GridPosition cell) =>
        (cell.X is >= 12 and <= 27 && cell.Y is >= 3 and <= 9) ||
        (cell.X is >= 3 and <= 10 && cell.Y is >= 10 and <= 15) ||
        (cell.X is >= 29 and <= 36 && cell.Y is >= 10 and <= 15) ||
        (cell.X is >= 2 and <= 8 && cell.Y is >= 3 and <= 9) ||
        (cell.X is >= 31 and <= 37 && cell.Y is >= 3 and <= 9) ||
        (cell.X is >= 2 and <= 7 && cell.Y is >= 17 and <= 20) ||
        (cell.X is >= 33 and <= 37 && cell.Y is >= 17 and <= 20);

    private static bool IsEmporiumFurniture(GridPosition cell) =>
        (cell.X is >= 14 and <= 25 && cell.Y is >= 4 and <= 8) ||
        (cell.X is >= 2 and <= 10 && cell.Y is >= 3 and <= 10) ||
        (cell.X is >= 29 and <= 37 && cell.Y is >= 3 and <= 10) ||
        (cell.X is >= 2 and <= 8 && cell.Y is >= 14 and <= 20) ||
        (cell.X is >= 31 and <= 37 && cell.Y is >= 14 and <= 20);

    private static bool IsPostFurniture(GridPosition cell) =>
        (cell.X is >= 11 and <= 28 && cell.Y is >= 7 and <= 10) ||
        (cell.X is >= 2 and <= 7 && cell.Y is >= 3 and <= 14) ||
        (cell.X is >= 32 and <= 37 && cell.Y is >= 3 and <= 14) ||
        (cell.X is >= 2 and <= 6 && cell.Y is >= 15 and <= 20) ||
        (cell.X is >= 33 and <= 37 && cell.Y is >= 15 and <= 20);
}

public static class NpcPathfinder
{
    private static readonly GridPosition[] Directions =
    [
        new(0, -1),
        new(-1, 0),
        new(1, 0),
        new(0, 1)
    ];

    public static IReadOnlyList<GridPosition> FindPath(
        string locationId,
        GridPosition start,
        GridPosition destination,
        IReadOnlySet<GridPosition>? additionalBlocked = null
    )
    {
        if (!NpcNavigationMap.IsNpcPassable(locationId, start) ||
            !NpcNavigationMap.IsNpcPassable(locationId, destination))
        {
            return [];
        }

        if (start == destination)
        {
            return [start];
        }

        var frontier = new Queue<GridPosition>();
        var parents = new Dictionary<GridPosition, GridPosition>();
        frontier.Enqueue(start);
        parents[start] = start;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var direction in Directions)
            {
                var next = new GridPosition(
                    current.X + direction.X,
                    current.Y + direction.Y
                );
                if (parents.ContainsKey(next) ||
                    !NpcNavigationMap.IsNpcPassable(locationId, next) ||
                    additionalBlocked?.Contains(next) == true)
                {
                    continue;
                }

                parents[next] = current;
                if (next == destination)
                {
                    return Reconstruct(parents, start, destination);
                }

                frontier.Enqueue(next);
            }
        }

        return [];
    }

    private static IReadOnlyList<GridPosition> Reconstruct(
        IReadOnlyDictionary<GridPosition, GridPosition> parents,
        GridPosition start,
        GridPosition destination
    )
    {
        var reversed = new List<GridPosition> { destination };
        var current = destination;
        while (current != start)
        {
            current = parents[current];
            reversed.Add(current);
        }

        reversed.Reverse();
        return reversed;
    }
}

public sealed class NpcScheduleSystem
{
    private static readonly NpcScheduleSystem CatalogProjection = new();

    private readonly object _cacheLock = new();
    private readonly Dictionary<int, IReadOnlyList<VillageNpcState>>
        _cachedTimeline = [];
    private int _cachedDay;
    private string _cachedWeatherId = string.Empty;
    private int _runtimeDay;
    private string _runtimeWeatherId = string.Empty;
    private int _runtimeMinute = -1;
    private IReadOnlyList<VillageNpcState>? _runtimeStates;

    public IReadOnlyList<VillageNpcState> ResolveAll(
        int day,
        int minuteOfDay,
        string weatherId,
        string? playerLocationId = null,
        GridPosition? playerPosition = null
    )
    {
        var normalizedDay = Math.Max(1, day);
        var normalizedMinute = NormalizeMinute(minuteOfDay);
        var normalizedWeatherId = NormalizeWeatherId(
            normalizedDay,
            weatherId
        );
        lock (_cacheLock)
        {
            if (_cachedDay != normalizedDay ||
                _cachedWeatherId != normalizedWeatherId)
            {
                _cachedTimeline.Clear();
                _cachedDay = normalizedDay;
                _cachedWeatherId = normalizedWeatherId;
                ResetRuntimeUnsafe();
            }

            EnsurePureTimeline(
                normalizedDay,
                normalizedMinute,
                normalizedWeatherId
            );
            if (playerPosition is null ||
                string.IsNullOrWhiteSpace(playerLocationId))
            {
                return _cachedTimeline[normalizedMinute];
            }

            IReadOnlyList<VillageNpcState> states;
            if (_runtimeStates is not null &&
                _runtimeDay == normalizedDay &&
                _runtimeWeatherId == normalizedWeatherId &&
                _runtimeMinute == normalizedMinute)
            {
                return _runtimeStates;
            }

            if (normalizedMinute == GameClock.StartMinute)
            {
                states = AvoidInitialPlayerOverlap(
                    normalizedDay,
                    normalizedWeatherId,
                    playerLocationId,
                    playerPosition.Value
                );
            }
            else
            {
                var previous = PreviousRuntimeOrPure(
                    normalizedDay,
                    normalizedMinute,
                    normalizedWeatherId
                );
                states = Advance(
                    normalizedDay,
                    normalizedMinute,
                    normalizedWeatherId,
                    previous,
                    (playerLocationId, playerPosition.Value)
                );
            }

            _runtimeDay = normalizedDay;
            _runtimeWeatherId = normalizedWeatherId;
            _runtimeMinute = normalizedMinute;
            _runtimeStates = states;
            return states;
        }
    }

    public static VillageNpcState? ResolveCatalogNpc(
        string npcId,
        int day,
        int minuteOfDay
    ) => ResolveCatalogNpc(
        npcId,
        day,
        minuteOfDay,
        WeatherSystem.WeatherForDay(day)
    );

    public static VillageNpcState? ResolveCatalogNpc(
        string npcId,
        int day,
        int minuteOfDay,
        string weatherId
    ) => CatalogProjection.ResolveAll(day, minuteOfDay, weatherId)
        .FirstOrDefault(state => state.Definition.Id == npcId);

    public void ResetRuntime()
    {
        lock (_cacheLock)
        {
            ResetRuntimeUnsafe();
        }
    }

    public static NpcScheduleEntry? SelectEntry(
        VillageNpcDefinition definition,
        int day,
        int minuteOfDay,
        string weatherId
    )
    {
        var normalizedDay = Math.Max(1, day);
        var normalizedWeatherId = NormalizeWeatherId(
            normalizedDay,
            weatherId
        );
        return definition.Schedule
            .Where(entry => entry.Matches(
                normalizedDay,
                minuteOfDay,
                normalizedWeatherId
            ))
            .OrderByDescending(entry => entry.Priority)
            .FirstOrDefault();
    }

    public static VillageNpcState ProjectRouteOrFallback(
        VillageNpcDefinition definition,
        NpcScheduleEntry entry,
        VillageNpcState previous
    )
    {
        if (previous.LocationId != entry.LocationId)
        {
            var arrival = NpcNavigationMap.SafeArrivalCell(
                previous.LocationId,
                entry.LocationId
            );
            if (arrival is not null &&
                NpcNavigationMap.IsNpcPassable(
                    entry.LocationId,
                    arrival.Value
                ))
            {
                return StateAt(
                    definition,
                    entry,
                    arrival.Value,
                    FacingToward(entry.LocationId, arrival.Value, entry)
                );
            }

            return AnchorState(definition, entry);
        }

        var path = NpcPathfinder.FindPath(
            entry.LocationId,
            previous.Position,
            entry.Position
        );
        if (path.Count == 0)
        {
            return AnchorState(definition, entry);
        }

        if (path.Count == 1)
        {
            return AnchorState(definition, entry);
        }

        return StateAt(
            definition,
            entry,
            path[1],
            FacingForStep(previous.Position, path[1])
        );
    }

    private static IReadOnlyList<VillageNpcState> BuildInitialStates(
        int day,
        string weatherId
    )
    {
        var states = new List<VillageNpcState>();
        foreach (var definition in OrderedDefinitions())
        {
            var entry = SelectEntry(
                definition,
                day,
                GameClock.StartMinute,
                weatherId
            );
            if (entry is not null)
            {
                states.Add(AnchorState(definition, entry));
            }
        }

        return states.AsReadOnly();
    }

    private static IReadOnlyList<VillageNpcState> Advance(
        int day,
        int minuteOfDay,
        string weatherId,
        IReadOnlyList<VillageNpcState> previousStates,
        (string LocationId, GridPosition Position)? playerOccupied = null
    )
    {
        var previousById = previousStates.ToDictionary(
            state => state.Definition.Id,
            StringComparer.Ordinal
        );
        var previousOccupied = previousStates
            .Select(state => (state.LocationId, state.Position))
            .ToHashSet();
        var reserved = new HashSet<(string LocationId, GridPosition Position)>();
        if (playerOccupied is not null)
        {
            reserved.Add(playerOccupied.Value);
        }
        var nextStates = new List<VillageNpcState>();

        foreach (var definition in OrderedDefinitions())
        {
            var entry = SelectEntry(
                definition,
                day,
                minuteOfDay,
                weatherId
            );
            if (entry is null)
            {
                continue;
            }

            if (!previousById.TryGetValue(
                    definition.Id,
                    out var previous
                ))
            {
                var initial = AnchorState(definition, entry);
                nextStates.Add(initial);
                reserved.Add((initial.LocationId, initial.Position));
                continue;
            }

            var blocked = previousOccupied
                .Where(cell => cell != (
                    previous.LocationId,
                    previous.Position
                ))
                .Where(cell => cell.LocationId == entry.LocationId)
                .Select(cell => cell.Position)
                .Concat(
                    reserved
                        .Where(cell => cell.LocationId == entry.LocationId)
                        .Select(cell => cell.Position)
                )
                .ToHashSet();
            var desired = ProjectNext(
                definition,
                entry,
                previous,
                blocked,
                playerOccupied
            );
            var desiredCell = (desired.LocationId, desired.Position);
            if (reserved.Contains(desiredCell))
            {
                desired = WaitOrFallback(definition, entry, previous, reserved);
                desiredCell = (desired.LocationId, desired.Position);
            }

            nextStates.Add(desired);
            reserved.Add(desiredCell);
        }

        return nextStates.AsReadOnly();
    }

    private static VillageNpcState ProjectNext(
        VillageNpcDefinition definition,
        NpcScheduleEntry entry,
        VillageNpcState previous,
        IReadOnlySet<GridPosition> blocked,
        (string LocationId, GridPosition Position)? playerOccupied
    )
    {
        if (previous.LocationId != entry.LocationId)
        {
            var transfer = ProjectRouteOrFallback(
                definition,
                entry,
                previous
            );
            if (playerOccupied is not null &&
                transfer.LocationId == playerOccupied.Value.LocationId &&
                transfer.Position == playerOccupied.Value.Position)
            {
                return WaitingState(definition, entry, previous);
            }

            if (!blocked.Contains(transfer.Position))
            {
                return transfer;
            }

            return WaitingState(definition, entry, previous);
        }

        var staticPath = NpcPathfinder.FindPath(
            entry.LocationId,
            previous.Position,
            entry.Position
        );
        if (staticPath.Count > 1 &&
            playerOccupied is not null &&
            entry.LocationId == playerOccupied.Value.LocationId &&
            staticPath[1] == playerOccupied.Value.Position)
        {
            return WaitingState(definition, entry, previous);
        }

        var path = NpcPathfinder.FindPath(
            entry.LocationId,
            previous.Position,
            entry.Position,
            blocked
        );
        if (path.Count > 1)
        {
            return StateAt(
                definition,
                entry,
                path[1],
                FacingForStep(previous.Position, path[1])
            );
        }

        if (path.Count == 1)
        {
            return AnchorState(definition, entry);
        }

        if (staticPath.Count > 0)
        {
            return StateAt(
                definition,
                entry,
                previous.Position,
                previous.Facing
            );
        }

        return AnchorState(definition, entry);
    }

    private static VillageNpcState WaitOrFallback(
        VillageNpcDefinition definition,
        NpcScheduleEntry entry,
        VillageNpcState previous,
        IReadOnlySet<(string LocationId, GridPosition Position)> reserved
    )
    {
        var previousCell = (previous.LocationId, previous.Position);
        if (!reserved.Contains(previousCell))
        {
            return WaitingState(definition, entry, previous);
        }

        return AnchorState(definition, entry);
    }

    private static VillageNpcState WaitingState(
        VillageNpcDefinition definition,
        NpcScheduleEntry entry,
        VillageNpcState previous
    ) => new(
        definition,
        previous.LocationId,
        previous.Position,
        previous.Facing,
        entry.DialogueKey
    );

    private static VillageNpcState AnchorState(
        VillageNpcDefinition definition,
        NpcScheduleEntry entry
    ) => StateAt(
        definition,
        entry,
        entry.Position,
        entry.Facing
    );

    private static VillageNpcState StateAt(
        VillageNpcDefinition definition,
        NpcScheduleEntry entry,
        GridPosition position,
        NpcFacing facing
    ) => new(
        definition,
        entry.LocationId,
        position,
        facing,
        entry.DialogueKey
    );

    private static NpcFacing FacingToward(
        string locationId,
        GridPosition start,
        NpcScheduleEntry entry
    )
    {
        var path = NpcPathfinder.FindPath(
            locationId,
            start,
            entry.Position
        );
        if (path.Count > 1)
        {
            return FacingForStep(path[0], path[1]);
        }

        return entry.Facing;
    }

    private static NpcFacing FacingForStep(
        GridPosition start,
        GridPosition destination
    )
    {
        if (destination.X < start.X)
        {
            return NpcFacing.Left;
        }

        if (destination.X > start.X)
        {
            return NpcFacing.Right;
        }

        if (destination.Y < start.Y)
        {
            return NpcFacing.Up;
        }

        return NpcFacing.Down;
    }

    private static IEnumerable<VillageNpcDefinition> OrderedDefinitions() =>
        VillageCatalog.Npcs.Values.OrderBy(definition => definition.AtlasRow);

    private void EnsurePureTimeline(
        int day,
        int minuteOfDay,
        string weatherId
    )
    {
        if (!_cachedTimeline.ContainsKey(GameClock.StartMinute))
        {
            _cachedTimeline[GameClock.StartMinute] = BuildInitialStates(
                day,
                weatherId
            );
        }

        for (var minute = GameClock.StartMinute + GameClock.MinutesPerTick;
             minute <= minuteOfDay;
             minute += GameClock.MinutesPerTick)
        {
            if (_cachedTimeline.ContainsKey(minute))
            {
                continue;
            }

            var previous = _cachedTimeline[
                minute - GameClock.MinutesPerTick
            ];
            _cachedTimeline[minute] = Advance(
                day,
                minute,
                weatherId,
                previous
            );
        }
    }

    private IReadOnlyList<VillageNpcState> PreviousRuntimeOrPure(
        int day,
        int minuteOfDay,
        string weatherId
    )
    {
        var previousMinute = minuteOfDay - GameClock.MinutesPerTick;
        if (_runtimeStates is not null &&
            _runtimeDay == day &&
            _runtimeWeatherId == weatherId &&
            _runtimeMinute == previousMinute)
        {
            return _runtimeStates;
        }

        return _cachedTimeline[previousMinute];
    }

    private static IReadOnlyList<VillageNpcState> AvoidInitialPlayerOverlap(
        int day,
        string weatherId,
        string playerLocationId,
        GridPosition playerPosition
    )
    {
        var initial = BuildInitialStates(day, weatherId);
        if (!initial.Any(state =>
                state.LocationId == playerLocationId &&
                state.Position == playerPosition
            ))
        {
            return initial;
        }

        var occupied = initial
            .Select(state => (
                LocationId: state.LocationId,
                Position: state.Position
            ))
            .Append((
                LocationId: playerLocationId,
                Position: playerPosition
            ))
            .ToHashSet();
        var safe = new List<VillageNpcState>();
        foreach (var state in initial)
        {
            if (state.LocationId != playerLocationId ||
                state.Position != playerPosition)
            {
                safe.Add(state);
                continue;
            }

            var replacement = NearestSafeCell(
                state.LocationId,
                state.Position,
                occupied
                    .Where(cell => cell.LocationId == state.LocationId)
                    .Select(cell => cell.Position)
                    .ToHashSet()
            );
            if (replacement is null)
            {
                safe.Add(state);
                continue;
            }

            occupied.Remove((state.LocationId, state.Position));
            occupied.Add((state.LocationId, replacement.Value));
            safe.Add(state with { Position = replacement.Value });
        }

        return safe.AsReadOnly();
    }

    private static GridPosition? NearestSafeCell(
        string locationId,
        GridPosition start,
        IReadOnlySet<GridPosition> occupied
    )
    {
        var frontier = new Queue<GridPosition>();
        var visited = new HashSet<GridPosition> { start };
        frontier.Enqueue(start);
        var directions = new[]
        {
            new GridPosition(0, -1),
            new GridPosition(-1, 0),
            new GridPosition(1, 0),
            new GridPosition(0, 1)
        };
        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var direction in directions)
            {
                var next = new GridPosition(
                    current.X + direction.X,
                    current.Y + direction.Y
                );
                if (!visited.Add(next) ||
                    !NpcNavigationMap.IsNpcPassable(locationId, next))
                {
                    continue;
                }

                if (!occupied.Contains(next))
                {
                    return next;
                }

                frontier.Enqueue(next);
            }
        }

        return null;
    }

    private static int NormalizeMinute(int minuteOfDay)
    {
        var clamped = Math.Clamp(
            minuteOfDay,
            GameClock.StartMinute,
            GameClock.EndMinute
        );
        var elapsed = clamped - GameClock.StartMinute;
        return GameClock.StartMinute +
            elapsed / GameClock.MinutesPerTick * GameClock.MinutesPerTick;
    }

    private static string NormalizeWeatherId(int day, string weatherId)
    {
        if (DataCatalog.WeatherDefinitions.ContainsKey(weatherId))
        {
            return weatherId;
        }

        return WeatherSystem.WeatherForDay(day);
    }

    private void ResetRuntimeUnsafe()
    {
        _runtimeDay = 0;
        _runtimeWeatherId = string.Empty;
        _runtimeMinute = -1;
        _runtimeStates = null;
    }
}
