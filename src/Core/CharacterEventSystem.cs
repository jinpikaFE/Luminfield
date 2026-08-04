namespace Luminfield.Core;

public sealed record CharacterEventDefinition(
    string Id,
    string NpcId,
    int RequiredRelationshipPoints,
    string RequiredLocationId,
    IReadOnlyList<string> DialogueKeys,
    string? RequiredPreviousEventId = null
);

public sealed record CharacterEventDialogue(
    string EventId,
    IReadOnlyList<string> DialogueKeys
);

public static class CharacterEventCatalog
{
    public const string LioraFadedReturnRouteId =
        "liora_faded_return_route";
    public const string LioraRememberedWayHomeId =
        "liora_remembered_way_home";
    public const string TaviCrackedMoonRuneId =
        "tavi_cracked_moon_rune";
    public const string TaviMendedLightId =
        "tavi_mended_light";

    public static readonly IReadOnlyList<CharacterEventDefinition>
        Definitions =
        [
            new(
                LioraFadedReturnRouteId,
                VillageCatalog.LioraId,
                25,
                PlayerLocationIds.MoonlitArchive,
                [
                    "character_event.liora.faded_return_route.1",
                    "character_event.liora.faded_return_route.2",
                    "character_event.liora.faded_return_route.3"
                ]
            ),
            new(
                LioraRememberedWayHomeId,
                VillageCatalog.LioraId,
                60,
                PlayerLocationIds.MoonlitArchive,
                [
                    "character_event.liora.remembered_way_home.1",
                    "character_event.liora.remembered_way_home.2",
                    "character_event.liora.remembered_way_home.3"
                ],
                LioraFadedReturnRouteId
            ),
            new(
                TaviCrackedMoonRuneId,
                VillageCatalog.TaviId,
                25,
                PlayerLocationIds.MoonstoneWorkshop,
                [
                    "character_event.tavi.cracked_moon_rune.1",
                    "character_event.tavi.cracked_moon_rune.2",
                    "character_event.tavi.cracked_moon_rune.3"
                ]
            ),
            new(
                TaviMendedLightId,
                VillageCatalog.TaviId,
                60,
                PlayerLocationIds.MoonstoneWorkshop,
                [
                    "character_event.tavi.mended_light.1",
                    "character_event.tavi.mended_light.2",
                    "character_event.tavi.mended_light.3"
                ],
                TaviCrackedMoonRuneId
            )
        ];

    public static readonly IReadOnlyDictionary<string, CharacterEventDefinition>
        ById = Definitions.ToDictionary(
            definition => definition.Id,
            StringComparer.Ordinal
        );
}

public sealed class CharacterEventSystem
{
    private readonly Dictionary<string, int> _completedDays =
        new(StringComparer.Ordinal);
    private string? _activeEventId;

    public event Action? Changed;

    public string? ActiveEventId => _activeEventId;

    public void Reset()
    {
        _completedDays.Clear();
        _activeEventId = null;
        Changed?.Invoke();
    }

    public void Restore(CharacterEventSave? save, int currentDay)
    {
        var normalized = NormalizeSave(save, currentDay);
        _completedDays.Clear();
        foreach (var entry in normalized.Entries)
        {
            _completedDays[entry.EventId] = entry.CompletedDay;
        }

        _activeEventId = null;
        Changed?.Invoke();
    }

    public bool IsCompleted(string eventId) =>
        _completedDays.ContainsKey(eventId);

    public int? CompletedDay(string eventId) =>
        _completedDays.TryGetValue(eventId, out var day)
            ? day
            : null;

    public CharacterEventDefinition? EligibleEvent(
        GridPosition target,
        int day,
        int minuteOfDay,
        string locationId,
        string selectedItemId,
        VillageSystem village,
        GridPosition? playerPosition = null
    )
    {
        if (selectedItemId != DataCatalog.HandId)
        {
            return null;
        }

        var npc = village.NpcAt(
            target,
            day,
            minuteOfDay,
            locationId,
            playerPosition
        );
        if (npc is null ||
            npc.LocationId != locationId ||
            !village.MetNpcIds.Contains(npc.Definition.Id))
        {
            return null;
        }

        var relationshipPoints = village
            .Relationship(npc.Definition.Id)
            .Points;
        foreach (var definition in CharacterEventCatalog.Definitions)
        {
            if (_completedDays.ContainsKey(definition.Id) ||
                definition.NpcId != npc.Definition.Id ||
                definition.RequiredLocationId != locationId ||
                relationshipPoints <
                    definition.RequiredRelationshipPoints)
            {
                continue;
            }

            if (definition.RequiredPreviousEventId is null)
            {
                return definition;
            }

            if (_completedDays.TryGetValue(
                    definition.RequiredPreviousEventId,
                    out var previousCompletedDay
                ) &&
                previousCompletedDay < day)
            {
                return definition;
            }
        }

        return null;
    }

    internal CharacterEventDialogue BeginEvent(
        CharacterEventDefinition definition
    )
    {
        if (!CharacterEventCatalog.ById.TryGetValue(
                definition.Id,
                out var catalogDefinition
            ) ||
            _completedDays.ContainsKey(definition.Id))
        {
            throw new ArgumentException(
                "Character event must be known and incomplete.",
                nameof(definition)
            );
        }

        _activeEventId = catalogDefinition.Id;
        return new CharacterEventDialogue(
            catalogDefinition.Id,
            catalogDefinition.DialogueKeys
        );
    }

    public ActionResult CompleteActiveEvent(string eventId, int day)
    {
        if (_activeEventId != eventId ||
            !CharacterEventCatalog.ById.TryGetValue(
                eventId,
                out var definition
            ) ||
            _completedDays.ContainsKey(eventId))
        {
            return ActionResult.Fail("character_event.not_active");
        }

        if (definition.RequiredPreviousEventId is not null &&
            (!_completedDays.TryGetValue(
                definition.RequiredPreviousEventId,
                out var previousCompletedDay
            ) ||
            previousCompletedDay >= day))
        {
            return ActionResult.Fail(
                "character_event.previous_day_required"
            );
        }

        _completedDays[eventId] = Math.Max(1, day);
        _activeEventId = null;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "character_event.completed");
    }

    public CharacterEventSave Capture() => new()
    {
        Entries = CharacterEventCatalog.Definitions
            .Where(definition =>
                _completedDays.ContainsKey(definition.Id)
            )
            .Select(definition => new CharacterEventEntrySave
            {
                EventId = definition.Id,
                CompletedDay = _completedDays[definition.Id]
            })
            .ToList()
    };

    public static CharacterEventSave NormalizeSave(
        CharacterEventSave? save,
        int currentDay
    )
    {
        var validCurrentDay = Math.Max(1, currentDay);
        var earliestDays = (save?.Entries ?? [])
            .Where(entry =>
                CharacterEventCatalog.ById.ContainsKey(entry.EventId) &&
                entry.CompletedDay > 0
            )
            .GroupBy(entry => entry.EventId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => Math.Min(
                    group.Min(entry => entry.CompletedDay),
                    validCurrentDay
                ),
                StringComparer.Ordinal
            );

        var entries = new List<CharacterEventEntrySave>();
        foreach (var definition in CharacterEventCatalog.Definitions)
        {
            if (!earliestDays.TryGetValue(
                    definition.Id,
                    out var completedDay
                ))
            {
                continue;
            }

            if (definition.RequiredPreviousEventId is not null)
            {
                var previous = entries.FirstOrDefault(entry =>
                    entry.EventId == definition.RequiredPreviousEventId
                );
                if (previous is null ||
                    previous.CompletedDay >= completedDay)
                {
                    continue;
                }
            }

            entries.Add(new CharacterEventEntrySave
            {
                EventId = definition.Id,
                CompletedDay = completedDay
            });
        }

        return new CharacterEventSave { Entries = entries };
    }
}
