namespace Luminfield.Core;

public sealed record GroupCharacterEventPage(
    string SpeakerNpcId,
    string DialogueKey
);

public sealed record GroupCharacterEventDefinition(
    string Id,
    string TriggerNpcId,
    IReadOnlyList<string> ParticipantNpcIds,
    int RequiredRelationshipPoints,
    string RequiredLocationId,
    IReadOnlyDictionary<string, string> RequiredNpcDialogueKeys,
    IReadOnlyList<string> RequiredCharacterEventIds,
    int RequiredStartMinute,
    int RequiredEndMinute,
    GridArea RequiredParticipantArea,
    IReadOnlyList<GroupCharacterEventPage> Pages
);

public sealed record GroupCharacterEventDialogue(
    string EventId,
    IReadOnlyList<GroupCharacterEventPage> Pages
);

public static class GroupCharacterEventCatalog
{
    public const string NpcAFourRoutesOneLanternId =
        "npc_a_four_routes_one_lantern";
    public const string NpcBLastLampWaitsForReturnId =
        "npc_b_last_lamp_waits_for_return";
    public const string NpcCOneOpenCornerFourUsesId =
        "npc_c_one_open_corner_four_uses";
    public const string NpcDOneBenchFourKindsOfReadinessId =
        "npc_d_one_bench_four_kinds_of_readiness";

    public static readonly IReadOnlyList<GroupCharacterEventDefinition>
        Definitions =
        [
            new(
                NpcAFourRoutesOneLanternId,
                VillageCatalog.LioraId,
                [
                    VillageCatalog.LioraId,
                    VillageCatalog.TaviId,
                    VillageCatalog.VessaId,
                    VillageCatalog.OrinId
                ],
                90,
                PlayerLocationIds.World,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [VillageCatalog.LioraId] =
                        "village.npc.liora.npc_a_group",
                    [VillageCatalog.TaviId] =
                        "village.npc.tavi.npc_a_group",
                    [VillageCatalog.VessaId] =
                        "village.npc.vessa.npc_a_group",
                    [VillageCatalog.OrinId] =
                        "village.npc.orin.npc_a_group"
                },
                [
                    CharacterEventCatalog.NpcALioraFirstUncopiedChartId,
                    CharacterEventCatalog.NpcATaviJointWithRoomToMoveId,
                    CharacterEventCatalog.NpcAVessaCupBrewedForHerselfId,
                    CharacterEventCatalog.NpcAOrinCaseHeUnpackedId
                ],
                13 * 60,
                17 * 60,
                VillageCatalog.NpcAGroupMeetingArea,
                [
                    new(
                        VillageCatalog.LioraId,
                        "character_event.npc_a.group.four_routes_one_lantern.1"
                    ),
                    new(
                        VillageCatalog.TaviId,
                        "character_event.npc_a.group.four_routes_one_lantern.2"
                    ),
                    new(
                        VillageCatalog.VessaId,
                        "character_event.npc_a.group.four_routes_one_lantern.3"
                    ),
                    new(
                        VillageCatalog.OrinId,
                        "character_event.npc_a.group.four_routes_one_lantern.4"
                    ),
                    new(
                        VillageCatalog.LioraId,
                        "character_event.npc_a.group.four_routes_one_lantern.5"
                    )
                ]
            ),
            new(
                NpcBLastLampWaitsForReturnId,
                VillageCatalog.NemiId,
                [
                    VillageCatalog.NemiId,
                    VillageCatalog.KaelId,
                    VillageCatalog.SelaId,
                    VillageCatalog.HaldenId
                ],
                90,
                PlayerLocationIds.World,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [VillageCatalog.NemiId] =
                        "village.npc.nemi.npc_b_group",
                    [VillageCatalog.KaelId] =
                        "village.npc.kael.npc_b_group",
                    [VillageCatalog.SelaId] =
                        "village.npc.sela.npc_b_group",
                    [VillageCatalog.HaldenId] =
                        "village.npc.halden.npc_b_group"
                },
                [
                    CharacterEventCatalog.NpcBNemiHookForHerOwnMailbagId,
                    CharacterEventCatalog.NpcBKaelLastMarkerOnTheReturnBoardId,
                    CharacterEventCatalog.NpcBSelaHammerFittedToHerHandId,
                    CharacterEventCatalog.NpcBHaldenBellHeChoseNotToRingId
                ],
                VillageCatalog.NpcBGroupStartMinute,
                VillageCatalog.NpcBGroupEndMinute,
                VillageCatalog.NpcBGroupMeetingArea,
                [
                    new(
                        VillageCatalog.NemiId,
                        "group_character_event.npc_b_last_lamp_waits_for_return.page_1"
                    ),
                    new(
                        VillageCatalog.KaelId,
                        "group_character_event.npc_b_last_lamp_waits_for_return.page_2"
                    ),
                    new(
                        VillageCatalog.SelaId,
                        "group_character_event.npc_b_last_lamp_waits_for_return.page_3"
                    ),
                    new(
                        VillageCatalog.HaldenId,
                        "group_character_event.npc_b_last_lamp_waits_for_return.page_4"
                    ),
                    new(
                        VillageCatalog.NemiId,
                        "group_character_event.npc_b_last_lamp_waits_for_return.page_5"
                    )
                ]
            ),
            new(
                NpcCOneOpenCornerFourUsesId,
                VillageCatalog.DorrikId,
                [
                    VillageCatalog.ElowenId,
                    VillageCatalog.MaveaId,
                    VillageCatalog.SivrenId,
                    VillageCatalog.DorrikId
                ],
                90,
                PlayerLocationIds.World,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [VillageCatalog.ElowenId] =
                        "village.npc.elowen.npc_c_group",
                    [VillageCatalog.MaveaId] =
                        "village.npc.mavea.npc_c_group",
                    [VillageCatalog.SivrenId] =
                        "village.npc.sivren.npc_c_group",
                    [VillageCatalog.DorrikId] =
                        "village.npc.dorrik.npc_c_group"
                },
                [
                    CharacterEventCatalog.NpcCElowenMarkerAllowedToDriftId,
                    CharacterEventCatalog.NpcCMaveaLastJarOpenedOnAnOrdinaryDayId,
                    CharacterEventCatalog.NpcCSivrenFirstPersonFootnoteId,
                    CharacterEventCatalog.NpcCDorrikPlanReturnedToItsUsersId
                ],
                VillageCatalog.NpcCGroupStartMinute,
                VillageCatalog.NpcCGroupEndMinute,
                VillageCatalog.NpcCGroupMeetingArea,
                [
                    new(
                        VillageCatalog.DorrikId,
                        "group_character_event.npc_c_one_open_corner_four_uses.page_1"
                    ),
                    new(
                        VillageCatalog.ElowenId,
                        "group_character_event.npc_c_one_open_corner_four_uses.page_2"
                    ),
                    new(
                        VillageCatalog.MaveaId,
                        "group_character_event.npc_c_one_open_corner_four_uses.page_3"
                    ),
                    new(
                        VillageCatalog.SivrenId,
                        "group_character_event.npc_c_one_open_corner_four_uses.page_4"
                    ),
                    new(
                        VillageCatalog.DorrikId,
                        "group_character_event.npc_c_one_open_corner_four_uses.page_5"
                    )
                ]
            ),
            new(
                NpcDOneBenchFourKindsOfReadinessId,
                VillageCatalog.RovenId,
                [
                    VillageCatalog.YvaraId,
                    VillageCatalog.BrialId,
                    VillageCatalog.PavriId,
                    VillageCatalog.RovenId
                ],
                90,
                PlayerLocationIds.World,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [VillageCatalog.YvaraId] =
                        "village.npc.yvara.npc_d_group",
                    [VillageCatalog.BrialId] =
                        "village.npc.brial.npc_d_group",
                    [VillageCatalog.PavriId] =
                        "village.npc.pavri.npc_d_group",
                    [VillageCatalog.RovenId] =
                        "village.npc.roven.npc_d_group"
                },
                [
                    CharacterEventCatalog.NpcDYvaraASeedRecordInTwoHandsId,
                    CharacterEventCatalog.NpcDBrialThePruningMarkHeErasedId,
                    CharacterEventCatalog.NpcDPavriOneStitchBesideTheOldId,
                    CharacterEventCatalog.NpcDRovenARouteForAnOrdinaryDayId
                ],
                VillageCatalog.NpcDGroupStartMinute,
                VillageCatalog.NpcDGroupEndMinute,
                VillageCatalog.NpcDGroupMeetingArea,
                [
                    new(
                        VillageCatalog.RovenId,
                        "group_character_event.npc_d_one_bench_four_kinds_of_readiness.page_1"
                    ),
                    new(
                        VillageCatalog.YvaraId,
                        "group_character_event.npc_d_one_bench_four_kinds_of_readiness.page_2"
                    ),
                    new(
                        VillageCatalog.BrialId,
                        "group_character_event.npc_d_one_bench_four_kinds_of_readiness.page_3"
                    ),
                    new(
                        VillageCatalog.PavriId,
                        "group_character_event.npc_d_one_bench_four_kinds_of_readiness.page_4"
                    ),
                    new(
                        VillageCatalog.RovenId,
                        "group_character_event.npc_d_one_bench_four_kinds_of_readiness.page_5"
                    )
                ]
            )
        ];

    public static readonly IReadOnlyDictionary<
        string,
        GroupCharacterEventDefinition
    > ById = BuildById();

    private static IReadOnlyDictionary<
        string,
        GroupCharacterEventDefinition
    > BuildById()
    {
        var byId = new Dictionary<
            string,
            GroupCharacterEventDefinition
        >(StringComparer.Ordinal);
        foreach (var definition in Definitions)
        {
            var participantIds = definition.ParticipantNpcIds;
            var hasTriggerConflict = byId.Values.Any(existing =>
                existing.TriggerNpcId == definition.TriggerNpcId &&
                existing.RequiredLocationId ==
                    definition.RequiredLocationId &&
                existing.RequiredStartMinute <
                    definition.RequiredEndMinute &&
                existing.RequiredEndMinute >
                    definition.RequiredStartMinute &&
                existing.RequiredNpcDialogueKeys.TryGetValue(
                    existing.TriggerNpcId,
                    out var existingTriggerDialogueKey
                ) &&
                definition.RequiredNpcDialogueKeys.TryGetValue(
                    definition.TriggerNpcId,
                    out var triggerDialogueKey
                ) &&
                existingTriggerDialogueKey == triggerDialogueKey
            );
            if (string.IsNullOrWhiteSpace(definition.Id) ||
                !VillageCatalog.Npcs.ContainsKey(
                    definition.TriggerNpcId
                ) ||
                participantIds.Count < 2 ||
                participantIds.Distinct(StringComparer.Ordinal).Count() !=
                    participantIds.Count ||
                !participantIds.Contains(
                    definition.TriggerNpcId,
                    StringComparer.Ordinal
                ) ||
                participantIds.Any(participantId =>
                    !VillageCatalog.Npcs.ContainsKey(participantId)
                ) ||
                definition.RequiredRelationshipPoints is < 0 or > 100 ||
                string.IsNullOrWhiteSpace(definition.RequiredLocationId) ||
                definition.RequiredNpcDialogueKeys.Count !=
                    participantIds.Count ||
                definition.RequiredCharacterEventIds.Count !=
                    participantIds.Count ||
                definition.RequiredCharacterEventIds
                    .Distinct(StringComparer.Ordinal).Count() !=
                    definition.RequiredCharacterEventIds.Count ||
                definition.RequiredCharacterEventIds.Any(eventId =>
                    !CharacterEventCatalog.ById.ContainsKey(eventId)
                ) ||
                definition.RequiredStartMinute < GameClock.StartMinute ||
                definition.RequiredEndMinute > GameClock.EndMinute ||
                definition.RequiredStartMinute >=
                    definition.RequiredEndMinute ||
                definition.Pages.Count < participantIds.Count ||
                definition.Pages.Any(page =>
                    !participantIds.Contains(
                        page.SpeakerNpcId,
                        StringComparer.Ordinal
                    ) ||
                    string.IsNullOrWhiteSpace(page.DialogueKey)
                ) ||
                definition.Pages.Select(page => page.DialogueKey)
                    .Distinct(StringComparer.Ordinal).Count() !=
                    definition.Pages.Count ||
                participantIds.Any(participantId =>
                    !definition.Pages.Any(page =>
                        page.SpeakerNpcId == participantId
                    )
                ) ||
                hasTriggerConflict ||
                !byId.TryAdd(definition.Id, definition))
            {
                throw new InvalidOperationException(
                    $"Invalid group character event catalog entry: {definition.Id}."
                );
            }

            foreach (var participantId in participantIds)
            {
                var requiredEvents = definition.RequiredCharacterEventIds
                    .Select(eventId => CharacterEventCatalog.ById[eventId])
                    .Where(characterEvent =>
                        characterEvent.NpcId == participantId
                    )
                    .ToList();
                var terminalEvent = CharacterEventCatalog.Definitions
                    .Where(characterEvent =>
                        characterEvent.NpcId == participantId
                    )
                    .OrderBy(characterEvent =>
                        characterEvent.RequiredRelationshipPoints
                    )
                    .Last();
                if (requiredEvents.Count != 1 ||
                    requiredEvents[0].Id != terminalEvent.Id ||
                    terminalEvent.RequiredRelationshipPoints !=
                        definition.RequiredRelationshipPoints)
                {
                    throw new InvalidOperationException(
                        $"Group character event {definition.Id} has invalid participant prerequisites."
                    );
                }
            }

            foreach (var participantId in participantIds)
            {
                if (!definition.RequiredNpcDialogueKeys.TryGetValue(
                        participantId,
                        out var dialogueKey
                    ) ||
                    string.IsNullOrWhiteSpace(dialogueKey) ||
                    !VillageCatalog.Npcs[participantId].Schedule.Any(entry =>
                        entry.LocationId ==
                            definition.RequiredLocationId &&
                        entry.DialogueKey == dialogueKey &&
                        entry.StartMinute <=
                            definition.RequiredStartMinute &&
                        entry.EndMinute >=
                            definition.RequiredEndMinute &&
                        entry.Priority ==
                            VillageCatalog.GroupEventSchedulePriority &&
                        definition.RequiredParticipantArea.Contains(
                            entry.Position
                        )
                    ))
                {
                    throw new InvalidOperationException(
                        $"Group character event {definition.Id} has an invalid participant schedule."
                    );
                }
            }
        }

        return byId;
    }
}

public sealed class GroupCharacterEventSystem
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

    public void Restore(
        GroupCharacterEventSave? save,
        int currentDay,
        CharacterEventSystem characterEvents
    )
    {
        var normalized = NormalizeSave(
            save,
            currentDay,
            characterEvents.Capture()
        );
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

    public GroupCharacterEventDefinition? EligibleEvent(
        GridPosition target,
        int day,
        int minuteOfDay,
        string locationId,
        string selectedItemId,
        VillageSystem village,
        CharacterEventSystem characterEvents,
        GridPosition? playerPosition = null
    )
    {
        if (_activeEventId is not null ||
            selectedItemId != DataCatalog.HandId)
        {
            return null;
        }

        var triggerNpc = village.NpcAt(
            target,
            day,
            minuteOfDay,
            locationId,
            playerPosition
        );
        if (triggerNpc is null || triggerNpc.LocationId != locationId)
        {
            return null;
        }

        var currentNpcs = village.CurrentNpcs(
            day,
            minuteOfDay,
            locationId,
            playerPosition
        ).ToDictionary(
            npc => npc.Definition.Id,
            StringComparer.Ordinal
        );
        foreach (var definition in GroupCharacterEventCatalog.Definitions)
        {
            if (_completedDays.ContainsKey(definition.Id) ||
                definition.TriggerNpcId != triggerNpc.Definition.Id ||
                definition.RequiredLocationId != locationId ||
                minuteOfDay < definition.RequiredStartMinute ||
                minuteOfDay >= definition.RequiredEndMinute ||
                definition.RequiredCharacterEventIds.Any(eventId =>
                    characterEvents.CompletedDay(eventId) is not int
                        completedDay ||
                    completedDay >= day
                ))
            {
                continue;
            }

            var participantsReady = definition.ParticipantNpcIds.All(
                participantId =>
                    village.MetNpcIds.Contains(participantId) &&
                    village.Relationship(participantId).Points >=
                        definition.RequiredRelationshipPoints &&
                    currentNpcs.TryGetValue(
                        participantId,
                        out var participant
                    ) &&
                    participant.LocationId == locationId &&
                    definition.RequiredParticipantArea.Contains(
                        participant.Position
                    ) &&
                    definition.RequiredNpcDialogueKeys[participantId] ==
                        participant.DialogueKey
            );
            if (participantsReady)
            {
                return definition;
            }
        }

        return null;
    }

    internal GroupCharacterEventDialogue BeginEvent(
        GroupCharacterEventDefinition definition
    )
    {
        if (!GroupCharacterEventCatalog.ById.TryGetValue(
                definition.Id,
                out var catalogDefinition
            ) ||
            _completedDays.ContainsKey(definition.Id) ||
            _activeEventId is not null)
        {
            throw new ArgumentException(
                "Group character event must be known and incomplete.",
                nameof(definition)
            );
        }

        _activeEventId = catalogDefinition.Id;
        return new GroupCharacterEventDialogue(
            catalogDefinition.Id,
            catalogDefinition.Pages
        );
    }

    public ActionResult CompleteActiveEvent(string eventId, int day)
    {
        if (_activeEventId != eventId ||
            !GroupCharacterEventCatalog.ById.ContainsKey(eventId) ||
            _completedDays.ContainsKey(eventId))
        {
            return ActionResult.Fail("character_event.not_active");
        }

        _completedDays[eventId] = Math.Max(1, day);
        _activeEventId = null;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "character_event.completed");
    }

    public GroupCharacterEventSave Capture() => new()
    {
        Entries = GroupCharacterEventCatalog.Definitions
            .Where(definition =>
                _completedDays.ContainsKey(definition.Id)
            )
            .Select(definition => new GroupCharacterEventEntrySave
            {
                EventId = definition.Id,
                CompletedDay = _completedDays[definition.Id]
            })
            .ToList()
    };

    public static GroupCharacterEventSave NormalizeSave(
        GroupCharacterEventSave? save,
        int currentDay,
        CharacterEventSave characterEvents
    )
    {
        var validCurrentDay = Math.Max(1, currentDay);
        var characterEventDays = characterEvents.Entries.ToDictionary(
            entry => entry.EventId,
            entry => entry.CompletedDay,
            StringComparer.Ordinal
        );
        var earliestDays = (save?.Entries ?? [])
            .Where(entry =>
                GroupCharacterEventCatalog.ById.ContainsKey(
                    entry.EventId
                ) &&
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

        var entries = new List<GroupCharacterEventEntrySave>();
        foreach (var definition in GroupCharacterEventCatalog.Definitions)
        {
            if (!earliestDays.TryGetValue(
                    definition.Id,
                    out var completedDay
                ) ||
                definition.RequiredCharacterEventIds.Any(eventId =>
                    !characterEventDays.TryGetValue(
                        eventId,
                        out var prerequisiteDay
                    ) ||
                    prerequisiteDay >= completedDay
                ))
            {
                continue;
            }

            entries.Add(new GroupCharacterEventEntrySave
            {
                EventId = definition.Id,
                CompletedDay = completedDay
            });
        }

        return new GroupCharacterEventSave { Entries = entries };
    }
}
