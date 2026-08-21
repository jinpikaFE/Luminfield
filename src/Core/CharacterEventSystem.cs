namespace Luminfield.Core;

public sealed record CharacterEventDefinition(
    string Id,
    string NpcId,
    int RequiredRelationshipPoints,
    string RequiredLocationId,
    IReadOnlyList<string> DialogueKeys,
    string? RequiredPreviousEventId = null,
    string? RequiredNpcDialogueKey = null
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
    public const string NemiUndeliverableLetterId =
        "nemi_undeliverable_letter";
    public const string NemiStarChartRouteId =
        "nemi_star_chart_route";
    public const string KaelBrokenBlueRuneId =
        "kael_broken_blue_rune";
    public const string KaelSafeReturnRouteId =
        "kael_safe_return_route";
    public const string SelaTemperedStarlightId =
        "sela_tempered_starlight";
    public const string SelaSharedForgeRhythmId =
        "sela_shared_forge_rhythm";
    public const string OrinUnpricedWaybillId =
        "orin_unpriced_waybill";
    public const string OrinSharedLanternRouteId =
        "orin_shared_lantern_route";
    public const string ElowenTideMarksAtTheWellId =
        "elowen_tide_marks_at_the_well";
    public const string ElowenWaterlineReadTogetherId =
        "elowen_waterline_read_together";
    public const string VessaBitterLeafWarmCupId =
        "vessa_bitter_leaf_warm_cup";
    public const string VessaPathThatListensBackId =
        "vessa_path_that_listens_back";
    public const string HaldenWeatherInTheFodderId =
        "halden_weather_in_the_fodder";
    public const string HaldenThreeBreathsOneRhythmId =
        "halden_three_breaths_one_rhythm";
    public const string MaveaFourBowlsOneTableId =
        "mavea_four_bowls_one_table";
    public const string MaveaWarmthThatKeepsId =
        "mavea_warmth_that_keeps";
    public const string SivrenUnfiledLanternsId =
        "sivren_unfiled_lanterns";
    public const string SivrenYearInThreeLightsId =
        "sivren_year_in_three_lights";
    public const string DorrikChalkBeyondWallsId =
        "dorrik_chalk_beyond_walls";
    public const string DorrikRoomsThatBreatheId =
        "dorrik_rooms_that_breathe";
    public const string YvaraSeedsBeyondTheCalendarId =
        "yvara_seeds_beyond_the_calendar";
    public const string YvaraASeasonCarriedGentlyId =
        "yvara_a_season_carried_gently";
    public const string BrialBlossomsBetweenHarvestsId =
        "brial_blossoms_between_harvests";
    public const string BrialAPathLeftForTheBeesId =
        "brial_a_path_left_for_the_bees";
    public const string PavriTheVisibleMendId =
        "pavri_the_visible_mend";
    public const string PavriClothThatKeepsWarmthId =
        "pavri_cloth_that_keeps_warmth";
    public const string RovenTheRouteWithRoomToRestId =
        "roven_the_route_with_room_to_rest";
    public const string RovenLightsThatWaitForReturnId =
        "roven_lights_that_wait_for_return";

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
            ),
            new(
                NemiUndeliverableLetterId,
                VillageCatalog.NemiId,
                25,
                PlayerLocationIds.World,
                [
                    "character_event.nemi.undeliverable_letter.1",
                    "character_event.nemi.undeliverable_letter.2",
                    "character_event.nemi.undeliverable_letter.3"
                ]
            ),
            new(
                NemiStarChartRouteId,
                VillageCatalog.NemiId,
                60,
                PlayerLocationIds.World,
                [
                    "character_event.nemi.star_chart_route.1",
                    "character_event.nemi.star_chart_route.2",
                    "character_event.nemi.star_chart_route.3"
                ],
                NemiUndeliverableLetterId
            ),
            new(
                KaelBrokenBlueRuneId,
                VillageCatalog.KaelId,
                25,
                PlayerLocationIds.World,
                [
                    "character_event.kael.broken_blue_rune.1",
                    "character_event.kael.broken_blue_rune.2",
                    "character_event.kael.broken_blue_rune.3"
                ]
            ),
            new(
                KaelSafeReturnRouteId,
                VillageCatalog.KaelId,
                60,
                PlayerLocationIds.World,
                [
                    "character_event.kael.safe_return_route.1",
                    "character_event.kael.safe_return_route.2",
                    "character_event.kael.safe_return_route.3"
                ],
                KaelBrokenBlueRuneId
            ),
            new(
                SelaTemperedStarlightId,
                VillageCatalog.SelaId,
                25,
                PlayerLocationIds.World,
                [
                    "character_event.sela.tempered_starlight.1",
                    "character_event.sela.tempered_starlight.2",
                    "character_event.sela.tempered_starlight.3"
                ]
            ),
            new(
                SelaSharedForgeRhythmId,
                VillageCatalog.SelaId,
                60,
                PlayerLocationIds.World,
                [
                    "character_event.sela.shared_forge_rhythm.1",
                    "character_event.sela.shared_forge_rhythm.2",
                    "character_event.sela.shared_forge_rhythm.3"
                ],
                SelaTemperedStarlightId
            ),
            new(
                OrinUnpricedWaybillId,
                VillageCatalog.OrinId,
                25,
                PlayerLocationIds.World,
                [
                    "character_event.orin.unpriced_waybill.1",
                    "character_event.orin.unpriced_waybill.2",
                    "character_event.orin.unpriced_waybill.3"
                ],
                RequiredNpcDialogueKey: "village.npc.orin.plaza"
            ),
            new(
                OrinSharedLanternRouteId,
                VillageCatalog.OrinId,
                60,
                PlayerLocationIds.World,
                [
                    "character_event.orin.shared_lantern_route.1",
                    "character_event.orin.shared_lantern_route.2",
                    "character_event.orin.shared_lantern_route.3"
                ],
                RequiredPreviousEventId: OrinUnpricedWaybillId,
                RequiredNpcDialogueKey: "village.npc.orin.plaza"
            ),
            new(
                HaldenWeatherInTheFodderId,
                VillageCatalog.HaldenId,
                25,
                PlayerLocationIds.World,
                [
                    "character_event.halden.weather_in_the_fodder.1",
                    "character_event.halden.weather_in_the_fodder.2",
                    "character_event.halden.weather_in_the_fodder.3"
                ],
                RequiredNpcDialogueKey: "village.npc.halden.plaza"
            ),
            new(
                HaldenThreeBreathsOneRhythmId,
                VillageCatalog.HaldenId,
                60,
                PlayerLocationIds.World,
                [
                    "character_event.halden.three_breaths_one_rhythm.1",
                    "character_event.halden.three_breaths_one_rhythm.2",
                    "character_event.halden.three_breaths_one_rhythm.3"
                ],
                RequiredPreviousEventId: HaldenWeatherInTheFodderId,
                RequiredNpcDialogueKey: "village.npc.halden.plaza"
            ),
            new(
                MaveaFourBowlsOneTableId,
                VillageCatalog.MaveaId,
                25,
                PlayerLocationIds.StarweaverTeaHouse,
                [
                    "character_event.mavea.four_bowls_one_table.1",
                    "character_event.mavea.four_bowls_one_table.2",
                    "character_event.mavea.four_bowls_one_table.3"
                ],
                RequiredNpcDialogueKey: "village.npc.mavea.tea_house"
            ),
            new(
                MaveaWarmthThatKeepsId,
                VillageCatalog.MaveaId,
                60,
                PlayerLocationIds.StarweaverTeaHouse,
                [
                    "character_event.mavea.warmth_that_keeps.1",
                    "character_event.mavea.warmth_that_keeps.2",
                    "character_event.mavea.warmth_that_keeps.3"
                ],
                RequiredPreviousEventId: MaveaFourBowlsOneTableId,
                RequiredNpcDialogueKey: "village.npc.mavea.tea_house"
            ),
            new(
                SivrenUnfiledLanternsId,
                VillageCatalog.SivrenId,
                25,
                PlayerLocationIds.MoonlitArchive,
                [
                    "character_event.sivren.unfiled_lanterns.1",
                    "character_event.sivren.unfiled_lanterns.2",
                    "character_event.sivren.unfiled_lanterns.3"
                ],
                RequiredNpcDialogueKey: "village.npc.sivren.archive"
            ),
            new(
                SivrenYearInThreeLightsId,
                VillageCatalog.SivrenId,
                60,
                PlayerLocationIds.MoonlitArchive,
                [
                    "character_event.sivren.year_in_three_lights.1",
                    "character_event.sivren.year_in_three_lights.2",
                    "character_event.sivren.year_in_three_lights.3"
                ],
                RequiredPreviousEventId: SivrenUnfiledLanternsId,
                RequiredNpcDialogueKey: "village.npc.sivren.archive"
            ),
            new(
                DorrikChalkBeyondWallsId,
                VillageCatalog.DorrikId,
                25,
                PlayerLocationIds.MoonstoneWorkshop,
                [
                    "character_event.dorrik.chalk_beyond_walls.1",
                    "character_event.dorrik.chalk_beyond_walls.2",
                    "character_event.dorrik.chalk_beyond_walls.3"
                ],
                RequiredNpcDialogueKey: "village.npc.dorrik.workshop"
            ),
            new(
                DorrikRoomsThatBreatheId,
                VillageCatalog.DorrikId,
                60,
                PlayerLocationIds.MoonstoneWorkshop,
                [
                    "character_event.dorrik.rooms_that_breathe.1",
                    "character_event.dorrik.rooms_that_breathe.2",
                    "character_event.dorrik.rooms_that_breathe.3"
                ],
                RequiredPreviousEventId: DorrikChalkBeyondWallsId,
                RequiredNpcDialogueKey: "village.npc.dorrik.workshop"
            ),
            new(
                ElowenTideMarksAtTheWellId,
                VillageCatalog.ElowenId,
                25,
                PlayerLocationIds.World,
                [
                    "character_event.elowen.tide_marks_at_the_well.1",
                    "character_event.elowen.tide_marks_at_the_well.2",
                    "character_event.elowen.tide_marks_at_the_well.3"
                ],
                RequiredNpcDialogueKey: "village.npc.elowen.well"
            ),
            new(
                ElowenWaterlineReadTogetherId,
                VillageCatalog.ElowenId,
                60,
                PlayerLocationIds.World,
                [
                    "character_event.elowen.waterline_read_together.1",
                    "character_event.elowen.waterline_read_together.2",
                    "character_event.elowen.waterline_read_together.3"
                ],
                RequiredPreviousEventId: ElowenTideMarksAtTheWellId,
                RequiredNpcDialogueKey: "village.npc.elowen.plaza"
            ),
            new(
                VessaBitterLeafWarmCupId,
                VillageCatalog.VessaId,
                25,
                PlayerLocationIds.StarweaverTeaHouse,
                [
                    "character_event.vessa.bitter_leaf_warm_cup.1",
                    "character_event.vessa.bitter_leaf_warm_cup.2",
                    "character_event.vessa.bitter_leaf_warm_cup.3"
                ],
                RequiredNpcDialogueKey: "village.npc.vessa.tea_house"
            ),
            new(
                VessaPathThatListensBackId,
                VillageCatalog.VessaId,
                60,
                PlayerLocationIds.World,
                [
                    "character_event.vessa.path_that_listens_back.1",
                    "character_event.vessa.path_that_listens_back.2",
                    "character_event.vessa.path_that_listens_back.3"
                ],
                RequiredPreviousEventId: VessaBitterLeafWarmCupId,
                RequiredNpcDialogueKey: "village.npc.vessa.route"
            ),
            new(
                YvaraSeedsBeyondTheCalendarId,
                VillageCatalog.YvaraId,
                25,
                PlayerLocationIds.TwilightEmporium,
                [
                    "character_event.yvara.seeds_beyond_the_calendar.1",
                    "character_event.yvara.seeds_beyond_the_calendar.2",
                    "character_event.yvara.seeds_beyond_the_calendar.3"
                ],
                RequiredNpcDialogueKey: "village.npc.yvara.emporium"
            ),
            new(
                YvaraASeasonCarriedGentlyId,
                VillageCatalog.YvaraId,
                60,
                PlayerLocationIds.TwilightEmporium,
                [
                    "character_event.yvara.a_season_carried_gently.1",
                    "character_event.yvara.a_season_carried_gently.2",
                    "character_event.yvara.a_season_carried_gently.3"
                ],
                RequiredPreviousEventId: YvaraSeedsBeyondTheCalendarId,
                RequiredNpcDialogueKey: "village.npc.yvara.emporium"
            ),
            new(
                BrialBlossomsBetweenHarvestsId,
                VillageCatalog.BrialId,
                25,
                PlayerLocationIds.StarweaverTeaHouse,
                [
                    "character_event.brial.blossoms_between_harvests.1",
                    "character_event.brial.blossoms_between_harvests.2",
                    "character_event.brial.blossoms_between_harvests.3"
                ],
                RequiredNpcDialogueKey: "village.npc.brial.tea_house"
            ),
            new(
                BrialAPathLeftForTheBeesId,
                VillageCatalog.BrialId,
                60,
                PlayerLocationIds.StarweaverTeaHouse,
                [
                    "character_event.brial.a_path_left_for_the_bees.1",
                    "character_event.brial.a_path_left_for_the_bees.2",
                    "character_event.brial.a_path_left_for_the_bees.3"
                ],
                RequiredPreviousEventId: BrialBlossomsBetweenHarvestsId,
                RequiredNpcDialogueKey: "village.npc.brial.tea_house"
            ),
            new(
                PavriTheVisibleMendId,
                VillageCatalog.PavriId,
                25,
                PlayerLocationIds.MoonstoneWorkshop,
                [
                    "character_event.pavri.the_visible_mend.1",
                    "character_event.pavri.the_visible_mend.2",
                    "character_event.pavri.the_visible_mend.3"
                ],
                RequiredNpcDialogueKey: "village.npc.pavri.workshop"
            ),
            new(
                PavriClothThatKeepsWarmthId,
                VillageCatalog.PavriId,
                60,
                PlayerLocationIds.MoonstoneWorkshop,
                [
                    "character_event.pavri.cloth_that_keeps_warmth.1",
                    "character_event.pavri.cloth_that_keeps_warmth.2",
                    "character_event.pavri.cloth_that_keeps_warmth.3"
                ],
                RequiredPreviousEventId: PavriTheVisibleMendId,
                RequiredNpcDialogueKey: "village.npc.pavri.workshop"
            ),
            new(
                RovenTheRouteWithRoomToRestId,
                VillageCatalog.RovenId,
                25,
                PlayerLocationIds.StarlightPost,
                [
                    "character_event.roven.the_route_with_room_to_rest.1",
                    "character_event.roven.the_route_with_room_to_rest.2",
                    "character_event.roven.the_route_with_room_to_rest.3"
                ],
                RequiredNpcDialogueKey: "village.npc.roven.starlight_post"
            ),
            new(
                RovenLightsThatWaitForReturnId,
                VillageCatalog.RovenId,
                60,
                PlayerLocationIds.World,
                [
                    "character_event.roven.lights_that_wait_for_return.1",
                    "character_event.roven.lights_that_wait_for_return.2",
                    "character_event.roven.lights_that_wait_for_return.3"
                ],
                RequiredPreviousEventId: RovenTheRouteWithRoomToRestId,
                RequiredNpcDialogueKey: "village.npc.roven.plaza"
            )
        ];

    public static readonly IReadOnlyDictionary<string, CharacterEventDefinition>
        ById = BuildById();

    private static IReadOnlyDictionary<string, CharacterEventDefinition>
        BuildById()
    {
        var byId = new Dictionary<string, CharacterEventDefinition>(
            StringComparer.Ordinal
        );
        foreach (var definition in Definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Id) ||
                !VillageCatalog.Npcs.TryGetValue(
                    definition.NpcId,
                    out var npc
                ) ||
                definition.RequiredRelationshipPoints is < 0 or > 100 ||
                string.IsNullOrWhiteSpace(definition.RequiredLocationId) ||
                definition.DialogueKeys.Count != 3 ||
                definition.DialogueKeys.Any(string.IsNullOrWhiteSpace) ||
                definition.DialogueKeys.Distinct(StringComparer.Ordinal)
                    .Count() != definition.DialogueKeys.Count ||
                !npc.Schedule.Any(entry =>
                    entry.LocationId == definition.RequiredLocationId) ||
                !byId.TryAdd(definition.Id, definition))
            {
                throw new InvalidOperationException(
                    $"Invalid character event catalog entry: {definition.Id}."
                );
            }

            if (definition.RequiredNpcDialogueKey is not null &&
                !npc.Schedule.Any(entry =>
                    entry.LocationId == definition.RequiredLocationId &&
                    entry.DialogueKey == definition.RequiredNpcDialogueKey))
            {
                throw new InvalidOperationException(
                    $"Character event {definition.Id} requires an unknown NPC dialogue."
                );
            }

            if (definition.RequiredPreviousEventId is null)
            {
                continue;
            }

            if (!byId.TryGetValue(
                    definition.RequiredPreviousEventId,
                    out var previous
                ) ||
                previous.NpcId != definition.NpcId ||
                previous.RequiredRelationshipPoints >=
                    definition.RequiredRelationshipPoints)
            {
                throw new InvalidOperationException(
                    $"Character event {definition.Id} has an invalid prerequisite."
                );
            }
        }

        foreach (var npcId in VillageCatalog.Npcs.Keys)
        {
            var chain = Definitions
                .Where(definition => definition.NpcId == npcId)
                .OrderBy(definition =>
                    definition.RequiredRelationshipPoints)
                .ToArray();
            if (chain.Length != 2 ||
                chain[0].RequiredRelationshipPoints != 25 ||
                chain[0].RequiredPreviousEventId is not null ||
                chain[1].RequiredRelationshipPoints != 60 ||
                chain[1].RequiredPreviousEventId != chain[0].Id)
            {
                throw new InvalidOperationException(
                    $"Village NPC {npcId} requires one complete 25/60 event chain."
                );
            }
        }

        return byId;
    }
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
        if (_activeEventId is not null ||
            selectedItemId != DataCatalog.HandId)
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
                (definition.RequiredNpcDialogueKey is not null &&
                    definition.RequiredNpcDialogueKey != npc.DialogueKey) ||
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
            _completedDays.ContainsKey(definition.Id) ||
            _activeEventId is not null)
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
