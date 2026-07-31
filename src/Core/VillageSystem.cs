namespace Luminfield.Core;

public enum NpcFacing
{
    Down,
    Up,
    Left,
    Right
}

public enum GiftReaction
{
    Loved,
    Liked,
    Neutral,
    Disliked
}

public enum RelationshipTier
{
    NewAcquaintance,
    TrustedFriend,
    KindredLight
}

public readonly record struct GridArea(
    int MinX,
    int MinY,
    int MaxX,
    int MaxY
)
{
    public bool Contains(GridPosition cell) =>
        cell.X >= MinX &&
        cell.X <= MaxX &&
        cell.Y >= MinY &&
        cell.Y <= MaxY;
}

public sealed record VillageLandmarkDefinition(
    string Id,
    GridPosition Anchor,
    int AtlasIndex,
    string NameKey,
    IReadOnlyList<GridArea> CollisionAreas
);

public sealed record NpcScheduleEntry(
    int StartMinute,
    int EndMinute,
    string LocationId,
    GridPosition Position,
    NpcFacing Facing,
    string DialogueKey,
    IReadOnlyList<int> WeekdayIndices
)
{
    public bool Matches(int day, int minuteOfDay)
    {
        if (minuteOfDay < StartMinute || minuteOfDay >= EndMinute)
        {
            return false;
        }

        return WeekdayIndices.Count == 0 ||
            WeekdayIndices.Contains(CalendarSystem.WeekdayIndex(day));
    }
}

public sealed record VillageNpcDefinition(
    string Id,
    string NameKey,
    string RoleKey,
    string IntroductionKey,
    int AtlasRow,
    IReadOnlyList<string> LovedGiftIds,
    IReadOnlyList<ItemKind> LikedGiftKinds,
    IReadOnlyList<ItemKind> DislikedGiftKinds,
    IReadOnlyList<NpcScheduleEntry> Schedule
);

public sealed record VillageNpcState(
    VillageNpcDefinition Definition,
    string LocationId,
    GridPosition Position,
    NpcFacing Facing,
    string DialogueKey
);

public sealed record VillageConversation(
    string NpcId,
    string NameKey,
    string RoleKey,
    string DialogueKey,
    bool FirstMeeting,
    GiftReaction? GiftReaction,
    int RelationshipPoints,
    RelationshipTier RelationshipTier,
    CharacterEventDialogue? CharacterEvent = null
);

public sealed record VillageInteractionCheck(
    VillageNpcState? Npc,
    bool IsAvailable,
    bool IsGift,
    GiftReaction? GiftReaction,
    string FailureKey
);

public static class VillageCatalog
{
    public const string LioraId = "liora";
    public const string TaviId = "tavi";
    public const string NemiId = "nemi";
    public const string SelaId = "sela";
    public const string ElowenId = "elowen";
    public const string VessaId = "vessa";
    public const string OrinId = "orin";
    public const string KaelId = "kael";
    public const string VillageGateLandmarkId = "lumen_village_gate";
    public const string MoonlitArchiveLandmarkId = "moonlit_archive";
    public const string MoonstoneWorkshopLandmarkId =
        "moonstone_workshop";

    public static readonly GridArea VillageBounds = new(77, 30, 115, 63);
    public static readonly GridPosition VillageGateCell = new(97, 59);
    public static readonly GridPosition MoonlitArchiveDoorCell = new(86, 41);
    public static readonly GridPosition MoonlitArchiveExitCell = new(20, 18);
    public static readonly GridPosition MoonlitArchiveDeskCell = new(20, 9);
    public static readonly GridPosition MoonstoneWorkshopDoorCell =
        new(85, 54);
    public static readonly GridPosition MoonstoneWorkshopExitCell =
        new(20, 19);
    public static readonly GridPosition MoonRuneWorkbenchCell =
        new(20, 9);
    public const int MoonlitArchiveOpenMinute = 8 * 60;
    public const int MoonlitArchiveCloseMinute = 20 * 60;
    public const int MoonstoneWorkshopOpenMinute = 8 * 60;
    public const int MoonstoneWorkshopCloseMinute = 19 * 60;

    public static readonly IReadOnlyList<VillageLandmarkDefinition> Landmarks =
    [
        new(
            MoonlitArchiveLandmarkId,
            MoonlitArchiveDoorCell,
            0,
            "village.landmark.archive",
            [new GridArea(82, 34, 90, 40)]
        ),
        new(
            "starweaver_tea_house",
            new GridPosition(107, 42),
            1,
            "village.landmark.tea_house",
            [new GridArea(102, 35, 111, 41)]
        ),
        new(
            MoonstoneWorkshopLandmarkId,
            MoonstoneWorkshopDoorCell,
            2,
            "village.landmark.workshop",
            [new GridArea(81, 47, 90, 53)]
        ),
        new(
            "starlight_well",
            new GridPosition(97, 50),
            3,
            "village.landmark.well",
            [new GridArea(94, 46, 100, 50)]
        ),
        new(
            VillageGateLandmarkId,
            VillageGateCell,
            4,
            "world.landmark.village_gate",
            [
                new GridArea(93, 56, 94, 59),
                new GridArea(100, 56, 101, 59)
            ]
        ),
        new(
            "village_sign",
            new GridPosition(91, 58),
            5,
            "village.landmark.sign",
            [new GridArea(91, 58, 91, 58)]
        ),
        new(
            "lantern_bench",
            new GridPosition(105, 50),
            6,
            "village.landmark.bench",
            [new GridArea(103, 50, 107, 50)]
        ),
        new(
            "glowflower_cart",
            new GridPosition(110, 53),
            7,
            "village.landmark.flower_cart",
            [new GridArea(109, 52, 111, 53)]
        )
    ];

    public static readonly IReadOnlyDictionary<string, VillageNpcDefinition>
        Npcs = new Dictionary<string, VillageNpcDefinition>(
            StringComparer.Ordinal
        )
        {
            [LioraId] = new(
                LioraId,
                "village.npc.liora.name",
                "village.npc.liora.role",
                "village.npc.liora.intro",
                0,
                [
                    DataCatalog.MoonrootId,
                    DataCatalog.MoonrootTonicId,
                    DataCatalog.CrystalShardId
                ],
                [ItemKind.Produce, ItemKind.Artisan],
                [ItemKind.Fertilizer, ItemKind.Placeable],
                [
                    Slot(
                        9,
                        18,
                        new GridPosition(102, 48),
                        NpcFacing.Left,
                        "village.npc.liora.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(86, 42),
                        NpcFacing.Down,
                        "village.npc.liora.morning"
                    ),
                    ArchiveSlot(
                        9,
                        13,
                        new GridPosition(12, 9),
                        NpcFacing.Left,
                        "village.npc.liora.archive"
                    ),
                    Slot(
                        13,
                        17,
                        new GridPosition(92, 48),
                        NpcFacing.Right,
                        "village.npc.liora.plaza"
                    ),
                    Slot(
                        17,
                        23,
                        new GridPosition(104, 43),
                        NpcFacing.Down,
                        "village.npc.liora.evening"
                    )
                ]
            ),
            [TaviId] = new(
                TaviId,
                "village.npc.tavi.name",
                "village.npc.tavi.role",
                "village.npc.tavi.intro",
                1,
                [
                    DataCatalog.LumenwoodId,
                    DataCatalog.CrystalShardId,
                    DataCatalog.MoonstonePathId
                ],
                [ItemKind.Resource, ItemKind.Artisan],
                [ItemKind.Seed, ItemKind.Fertilizer],
                [
                    Slot(
                        9,
                        18,
                        new GridPosition(92, 52),
                        NpcFacing.Right,
                        "village.npc.tavi.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(86, 55),
                        NpcFacing.Down,
                        "village.npc.tavi.morning"
                    ),
                    WorkshopSlot(
                        9,
                        13,
                        new GridPosition(13, 10),
                        NpcFacing.Right,
                        "village.npc.tavi.workshop"
                    ),
                    Slot(
                        13,
                        16,
                        new GridPosition(92, 51),
                        NpcFacing.Right,
                        "village.npc.tavi.plaza"
                    ),
                    Slot(
                        16,
                        23,
                        new GridPosition(107, 43),
                        NpcFacing.Down,
                        "village.npc.tavi.evening"
                    )
                ]
            ),
            [NemiId] = new(
                NemiId,
                "village.npc.nemi.name",
                "village.npc.nemi.role",
                "village.npc.nemi.intro",
                2,
                [
                    DataCatalog.StarbudId,
                    DataCatalog.CloudleafId,
                    DataCatalog.StarbudPreserveId
                ],
                [ItemKind.Produce, ItemKind.Seed],
                [ItemKind.Resource, ItemKind.Placeable],
                [
                    Slot(
                        9,
                        18,
                        new GridPosition(108, 54),
                        NpcFacing.Left,
                        "village.npc.nemi.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(97, 60),
                        NpcFacing.Up,
                        "village.npc.nemi.morning"
                    ),
                    Slot(
                        9,
                        13,
                        new GridPosition(93, 43),
                        NpcFacing.Left,
                        "village.npc.nemi.archive"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(108, 51),
                        NpcFacing.Down,
                        "village.npc.nemi.route"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(105, 43),
                        NpcFacing.Down,
                        "village.npc.nemi.evening"
                    )
                ]
            ),
            [SelaId] = new(
                SelaId,
                "village.npc.sela.name",
                "village.npc.sela.role",
                "village.npc.sela.intro",
                3,
                [
                    DataCatalog.CrystalShardId,
                    DataCatalog.MoonstonePathId,
                    DataCatalog.StarlightTorchId
                ],
                [ItemKind.Resource, ItemKind.Placeable],
                [ItemKind.Seed, ItemKind.Fertilizer],
                [
                    Slot(
                        9,
                        18,
                        new GridPosition(104, 52),
                        NpcFacing.Left,
                        "village.npc.sela.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(89, 54),
                        NpcFacing.Down,
                        "village.npc.sela.morning"
                    ),
                    Slot(
                        9,
                        13,
                        new GridPosition(90, 54),
                        NpcFacing.Left,
                        "village.npc.sela.workshop"
                    ),
                    Slot(
                        13,
                        17,
                        new GridPosition(99, 52),
                        NpcFacing.Up,
                        "village.npc.sela.plaza"
                    ),
                    Slot(
                        17,
                        23,
                        new GridPosition(109, 43),
                        NpcFacing.Left,
                        "village.npc.sela.evening"
                    )
                ]
            ),
            [ElowenId] = new(
                ElowenId,
                "village.npc.elowen.name",
                "village.npc.elowen.role",
                "village.npc.elowen.intro",
                4,
                [
                    DataCatalog.DewmelonId,
                    DataCatalog.CloudleafId,
                    DataCatalog.MoonrootTonicId
                ],
                [ItemKind.Produce, ItemKind.Artisan],
                [ItemKind.Placeable, ItemKind.Resource],
                [
                    Slot(
                        9,
                        18,
                        new GridPosition(100, 43),
                        NpcFacing.Down,
                        "village.npc.elowen.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(98, 58),
                        NpcFacing.Up,
                        "village.npc.elowen.morning"
                    ),
                    Slot(
                        9,
                        13,
                        new GridPosition(105, 44),
                        NpcFacing.Right,
                        "village.npc.elowen.well"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(100, 54),
                        NpcFacing.Left,
                        "village.npc.elowen.plaza"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(93, 44),
                        NpcFacing.Right,
                        "village.npc.elowen.evening"
                    )
                ]
            ),
            [VessaId] = new(
                VessaId,
                "village.npc.vessa.name",
                "village.npc.vessa.role",
                "village.npc.vessa.intro",
                5,
                [
                    DataCatalog.CloudleafId,
                    DataCatalog.MoonrootId,
                    DataCatalog.MoonrootTonicId
                ],
                [ItemKind.Produce, ItemKind.Seed],
                [ItemKind.Placeable, ItemKind.Fertilizer],
                [
                    Slot(
                        9,
                        18,
                        new GridPosition(88, 44),
                        NpcFacing.Right,
                        "village.npc.vessa.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(91, 44),
                        NpcFacing.Left,
                        "village.npc.vessa.morning"
                    ),
                    Slot(
                        9,
                        13,
                        new GridPosition(84, 44),
                        NpcFacing.Right,
                        "village.npc.vessa.tea_house"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(103, 54),
                        NpcFacing.Left,
                        "village.npc.vessa.route"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(110, 44),
                        NpcFacing.Left,
                        "village.npc.vessa.evening"
                    )
                ]
            ),
            [OrinId] = new(
                OrinId,
                "village.npc.orin.name",
                "village.npc.orin.role",
                "village.npc.orin.intro",
                6,
                [
                    DataCatalog.StarbudPreserveId,
                    DataCatalog.PrismcornId,
                    DataCatalog.GlowpeaId
                ],
                [ItemKind.Artisan, ItemKind.Produce],
                [ItemKind.Fertilizer, ItemKind.Resource],
                [
                    Slot(
                        9,
                        18,
                        new GridPosition(101, 54),
                        NpcFacing.Left,
                        "village.npc.orin.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(103, 44),
                        NpcFacing.Right,
                        "village.npc.orin.morning"
                    ),
                    Slot(
                        9,
                        13,
                        new GridPosition(109, 44),
                        NpcFacing.Left,
                        "village.npc.orin.market"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(102, 53),
                        NpcFacing.Right,
                        "village.npc.orin.plaza"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(93, 53),
                        NpcFacing.Right,
                        "village.npc.orin.evening"
                    )
                ]
            ),
            [KaelId] = new(
                KaelId,
                "village.npc.kael.name",
                "village.npc.kael.role",
                "village.npc.kael.intro",
                7,
                [
                    DataCatalog.CrystalShardId,
                    DataCatalog.StarlightTorchId,
                    DataCatalog.EmberbellId
                ],
                [ItemKind.Resource, ItemKind.Placeable],
                [ItemKind.Seed, ItemKind.Artisan],
                [
                    Slot(
                        9,
                        18,
                        new GridPosition(96, 43),
                        NpcFacing.Down,
                        "village.npc.kael.restday",
                        6
                    ),
                    Slot(
                        6,
                        9,
                        new GridPosition(95, 58),
                        NpcFacing.Up,
                        "village.npc.kael.morning"
                    ),
                    Slot(
                        9,
                        13,
                        new GridPosition(96, 54),
                        NpcFacing.Up,
                        "village.npc.kael.gate"
                    ),
                    Slot(
                        13,
                        18,
                        new GridPosition(98, 54),
                        NpcFacing.Down,
                        "village.npc.kael.plaza"
                    ),
                    Slot(
                        18,
                        23,
                        new GridPosition(99, 43),
                        NpcFacing.Left,
                        "village.npc.kael.evening"
                    )
                ]
            )
        };

    public static bool IsVillageCell(GridPosition cell) =>
        VillageBounds.Contains(cell);

    public static bool IsMoonlitArchiveDoor(GridPosition cell) =>
        cell == MoonlitArchiveDoorCell;

    public static bool IsMoonlitArchiveOpen(int minuteOfDay) =>
        minuteOfDay >= MoonlitArchiveOpenMinute &&
        minuteOfDay < MoonlitArchiveCloseMinute;

    public static bool IsMoonstoneWorkshopDoor(GridPosition cell) =>
        cell == MoonstoneWorkshopDoorCell;

    public static bool IsMoonstoneWorkshopOpen(int minuteOfDay) =>
        minuteOfDay >= MoonstoneWorkshopOpenMinute &&
        minuteOfDay < MoonstoneWorkshopCloseMinute;

    public static bool IsVillagePath(GridPosition cell)
    {
        if (!IsVillageCell(cell))
        {
            return false;
        }

        var mainLane = cell.X is >= 95 and <= 98 &&
            cell.Y is >= 31 and <= 62;
        var archiveLane = cell.Y is >= 42 and <= 44 &&
            cell.X is >= 84 and <= 110;
        var workshopLane = cell.Y is >= 52 and <= 54 &&
            cell.X is >= 84 and <= 110;
        var plaza = cell.X is >= 92 and <= 102 &&
            cell.Y is >= 44 and <= 54;
        return mainLane || archiveLane || workshopLane || plaza;
    }

    public static bool IsBlocked(GridPosition cell) =>
        Landmarks
            .SelectMany(landmark => landmark.CollisionAreas)
            .Any(area => area.Contains(cell));

    public static VillageNpcState? CurrentNpc(
        string npcId,
        int day,
        int minuteOfDay
    )
    {
        if (!Npcs.TryGetValue(npcId, out var definition))
        {
            return null;
        }

        var entry = definition.Schedule.FirstOrDefault(
            value => value.Matches(day, minuteOfDay)
        );
        return entry is null
            ? null
            : new VillageNpcState(
                definition,
                entry.LocationId,
                entry.Position,
                entry.Facing,
                entry.DialogueKey
            );
    }

    private static NpcScheduleEntry Slot(
        int startHour,
        int endHour,
        GridPosition position,
        NpcFacing facing,
        string dialogueKey,
        params int[] weekdayIndices
    ) => new(
        startHour * 60,
        endHour * 60,
        PlayerLocationIds.World,
        position,
        facing,
        dialogueKey,
        weekdayIndices
    );

    private static NpcScheduleEntry ArchiveSlot(
        int startHour,
        int endHour,
        GridPosition position,
        NpcFacing facing,
        string dialogueKey,
        params int[] weekdayIndices
    ) => new(
        startHour * 60,
        endHour * 60,
        PlayerLocationIds.MoonlitArchive,
        position,
        facing,
        dialogueKey,
        weekdayIndices
    );

    private static NpcScheduleEntry WorkshopSlot(
        int startHour,
        int endHour,
        GridPosition position,
        NpcFacing facing,
        string dialogueKey,
        params int[] weekdayIndices
    ) => new(
        startHour * 60,
        endHour * 60,
        PlayerLocationIds.MoonstoneWorkshop,
        position,
        facing,
        dialogueKey,
        weekdayIndices
    );
}

public sealed class VillageSystem
{
    private readonly HashSet<string> _metNpcIds =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, VillageRelationshipSave>
        _relationships = new(StringComparer.Ordinal);

    public IReadOnlySet<string> MetNpcIds => _metNpcIds;
    public const int MaximumRelationshipPoints = 100;

    public event Action? Changed;

    public void Reset()
    {
        _metNpcIds.Clear();
        _relationships.Clear();
        Changed?.Invoke();
    }

    public void Restore(VillageSave? save)
    {
        var normalized = NormalizeSave(save);
        _metNpcIds.Clear();
        _relationships.Clear();
        foreach (var npcId in normalized.MetNpcIds)
        {
            _metNpcIds.Add(npcId);
        }

        foreach (var relationship in normalized.Relationships)
        {
            _relationships[relationship.NpcId] = Clone(relationship);
        }

        Changed?.Invoke();
    }

    public IReadOnlyList<VillageNpcState> CurrentNpcs(
        int day,
        int minuteOfDay,
        string locationId
    ) => VillageCatalog.Npcs.Keys
        .Select(id => VillageCatalog.CurrentNpc(id, day, minuteOfDay))
        .Where(state =>
            state is not null &&
            state.LocationId == locationId
        )
        .Cast<VillageNpcState>()
        .ToList();

    public IReadOnlyList<VillageNpcState> AllCurrentNpcs(
        int day,
        int minuteOfDay
    ) => VillageCatalog.Npcs.Keys
        .Select(id => VillageCatalog.CurrentNpc(id, day, minuteOfDay))
        .Where(state => state is not null)
        .Cast<VillageNpcState>()
        .ToList();

    public VillageNpcState? NpcAt(
        GridPosition position,
        int day,
        int minuteOfDay,
        string locationId
    ) => CurrentNpcs(day, minuteOfDay, locationId)
        .FirstOrDefault(state => state.Position == position);

    public VillageInteractionCheck CheckInteraction(
        GridPosition position,
        int day,
        int minuteOfDay,
        string locationId,
        string selectedItemId
    )
    {
        var state = NpcAt(position, day, minuteOfDay, locationId);
        if (state is null)
        {
            return new VillageInteractionCheck(
                null,
                false,
                false,
                null,
                "notice.nothing_to_interact"
            );
        }

        if (selectedItemId == DataCatalog.HandId)
        {
            return new VillageInteractionCheck(
                state,
                true,
                false,
                null,
                string.Empty
            );
        }

        if (!DataCatalog.Items.TryGetValue(
                selectedItemId,
                out var selectedItem
            ) ||
            selectedItem.Kind == ItemKind.Tool)
        {
            return new VillageInteractionCheck(
                state,
                false,
                false,
                null,
                "notice.needs_hand"
            );
        }

        var relationship = Relationship(state.Definition.Id);
        if (relationship.LastGiftDay == day)
        {
            return new VillageInteractionCheck(
                state,
                false,
                true,
                null,
                "village.gift.already_today"
            );
        }

        return new VillageInteractionCheck(
            state,
            true,
            true,
            GiftReactionFor(state.Definition, selectedItemId),
            string.Empty
        );
    }

    public VillageConversation? Interact(
        GridPosition position,
        int day,
        int minuteOfDay,
        string locationId,
        string selectedItemId,
        Inventory inventory,
        out ActionResult result
    )
    {
        var check = CheckInteraction(
            position,
            day,
            minuteOfDay,
            locationId,
            selectedItemId
        );
        if (!check.IsAvailable || check.Npc is null)
        {
            result = ActionResult.Fail(check.FailureKey);
            return null;
        }

        var state = check.Npc;
        var firstMeeting = _metNpcIds.Add(state.Definition.Id);
        var relationship = Relationship(state.Definition.Id);
        var dialogueKey = state.DialogueKey;
        if (check.IsGift)
        {
            if (!inventory.Remove(selectedItemId, 1))
            {
                result = ActionResult.Fail("village.gift.missing_item");
                return null;
            }

            var reaction = check.GiftReaction ?? GiftReaction.Neutral;
            relationship.Points = Math.Clamp(
                relationship.Points + PointsFor(reaction),
                0,
                MaximumRelationshipPoints
            );
            relationship.LastGiftDay = day;
            dialogueKey = GiftDialogueKey(state.Definition.Id, reaction);
            result = ActionResult.Success(messageKey: "village.gift.given");
        }
        else
        {
            if (relationship.LastTalkDay != day)
            {
                relationship.Points = Math.Clamp(
                    relationship.Points + 2,
                    0,
                    MaximumRelationshipPoints
                );
                relationship.LastTalkDay = day;
            }

            if (firstMeeting)
            {
                dialogueKey = state.Definition.IntroductionKey;
            }
            result = ActionResult.Success(messageKey: "village.talked");
        }

        _relationships[state.Definition.Id] = relationship;
        Changed?.Invoke();
        return new VillageConversation(
            state.Definition.Id,
            state.Definition.NameKey,
            state.Definition.RoleKey,
            dialogueKey,
            firstMeeting,
            check.GiftReaction,
            relationship.Points,
            TierFor(relationship.Points)
        );
    }

    public VillageRelationshipSave Relationship(string npcId)
    {
        if (_relationships.TryGetValue(npcId, out var relationship))
        {
            return Clone(relationship);
        }

        return new VillageRelationshipSave { NpcId = npcId };
    }

    public VillageSave Capture() => new()
    {
        MetNpcIds = _metNpcIds
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList(),
        Relationships = _relationships.Values
            .Where(value => VillageCatalog.Npcs.ContainsKey(value.NpcId))
            .OrderBy(value => value.NpcId, StringComparer.Ordinal)
            .Select(Clone)
            .ToList()
    };

    public static VillageSave NormalizeSave(VillageSave? save)
    {
        var metNpcIds = (save?.MetNpcIds ?? [])
            .Where(VillageCatalog.Npcs.ContainsKey)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        var relationships = new Dictionary<string, VillageRelationshipSave>(
            StringComparer.Ordinal
        );
        foreach (var saved in save?.Relationships ?? [])
        {
            if (!VillageCatalog.Npcs.ContainsKey(saved.NpcId))
            {
                continue;
            }

            relationships[saved.NpcId] = new VillageRelationshipSave
            {
                NpcId = saved.NpcId,
                Points = Math.Clamp(
                    saved.Points,
                    0,
                    MaximumRelationshipPoints
                ),
                LastTalkDay = Math.Max(0, saved.LastTalkDay),
                LastGiftDay = Math.Max(0, saved.LastGiftDay)
            };
        }

        foreach (var npcId in metNpcIds)
        {
            relationships.TryAdd(
                npcId,
                new VillageRelationshipSave { NpcId = npcId }
            );
        }

        return new VillageSave
        {
            MetNpcIds = metNpcIds,
            Relationships = relationships.Values
                .OrderBy(value => value.NpcId, StringComparer.Ordinal)
                .Select(Clone)
                .ToList()
        };
    }

    public static RelationshipTier TierFor(int points)
    {
        if (points >= 60)
        {
            return RelationshipTier.KindredLight;
        }

        return points >= 25
            ? RelationshipTier.TrustedFriend
            : RelationshipTier.NewAcquaintance;
    }

    private static GiftReaction GiftReactionFor(
        VillageNpcDefinition definition,
        string itemId
    )
    {
        var baseItemId = DataCatalog.BaseItemId(itemId);
        if (definition.LovedGiftIds.Contains(
                baseItemId,
                StringComparer.Ordinal
            ) ||
            definition.LovedGiftIds.Contains(
                itemId,
                StringComparer.Ordinal
            ))
        {
            return GiftReaction.Loved;
        }

        var kind = DataCatalog.Item(itemId).Kind;
        if (definition.DislikedGiftKinds.Contains(kind))
        {
            return GiftReaction.Disliked;
        }

        return definition.LikedGiftKinds.Contains(kind)
            ? GiftReaction.Liked
            : GiftReaction.Neutral;
    }

    private static int PointsFor(GiftReaction reaction) => reaction switch
    {
        GiftReaction.Loved => 12,
        GiftReaction.Liked => 7,
        GiftReaction.Neutral => 3,
        GiftReaction.Disliked => 0,
        _ => 0
    };

    private static string GiftDialogueKey(
        string npcId,
        GiftReaction reaction
    ) => $"village.npc.{npcId}.gift.{reaction.ToString().ToLowerInvariant()}";

    private static VillageRelationshipSave Clone(
        VillageRelationshipSave relationship
    ) => new()
    {
        NpcId = relationship.NpcId,
        Points = relationship.Points,
        LastTalkDay = relationship.LastTalkDay,
        LastGiftDay = relationship.LastGiftDay
    };
}
