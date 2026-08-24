namespace Luminfield.Core;

public sealed record MineralDefinition(
    string ItemId,
    int SellPrice,
    string RequiredToolTierId,
    int EnergyCost,
    IReadOnlyList<int> RoomNumbers
);

public sealed record MiningVeinDefinition(
    string Id,
    string MineralItemId,
    int RoomNumber,
    GridPosition Cell
);

public static class CrystalGrottoSurveyLayout
{
    public const int RoomCount = 5;
    public const int Width = 40;
    public const int Height = 22;
    public static readonly GridPosition WorldEntryCell = new(74, 142);
    public static readonly GridPosition WorldReturnCell = new(74, 143);
    public static readonly GridPosition ExitCell = new(20, 20);
    public static readonly GridPosition SafeArrivalCell = new(20, 18);
    public static readonly GridPosition UpgradeBenchCell = new(17, 16);
    public static readonly GridPosition SealCell = new(7, 11);
    public static readonly GridPosition DepthAnchorCell = new(35, 7);

    public static bool IsWalkable(GridPosition cell)
    {
        var roomOne = cell.X is >= 15 and <= 25 &&
            cell.Y is >= 13 and <= 20;
        var roomTwo = cell.X is >= 3 and <= 12 &&
            cell.Y is >= 13 and <= 20;
        var roomThree = cell.X is >= 3 and <= 12 &&
            cell.Y is >= 3 and <= 10;
        var roomFour = cell.X is >= 15 and <= 25 &&
            cell.Y is >= 3 and <= 10;
        var roomFive = cell.X is >= 28 and <= 37 &&
            cell.Y is >= 3 and <= 10;
        var lowerPassage = cell.Y is >= 17 and <= 18 &&
            cell.X is >= 12 and <= 15;
        var leftPassage = cell.X is >= 6 and <= 8 &&
            cell.Y is >= 10 and <= 13;
        var upperMiddlePassage = cell.Y is >= 6 and <= 8 &&
            cell.X is >= 12 and <= 15;
        var upperRightPassage = cell.Y is >= 6 and <= 8 &&
            cell.X is >= 25 and <= 28;
        return roomOne || roomTwo || roomThree || roomFour || roomFive ||
            lowerPassage || leftPassage || upperMiddlePassage ||
            upperRightPassage;
    }

    public static int RoomNumberAt(GridPosition cell)
    {
        if (!IsWalkable(cell))
        {
            return 0;
        }

        if (cell.X is >= 15 and <= 25 && cell.Y >= 13)
        {
            return 1;
        }

        if (cell.X <= 12 && cell.Y >= 11)
        {
            return 2;
        }

        if (cell.X <= 12)
        {
            return 3;
        }

        if (cell.X <= 27)
        {
            return 4;
        }

        return 5;
    }
}

public static class MiningCatalog
{
    public const string CrystalGrottoSurveyId = "crystal_grotto_survey";
    public const string CrystalGrottoFifthRoomAnchorId =
        "mine_anchor_crystal_grotto_5";

    public static IReadOnlyList<MineralDefinition> Minerals { get; } =
        Array.AsReadOnly(
        [
            new MineralDefinition(
                DataCatalog.LumenSlateOreId,
                32,
                ToolProgressionCatalog.BasicTierId,
                4,
                Array.AsReadOnly([1, 2])
            ),
            new MineralDefinition(
                DataCatalog.MoonveinOreId,
                48,
                ToolProgressionCatalog.BasicTierId,
                4,
                Array.AsReadOnly([2])
            ),
            new MineralDefinition(
                DataCatalog.PrismheartOreId,
                72,
                ToolProgressionCatalog.BronzeStarTierId,
                5,
                Array.AsReadOnly([3, 4])
            ),
            new MineralDefinition(
                DataCatalog.StarironOreId,
                96,
                ToolProgressionCatalog.BronzeStarTierId,
                6,
                Array.AsReadOnly([4, 5])
            )
        ]);

    public static IReadOnlyList<MiningVeinDefinition> Veins { get; } =
        Array.AsReadOnly(
        [
            Vein("lumen_slate_r1_01", DataCatalog.LumenSlateOreId, 1, 23, 16),
            Vein("lumen_slate_r1_02", DataCatalog.LumenSlateOreId, 1, 22, 15),
            Vein("lumen_slate_r1_03", DataCatalog.LumenSlateOreId, 1, 24, 15),
            Vein("lumen_slate_r2_01", DataCatalog.LumenSlateOreId, 2, 6, 16),
            Vein("lumen_slate_r2_02", DataCatalog.LumenSlateOreId, 2, 5, 15),
            Vein("lumen_slate_r2_03", DataCatalog.LumenSlateOreId, 2, 7, 15),
            Vein("moonvein_r2_01", DataCatalog.MoonveinOreId, 2, 5, 18),
            Vein("moonvein_r2_02", DataCatalog.MoonveinOreId, 2, 7, 18),
            Vein("moonvein_r2_03", DataCatalog.MoonveinOreId, 2, 9, 16),
            Vein("prismheart_r3_01", DataCatalog.PrismheartOreId, 3, 7, 6),
            Vein("prismheart_r3_02", DataCatalog.PrismheartOreId, 3, 9, 6),
            Vein("prismheart_r4_01", DataCatalog.PrismheartOreId, 4, 20, 6),
            Vein("prismheart_r4_02", DataCatalog.PrismheartOreId, 4, 18, 6),
            Vein("stariron_r4_01", DataCatalog.StarironOreId, 4, 22, 6),
            Vein("stariron_r4_02", DataCatalog.StarironOreId, 4, 20, 8),
            Vein("stariron_r5_01", DataCatalog.StarironOreId, 5, 31, 6),
            Vein("stariron_r5_02", DataCatalog.StarironOreId, 5, 33, 6)
        ]);

    private static readonly IReadOnlyDictionary<string, MineralDefinition>
        MineralsByItemId = Minerals.ToDictionary(
            mineral => mineral.ItemId,
            StringComparer.Ordinal
        );

    private static readonly IReadOnlyDictionary<GridPosition,
        MiningVeinDefinition> VeinsByCell = Veins.ToDictionary(
            vein => vein.Cell
        );

    private static readonly IReadOnlyDictionary<string, MiningVeinDefinition>
        VeinsById = Veins.ToDictionary(vein => vein.Id, StringComparer.Ordinal);

    public static MineralDefinition Mineral(string itemId) =>
        MineralsByItemId.TryGetValue(itemId, out var mineral)
            ? mineral
            : throw new KeyNotFoundException(
                $"Unknown mineral item id '{itemId}'."
            );

    public static bool TryVeinAt(
        GridPosition cell,
        out MiningVeinDefinition vein
    ) => VeinsByCell.TryGetValue(cell, out vein!);

    public static bool TryVein(
        string? veinId,
        out MiningVeinDefinition vein
    ) => VeinsById.TryGetValue(veinId ?? string.Empty, out vein!);

    private static MiningVeinDefinition Vein(
        string suffix,
        string itemId,
        int room,
        int x,
        int y
    ) => new(
        $"crystal_grotto_{suffix}",
        itemId,
        room,
        new GridPosition(x, y)
    );
}

public sealed class MiningSystem
{
    private readonly HashSet<string> _depletedVeinIds =
        new(StringComparer.Ordinal);

    public int DeepestRoomReached { get; private set; }
    public bool FifthRoomAnchorReached =>
        DeepestRoomReached >= CrystalGrottoSurveyLayout.RoomCount;

    public event Action<GridPosition>? Changed;

    public void Reset()
    {
        _depletedVeinIds.Clear();
        DeepestRoomReached = 0;
    }

    public void Restore(MiningSave? save)
    {
        var normalized = NormalizeSave(save);
        _depletedVeinIds.Clear();
        _depletedVeinIds.UnionWith(normalized.DepletedVeinIds);
        DeepestRoomReached = normalized.DeepestRoomReached;
    }

    public bool IsDepleted(string veinId) =>
        _depletedVeinIds.Contains(veinId);

    public bool ReachRoom(int roomNumber)
    {
        var room = Math.Clamp(
            roomNumber,
            0,
            CrystalGrottoSurveyLayout.RoomCount
        );
        if (room <= DeepestRoomReached)
        {
            return false;
        }

        DeepestRoomReached = room;
        return true;
    }

    public ActionResult CheckMineVein(
        string locationId,
        GridPosition playerCell,
        GridPosition target,
        string toolId,
        string toolTierId,
        int availableEnergy,
        Inventory inventory
    )
    {
        if (locationId != MiningCatalog.CrystalGrottoSurveyId ||
            !MiningCatalog.TryVeinAt(target, out var vein) ||
            Distance(playerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (IsDepleted(vein.Id))
        {
            return ActionResult.Fail("mining.vein_depleted");
        }

        if (toolId != DataCatalog.ShovelId)
        {
            return ActionResult.Fail("notice.needs_shovel");
        }

        var mineral = MiningCatalog.Mineral(vein.MineralItemId);
        if (!ToolProgressionCatalog.TryTier(toolTierId, out var tier) ||
            tier.Rank < ToolProgressionCatalog.Tier(
                mineral.RequiredToolTierId
            ).Rank)
        {
            return ActionResult.Fail("mining.requires_bronze_star_shovel");
        }

        if (availableEnergy < mineral.EnergyCost)
        {
            return ActionResult.Fail("notice.no_energy");
        }

        if (!inventory.CanAdd(mineral.ItemId, 1))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        return ActionResult.Success(
            mineral.EnergyCost,
            "mining.ready"
        );
    }

    public ActionResult TryMineVein(
        string locationId,
        GridPosition playerCell,
        GridPosition target,
        string toolId,
        string toolTierId,
        int availableEnergy,
        Inventory inventory
    )
    {
        var check = CheckMineVein(
            locationId,
            playerCell,
            target,
            toolId,
            toolTierId,
            availableEnergy,
            inventory
        );
        if (!check.Succeeded)
        {
            return check;
        }

        var vein = MiningCatalog.Veins.Single(value => value.Cell == target);
        if (!inventory.Add(vein.MineralItemId, 1))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        _depletedVeinIds.Add(vein.Id);
        Changed?.Invoke(target);
        return ActionResult.Grant(
            vein.MineralItemId,
            1,
            check.EnergyCost,
            "mining.gathered_mineral"
        );
    }

    public IReadOnlySet<string> CompletedMilestoneIds() =>
        FifthRoomAnchorReached
            ? new HashSet<string>(StringComparer.Ordinal)
            {
                MiningCatalog.CrystalGrottoFifthRoomAnchorId
            }
            : new HashSet<string>(StringComparer.Ordinal);

    public MiningSave Capture() => new()
    {
        DepletedVeinIds = MiningCatalog.Veins
            .Select(vein => vein.Id)
            .Where(_depletedVeinIds.Contains)
            .ToList(),
        DeepestRoomReached = DeepestRoomReached
    };

    public static MiningSave NormalizeSave(MiningSave? save)
    {
        var expeditionRoom = Math.Clamp(
            save?.ExpeditionRoom ?? 0,
            0,
            DeepMineCatalog.MaximumRoom
        );
        var active = save?.ExpeditionActive == true && expeditionRoom > 0;
        return new MiningSave
        {
            DepletedVeinIds = (save?.DepletedVeinIds ?? [])
                .Where(id => MiningCatalog.TryVein(id, out _))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList(),
            DeepestRoomReached = Math.Clamp(
                save?.DeepestRoomReached ?? 0,
                0,
                CrystalGrottoSurveyLayout.RoomCount
            ),
            ExpeditionSeed = Math.Max(0, save?.ExpeditionSeed ?? 0),
            ExpeditionActive = active,
            ExpeditionRoom = active ? expeditionRoom : 0,
            ExpeditionEnemyHealth = active
                ? Math.Max(0, save?.ExpeditionEnemyHealth ?? 0)
                : 0,
            DeepestExpeditionRoom = Math.Clamp(
                save?.DeepestExpeditionRoom ?? 0,
                0,
                DeepMineCatalog.MaximumRoom
            ),
            StableAnchorRoom = NormalizeAnchor(
                save?.StableAnchorRoom ?? 0
            ),
            ClearedExpeditionRooms = NormalizeRooms(
                save?.ClearedExpeditionRooms
            ),
            ExcavatedExpeditionRooms = NormalizeRooms(
                save?.ExcavatedExpeditionRooms
            ),
            ClaimedExpeditionWeaponIds =
                (save?.ClaimedExpeditionWeaponIds ?? [])
                .Where(id => StarfallRuinsTrialCatalog.TryWeapon(id, out _))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList(),
            CrystalMiningSkill = NormalizeAdventureSkill(
                save?.CrystalMiningSkill,
                AdventureSkillKind.CrystalMining
            ),
            NightwatchSkill = NormalizeAdventureSkill(
                save?.NightwatchSkill,
                AdventureSkillKind.Nightwatch
            )
        };
    }

    public static IReadOnlySet<string> CompletedMilestoneIds(
        MiningSave? save
    ) => NormalizeSave(save).DeepestRoomReached >=
        CrystalGrottoSurveyLayout.RoomCount
            ? new HashSet<string>(StringComparer.Ordinal)
            {
                MiningCatalog.CrystalGrottoFifthRoomAnchorId
            }
            : new HashSet<string>(StringComparer.Ordinal);

    public static IEnumerable<string> EvidenceItemIds(MiningSave? save)
    {
        var normalized = NormalizeSave(save);
        foreach (var veinId in normalized.DepletedVeinIds)
        {
            if (MiningCatalog.TryVein(veinId, out var vein))
            {
                yield return vein.MineralItemId;
            }
        }
    }

    private static int Distance(GridPosition left, GridPosition right) =>
        Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);

    private static int NormalizeAnchor(int room)
    {
        var normalized = Math.Clamp(room, 0, DeepMineCatalog.MaximumRoom);
        return normalized - normalized % DeepMineCatalog.AnchorInterval;
    }

    private static List<int> NormalizeRooms(IEnumerable<int>? rooms) =>
        (rooms ?? [])
        .Where(room => room is >= 1 and <= DeepMineCatalog.MaximumRoom)
        .Distinct()
        .Order()
        .ToList();

    private static AdventureSkillSave NormalizeAdventureSkill(
        AdventureSkillSave? save,
        AdventureSkillKind kind
    )
    {
        var experience = Math.Clamp(save?.Experience ?? 0, 0, 999999);
        var level = AdventureSkillProgression.LevelFor(experience);
        var specializationId = save?.SpecializationId ?? string.Empty;
        if (level < 3 || !AdventureSkillCatalog.IsSpecialization(
                kind,
                specializationId
            ))
        {
            specializationId = string.Empty;
        }
        return new AdventureSkillSave
        {
            Experience = experience,
            Level = level,
            SpecializationId = specializationId
        };
    }
}
