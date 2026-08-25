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
    public const string NpcALioraMarginOfLivingRoutesId =
        "npc_a_liora_margin_of_living_routes";
    public const string NpcALioraFirstUncopiedChartId =
        "npc_a_liora_first_uncopied_chart";
    public const string TaviCrackedMoonRuneId =
        "tavi_cracked_moon_rune";
    public const string TaviMendedLightId =
        "tavi_mended_light";
    public const string NpcATaviStoneThatAnswersFootstepsId =
        "npc_a_tavi_stone_that_answers_footsteps";
    public const string NpcATaviJointWithRoomToMoveId =
        "npc_a_tavi_joint_with_room_to_move";
    public const string NemiUndeliverableLetterId =
        "nemi_undeliverable_letter";
    public const string NemiStarChartRouteId =
        "nemi_star_chart_route";
    public const string NpcBNemiDeliveryThatNeededNoAnswerId =
        "npc_b_nemi_delivery_that_needed_no_answer";
    public const string NpcBNemiHookForHerOwnMailbagId =
        "npc_b_nemi_hook_for_her_own_mailbag";
    public const string KaelBrokenBlueRuneId =
        "kael_broken_blue_rune";
    public const string KaelSafeReturnRouteId =
        "kael_safe_return_route";
    public const string NpcBKaelPatrolLeftUnfinishedOnPurposeId =
        "npc_b_kael_patrol_left_unfinished_on_purpose";
    public const string NpcBKaelLastMarkerOnTheReturnBoardId =
        "npc_b_kael_last_marker_on_the_return_board";
    public const string SelaTemperedStarlightId =
        "sela_tempered_starlight";
    public const string SelaSharedForgeRhythmId =
        "sela_shared_forge_rhythm";
    public const string NpcBSelaInstructionsBeyondHerHandsId =
        "npc_b_sela_instructions_beyond_her_hands";
    public const string NpcBSelaHammerFittedToHerHandId =
        "npc_b_sela_hammer_fitted_to_her_hand";
    public const string OrinUnpricedWaybillId =
        "orin_unpriced_waybill";
    public const string OrinSharedLanternRouteId =
        "orin_shared_lantern_route";
    public const string NpcAOrinOrderHeDeclinedId =
        "npc_a_orin_order_he_declined";
    public const string NpcAOrinCaseHeUnpackedId =
        "npc_a_orin_case_he_unpacked";
    public const string ElowenTideMarksAtTheWellId =
        "elowen_tide_marks_at_the_well";
    public const string ElowenWaterlineReadTogetherId =
        "elowen_waterline_read_together";
    public const string NpcCElowenWaterWithTwoHonestNamesId =
        "npc_c_elowen_water_with_two_honest_names";
    public const string NpcCElowenMarkerAllowedToDriftId =
        "npc_c_elowen_marker_allowed_to_drift";
    public const string VessaBitterLeafWarmCupId =
        "vessa_bitter_leaf_warm_cup";
    public const string VessaPathThatListensBackId =
        "vessa_path_that_listens_back";
    public const string NpcAVessaPatchLeftUngatheredId =
        "npc_a_vessa_patch_left_ungathered";
    public const string NpcAVessaCupBrewedForHerselfId =
        "npc_a_vessa_cup_brewed_for_herself";
    public const string HaldenWeatherInTheFodderId =
        "halden_weather_in_the_fodder";
    public const string HaldenThreeBreathsOneRhythmId =
        "halden_three_breaths_one_rhythm";
    public const string NpcBHaldenBowlThatDidNotNeedEmptyingId =
        "npc_b_halden_bowl_that_did_not_need_emptying";
    public const string NpcBHaldenBellHeChoseNotToRingId =
        "npc_b_halden_bell_he_chose_not_to_ring";
    public const string MaveaFourBowlsOneTableId =
        "mavea_four_bowls_one_table";
    public const string MaveaWarmthThatKeepsId =
        "mavea_warmth_that_keeps";
    public const string NpcCMaveaRecipeThatChangedWithTheTableId =
        "npc_c_mavea_recipe_that_changed_with_the_table";
    public const string NpcCMaveaLastJarOpenedOnAnOrdinaryDayId =
        "npc_c_mavea_last_jar_opened_on_an_ordinary_day";
    public const string SivrenUnfiledLanternsId =
        "sivren_unfiled_lanterns";
    public const string SivrenYearInThreeLightsId =
        "sivren_year_in_three_lights";
    public const string NpcCSivrenTwoMemoriesUnderOneDateId =
        "npc_c_sivren_two_memories_under_one_date";
    public const string NpcCSivrenFirstPersonFootnoteId =
        "npc_c_sivren_first_person_footnote";
    public const string DorrikChalkBeyondWallsId =
        "dorrik_chalk_beyond_walls";
    public const string DorrikRoomsThatBreatheId =
        "dorrik_rooms_that_breathe";
    public const string NpcCDorrikMaintenancePathBehindTheBraceId =
        "npc_c_dorrik_maintenance_path_behind_the_brace";
    public const string NpcCDorrikPlanReturnedToItsUsersId =
        "npc_c_dorrik_plan_returned_to_its_users";
    public const string YvaraSeedsBeyondTheCalendarId =
        "yvara_seeds_beyond_the_calendar";
    public const string YvaraASeasonCarriedGentlyId =
        "yvara_a_season_carried_gently";
    public const string NpcDYvaraTheDaySheLeftTheCaseClosedId =
        "npc_d_yvara_the_day_she_left_the_case_closed";
    public const string NpcDYvaraASeedRecordInTwoHandsId =
        "npc_d_yvara_a_seed_record_in_two_hands";
    public const string BrialBlossomsBetweenHarvestsId =
        "brial_blossoms_between_harvests";
    public const string BrialAPathLeftForTheBeesId =
        "brial_a_path_left_for_the_bees";
    public const string NpcDBrialTheOrchardRoundWithAnEmptyBasketId =
        "npc_d_brial_the_orchard_round_with_an_empty_basket";
    public const string NpcDBrialThePruningMarkHeErasedId =
        "npc_d_brial_the_pruning_mark_he_erased";
    public const string PavriTheVisibleMendId =
        "pavri_the_visible_mend";
    public const string PavriClothThatKeepsWarmthId =
        "pavri_cloth_that_keeps_warmth";
    public const string NpcDPavriTheCuffTestedInMotionId =
        "npc_d_pavri_the_cuff_tested_in_motion";
    public const string NpcDPavriOneStitchBesideTheOldId =
        "npc_d_pavri_one_stitch_beside_the_old";
    public const string RovenTheRouteWithRoomToRestId =
        "roven_the_route_with_room_to_rest";
    public const string RovenLightsThatWaitForReturnId =
        "roven_lights_that_wait_for_return";
    public const string NpcDRovenTheCornerPeopleAlreadyChoseId =
        "npc_d_roven_the_corner_people_already_chose";
    public const string NpcDRovenARouteForAnOrdinaryDayId =
        "npc_d_roven_a_route_for_an_ordinary_day";
    public static readonly IReadOnlySet<string> NpcAIds =
        new HashSet<string>(
            [
                VillageCatalog.LioraId,
                VillageCatalog.TaviId,
                VillageCatalog.OrinId,
                VillageCatalog.VessaId
            ],
            StringComparer.Ordinal
        );
    public static readonly IReadOnlySet<string> NpcBIds =
        new HashSet<string>(
            [
                VillageCatalog.NemiId,
                VillageCatalog.KaelId,
                VillageCatalog.SelaId,
                VillageCatalog.HaldenId
            ],
            StringComparer.Ordinal
        );
    public static readonly IReadOnlySet<string> NpcCIds =
        new HashSet<string>(
            [
                VillageCatalog.ElowenId,
                VillageCatalog.MaveaId,
                VillageCatalog.SivrenId,
                VillageCatalog.DorrikId
            ],
            StringComparer.Ordinal
        );
    public static readonly IReadOnlySet<string> NpcDIds =
        new HashSet<string>(
            [
                VillageCatalog.YvaraId,
                VillageCatalog.BrialId,
                VillageCatalog.PavriId,
                VillageCatalog.RovenId
            ],
            StringComparer.Ordinal
        );
    public static readonly IReadOnlySet<string> FourEventNpcIds =
        new HashSet<string>(
            NpcAIds.Concat(NpcBIds).Concat(NpcCIds).Concat(NpcDIds),
            StringComparer.Ordinal
        );

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
                NpcALioraMarginOfLivingRoutesId,
                VillageCatalog.LioraId,
                75,
                PlayerLocationIds.MoonlitArchive,
                [
                    "character_event.npc_a.liora.margin_of_living_routes.1",
                    "character_event.npc_a.liora.margin_of_living_routes.2",
                    "character_event.npc_a.liora.margin_of_living_routes.3"
                ],
                RequiredPreviousEventId: LioraRememberedWayHomeId,
                RequiredNpcDialogueKey: "village.npc.liora.archive"
            ),
            new(
                NpcALioraFirstUncopiedChartId,
                VillageCatalog.LioraId,
                90,
                PlayerLocationIds.MoonlitArchive,
                [
                    "character_event.npc_a.liora.first_uncopied_chart.1",
                    "character_event.npc_a.liora.first_uncopied_chart.2",
                    "character_event.npc_a.liora.first_uncopied_chart.3"
                ],
                RequiredPreviousEventId: NpcALioraMarginOfLivingRoutesId,
                RequiredNpcDialogueKey: "village.npc.liora.archive"
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
                NpcATaviStoneThatAnswersFootstepsId,
                VillageCatalog.TaviId,
                75,
                PlayerLocationIds.MoonstoneWorkshop,
                [
                    "character_event.npc_a.tavi.stone_that_answers_footsteps.1",
                    "character_event.npc_a.tavi.stone_that_answers_footsteps.2",
                    "character_event.npc_a.tavi.stone_that_answers_footsteps.3"
                ],
                RequiredPreviousEventId: TaviMendedLightId,
                RequiredNpcDialogueKey: "village.npc.tavi.workshop"
            ),
            new(
                NpcATaviJointWithRoomToMoveId,
                VillageCatalog.TaviId,
                90,
                PlayerLocationIds.World,
                [
                    "character_event.npc_a.tavi.joint_with_room_to_move.1",
                    "character_event.npc_a.tavi.joint_with_room_to_move.2",
                    "character_event.npc_a.tavi.joint_with_room_to_move.3"
                ],
                RequiredPreviousEventId:
                    NpcATaviStoneThatAnswersFootstepsId,
                RequiredNpcDialogueKey: "village.npc.tavi.plaza"
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
                NpcBNemiDeliveryThatNeededNoAnswerId,
                VillageCatalog.NemiId,
                75,
                PlayerLocationIds.World,
                [
                    "character_event.npc_b.nemi.delivery_that_needed_no_answer.1",
                    "character_event.npc_b.nemi.delivery_that_needed_no_answer.2",
                    "character_event.npc_b.nemi.delivery_that_needed_no_answer.3"
                ],
                RequiredPreviousEventId: NemiStarChartRouteId,
                RequiredNpcDialogueKey: "village.npc.nemi.route"
            ),
            new(
                NpcBNemiHookForHerOwnMailbagId,
                VillageCatalog.NemiId,
                90,
                PlayerLocationIds.StarlightPost,
                [
                    "character_event.npc_b.nemi.hook_for_her_own_mailbag.1",
                    "character_event.npc_b.nemi.hook_for_her_own_mailbag.2",
                    "character_event.npc_b.nemi.hook_for_her_own_mailbag.3"
                ],
                RequiredPreviousEventId:
                    NpcBNemiDeliveryThatNeededNoAnswerId,
                RequiredNpcDialogueKey: "village.npc.nemi.starlight_post"
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
                NpcBKaelPatrolLeftUnfinishedOnPurposeId,
                VillageCatalog.KaelId,
                75,
                PlayerLocationIds.StarfallWatch,
                [
                    "character_event.npc_b.kael.patrol_left_unfinished_on_purpose.1",
                    "character_event.npc_b.kael.patrol_left_unfinished_on_purpose.2",
                    "character_event.npc_b.kael.patrol_left_unfinished_on_purpose.3"
                ],
                RequiredPreviousEventId: KaelSafeReturnRouteId,
                RequiredNpcDialogueKey: "village.npc.kael.starfall_watch"
            ),
            new(
                NpcBKaelLastMarkerOnTheReturnBoardId,
                VillageCatalog.KaelId,
                90,
                PlayerLocationIds.World,
                [
                    "character_event.npc_b.kael.last_marker_on_the_return_board.1",
                    "character_event.npc_b.kael.last_marker_on_the_return_board.2",
                    "character_event.npc_b.kael.last_marker_on_the_return_board.3"
                ],
                RequiredPreviousEventId:
                    NpcBKaelPatrolLeftUnfinishedOnPurposeId,
                RequiredNpcDialogueKey: "village.npc.kael.plaza"
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
                NpcBSelaInstructionsBeyondHerHandsId,
                VillageCatalog.SelaId,
                75,
                PlayerLocationIds.World,
                [
                    "character_event.npc_b.sela.instructions_beyond_her_hands.1",
                    "character_event.npc_b.sela.instructions_beyond_her_hands.2",
                    "character_event.npc_b.sela.instructions_beyond_her_hands.3"
                ],
                RequiredPreviousEventId: SelaSharedForgeRhythmId,
                RequiredNpcDialogueKey: "village.npc.sela.plaza"
            ),
            new(
                NpcBSelaHammerFittedToHerHandId,
                VillageCatalog.SelaId,
                90,
                PlayerLocationIds.World,
                [
                    "character_event.npc_b.sela.hammer_fitted_to_her_hand.1",
                    "character_event.npc_b.sela.hammer_fitted_to_her_hand.2",
                    "character_event.npc_b.sela.hammer_fitted_to_her_hand.3"
                ],
                RequiredPreviousEventId:
                    NpcBSelaInstructionsBeyondHerHandsId,
                RequiredNpcDialogueKey: "village.npc.sela.workshop"
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
                NpcAOrinOrderHeDeclinedId,
                VillageCatalog.OrinId,
                75,
                PlayerLocationIds.TwilightEmporium,
                [
                    "character_event.npc_a.orin.order_he_declined.1",
                    "character_event.npc_a.orin.order_he_declined.2",
                    "character_event.npc_a.orin.order_he_declined.3"
                ],
                RequiredPreviousEventId: OrinSharedLanternRouteId,
                RequiredNpcDialogueKey: "village.npc.orin.emporium"
            ),
            new(
                NpcAOrinCaseHeUnpackedId,
                VillageCatalog.OrinId,
                90,
                PlayerLocationIds.TwilightEmporium,
                [
                    "character_event.npc_a.orin.case_he_unpacked.1",
                    "character_event.npc_a.orin.case_he_unpacked.2",
                    "character_event.npc_a.orin.case_he_unpacked.3"
                ],
                RequiredPreviousEventId: NpcAOrinOrderHeDeclinedId,
                RequiredNpcDialogueKey: "village.npc.orin.emporium"
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
                NpcBHaldenBowlThatDidNotNeedEmptyingId,
                VillageCatalog.HaldenId,
                75,
                PlayerLocationIds.World,
                [
                    "character_event.npc_b.halden.bowl_that_did_not_need_emptying.1",
                    "character_event.npc_b.halden.bowl_that_did_not_need_emptying.2",
                    "character_event.npc_b.halden.bowl_that_did_not_need_emptying.3"
                ],
                RequiredPreviousEventId: HaldenThreeBreathsOneRhythmId,
                RequiredNpcDialogueKey: "village.npc.halden.stocktake"
            ),
            new(
                NpcBHaldenBellHeChoseNotToRingId,
                VillageCatalog.HaldenId,
                90,
                PlayerLocationIds.World,
                [
                    "character_event.npc_b.halden.bell_he_chose_not_to_ring.1",
                    "character_event.npc_b.halden.bell_he_chose_not_to_ring.2",
                    "character_event.npc_b.halden.bell_he_chose_not_to_ring.3"
                ],
                RequiredPreviousEventId:
                    NpcBHaldenBowlThatDidNotNeedEmptyingId,
                RequiredNpcDialogueKey: "village.npc.halden.evening"
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
                NpcCMaveaRecipeThatChangedWithTheTableId,
                VillageCatalog.MaveaId,
                75,
                PlayerLocationIds.StarweaverTeaHouse,
                [
                    "character_event.npc_c.mavea.recipe_that_changed_with_the_table.1",
                    "character_event.npc_c.mavea.recipe_that_changed_with_the_table.2",
                    "character_event.npc_c.mavea.recipe_that_changed_with_the_table.3"
                ],
                RequiredPreviousEventId: MaveaWarmthThatKeepsId,
                RequiredNpcDialogueKey: "village.npc.mavea.tea_house"
            ),
            new(
                NpcCMaveaLastJarOpenedOnAnOrdinaryDayId,
                VillageCatalog.MaveaId,
                90,
                PlayerLocationIds.StarweaverTeaHouse,
                [
                    "character_event.npc_c.mavea.last_jar_opened_on_an_ordinary_day.1",
                    "character_event.npc_c.mavea.last_jar_opened_on_an_ordinary_day.2",
                    "character_event.npc_c.mavea.last_jar_opened_on_an_ordinary_day.3"
                ],
                RequiredPreviousEventId:
                    NpcCMaveaRecipeThatChangedWithTheTableId,
                RequiredNpcDialogueKey: "village.npc.mavea.evening"
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
                NpcCSivrenTwoMemoriesUnderOneDateId,
                VillageCatalog.SivrenId,
                75,
                PlayerLocationIds.MoonlitArchive,
                [
                    "character_event.npc_c.sivren.two_memories_under_one_date.1",
                    "character_event.npc_c.sivren.two_memories_under_one_date.2",
                    "character_event.npc_c.sivren.two_memories_under_one_date.3"
                ],
                RequiredPreviousEventId: SivrenYearInThreeLightsId,
                RequiredNpcDialogueKey: "village.npc.sivren.archive"
            ),
            new(
                NpcCSivrenFirstPersonFootnoteId,
                VillageCatalog.SivrenId,
                90,
                PlayerLocationIds.World,
                [
                    "character_event.npc_c.sivren.first_person_footnote.1",
                    "character_event.npc_c.sivren.first_person_footnote.2",
                    "character_event.npc_c.sivren.first_person_footnote.3"
                ],
                RequiredPreviousEventId:
                    NpcCSivrenTwoMemoriesUnderOneDateId,
                RequiredNpcDialogueKey: "village.npc.sivren.evening"
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
                NpcCDorrikMaintenancePathBehindTheBraceId,
                VillageCatalog.DorrikId,
                75,
                PlayerLocationIds.MoonstoneWorkshop,
                [
                    "character_event.npc_c.dorrik.maintenance_path_behind_the_brace.1",
                    "character_event.npc_c.dorrik.maintenance_path_behind_the_brace.2",
                    "character_event.npc_c.dorrik.maintenance_path_behind_the_brace.3"
                ],
                RequiredPreviousEventId: DorrikRoomsThatBreatheId,
                RequiredNpcDialogueKey: "village.npc.dorrik.workshop"
            ),
            new(
                NpcCDorrikPlanReturnedToItsUsersId,
                VillageCatalog.DorrikId,
                90,
                PlayerLocationIds.World,
                [
                    "character_event.npc_c.dorrik.plan_returned_to_its_users.1",
                    "character_event.npc_c.dorrik.plan_returned_to_its_users.2",
                    "character_event.npc_c.dorrik.plan_returned_to_its_users.3"
                ],
                RequiredPreviousEventId:
                    NpcCDorrikMaintenancePathBehindTheBraceId,
                RequiredNpcDialogueKey: "village.npc.dorrik.plaza"
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
                NpcCElowenWaterWithTwoHonestNamesId,
                VillageCatalog.ElowenId,
                75,
                PlayerLocationIds.World,
                [
                    "character_event.npc_c.elowen.water_with_two_honest_names.1",
                    "character_event.npc_c.elowen.water_with_two_honest_names.2",
                    "character_event.npc_c.elowen.water_with_two_honest_names.3"
                ],
                RequiredPreviousEventId: ElowenWaterlineReadTogetherId,
                RequiredNpcDialogueKey: "village.npc.elowen.well"
            ),
            new(
                NpcCElowenMarkerAllowedToDriftId,
                VillageCatalog.ElowenId,
                90,
                PlayerLocationIds.World,
                [
                    "character_event.npc_c.elowen.marker_allowed_to_drift.1",
                    "character_event.npc_c.elowen.marker_allowed_to_drift.2",
                    "character_event.npc_c.elowen.marker_allowed_to_drift.3"
                ],
                RequiredPreviousEventId:
                    NpcCElowenWaterWithTwoHonestNamesId,
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
                NpcAVessaPatchLeftUngatheredId,
                VillageCatalog.VessaId,
                75,
                PlayerLocationIds.World,
                [
                    "character_event.npc_a.vessa.patch_left_ungathered.1",
                    "character_event.npc_a.vessa.patch_left_ungathered.2",
                    "character_event.npc_a.vessa.patch_left_ungathered.3"
                ],
                RequiredPreviousEventId: VessaPathThatListensBackId,
                RequiredNpcDialogueKey: "village.npc.vessa.route"
            ),
            new(
                NpcAVessaCupBrewedForHerselfId,
                VillageCatalog.VessaId,
                90,
                PlayerLocationIds.StarweaverTeaHouse,
                [
                    "character_event.npc_a.vessa.cup_brewed_for_herself.1",
                    "character_event.npc_a.vessa.cup_brewed_for_herself.2",
                    "character_event.npc_a.vessa.cup_brewed_for_herself.3"
                ],
                RequiredPreviousEventId: NpcAVessaPatchLeftUngatheredId,
                RequiredNpcDialogueKey: "village.npc.vessa.tea_house"
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
                NpcDYvaraTheDaySheLeftTheCaseClosedId,
                VillageCatalog.YvaraId,
                75,
                PlayerLocationIds.TwilightEmporium,
                [
                    "character_event.npc_d.yvara.the_day_she_left_the_case_closed.1",
                    "character_event.npc_d.yvara.the_day_she_left_the_case_closed.2",
                    "character_event.npc_d.yvara.the_day_she_left_the_case_closed.3"
                ],
                RequiredPreviousEventId: YvaraASeasonCarriedGentlyId,
                RequiredNpcDialogueKey: "village.npc.yvara.emporium"
            ),
            new(
                NpcDYvaraASeedRecordInTwoHandsId,
                VillageCatalog.YvaraId,
                90,
                PlayerLocationIds.World,
                [
                    "character_event.npc_d.yvara.a_seed_record_in_two_hands.1",
                    "character_event.npc_d.yvara.a_seed_record_in_two_hands.2",
                    "character_event.npc_d.yvara.a_seed_record_in_two_hands.3"
                ],
                RequiredPreviousEventId:
                    NpcDYvaraTheDaySheLeftTheCaseClosedId,
                RequiredNpcDialogueKey: "village.npc.yvara.plaza"
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
                NpcDBrialTheOrchardRoundWithAnEmptyBasketId,
                VillageCatalog.BrialId,
                75,
                PlayerLocationIds.StarweaverTeaHouse,
                [
                    "character_event.npc_d.brial.the_orchard_round_with_an_empty_basket.1",
                    "character_event.npc_d.brial.the_orchard_round_with_an_empty_basket.2",
                    "character_event.npc_d.brial.the_orchard_round_with_an_empty_basket.3"
                ],
                RequiredPreviousEventId: BrialAPathLeftForTheBeesId,
                RequiredNpcDialogueKey: "village.npc.brial.tea_house"
            ),
            new(
                NpcDBrialThePruningMarkHeErasedId,
                VillageCatalog.BrialId,
                90,
                PlayerLocationIds.World,
                [
                    "character_event.npc_d.brial.the_pruning_mark_he_erased.1",
                    "character_event.npc_d.brial.the_pruning_mark_he_erased.2",
                    "character_event.npc_d.brial.the_pruning_mark_he_erased.3"
                ],
                RequiredPreviousEventId:
                    NpcDBrialTheOrchardRoundWithAnEmptyBasketId,
                RequiredNpcDialogueKey: "village.npc.brial.plaza"
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
                NpcDPavriTheCuffTestedInMotionId,
                VillageCatalog.PavriId,
                75,
                PlayerLocationIds.World,
                [
                    "character_event.npc_d.pavri.the_cuff_tested_in_motion.1",
                    "character_event.npc_d.pavri.the_cuff_tested_in_motion.2",
                    "character_event.npc_d.pavri.the_cuff_tested_in_motion.3"
                ],
                RequiredPreviousEventId: PavriClothThatKeepsWarmthId,
                RequiredNpcDialogueKey: "village.npc.pavri.plaza"
            ),
            new(
                NpcDPavriOneStitchBesideTheOldId,
                VillageCatalog.PavriId,
                90,
                PlayerLocationIds.MoonstoneWorkshop,
                [
                    "character_event.npc_d.pavri.one_stitch_beside_the_old.1",
                    "character_event.npc_d.pavri.one_stitch_beside_the_old.2",
                    "character_event.npc_d.pavri.one_stitch_beside_the_old.3"
                ],
                RequiredPreviousEventId: NpcDPavriTheCuffTestedInMotionId,
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
            ),
            new(
                NpcDRovenTheCornerPeopleAlreadyChoseId,
                VillageCatalog.RovenId,
                75,
                PlayerLocationIds.World,
                [
                    "character_event.npc_d.roven.the_corner_people_already_chose.1",
                    "character_event.npc_d.roven.the_corner_people_already_chose.2",
                    "character_event.npc_d.roven.the_corner_people_already_chose.3"
                ],
                RequiredPreviousEventId: RovenLightsThatWaitForReturnId,
                RequiredNpcDialogueKey: "village.npc.roven.plaza"
            ),
            new(
                NpcDRovenARouteForAnOrdinaryDayId,
                VillageCatalog.RovenId,
                90,
                PlayerLocationIds.StarlightPost,
                [
                    "character_event.npc_d.roven.a_route_for_an_ordinary_day.1",
                    "character_event.npc_d.roven.a_route_for_an_ordinary_day.2",
                    "character_event.npc_d.roven.a_route_for_an_ordinary_day.3"
                ],
                RequiredPreviousEventId:
                    NpcDRovenTheCornerPeopleAlreadyChoseId,
                RequiredNpcDialogueKey: "village.npc.roven.starlight_post"
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
            var expectedThresholds = FourEventNpcIds.Contains(npcId)
                ? new[] { 25, 60, 75, 90 }
                : new[] { 25, 60 };
            if (chain.Length != expectedThresholds.Length)
            {
                throw new InvalidOperationException(
                    $"Village NPC {npcId} has an incomplete event chain."
                );
            }

            for (var index = 0; index < chain.Length; index++)
            {
                var expectedPrevious = index == 0
                    ? null
                    : chain[index - 1].Id;
                if (chain[index].RequiredRelationshipPoints !=
                        expectedThresholds[index] ||
                    chain[index].RequiredPreviousEventId != expectedPrevious)
                {
                    throw new InvalidOperationException(
                        $"Village NPC {npcId} has an invalid event sequence."
                    );
                }
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
