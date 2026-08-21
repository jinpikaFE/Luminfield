namespace Luminfield.Core;

public enum EnemyAttackKind
{
    Melee,
    Projectile,
    AreaOfEffect
}

public sealed record WeaponDefinition(
    string ItemId,
    int Damage,
    float RangePixels,
    float CooldownSeconds
);

public sealed record EnemyDefinition(
    string Id,
    int MaxHealth,
    int Damage,
    float MovementSpeedPixelsPerSecond,
    float WindupSeconds,
    EnemyAttackKind AttackKind,
    float ProjectileSpeedPixelsPerSecond = 0,
    float AreaRadiusPixels = 0
);

public sealed record StarfallTrialRoomDefinition(
    string Id,
    IReadOnlyList<string> EnemyInstanceIds
);

public sealed record StarfallTrialEnemyDefinition(
    string InstanceId,
    string EnemyId,
    string RoomId,
    GridPosition SpawnCell
);

public sealed record StarfallTrialArtifactDefinition(
    string ItemId,
    GridPosition Cell,
    string RequiredClearedRoomId = ""
);

public sealed record StarfallTrialEnemySnapshot(
    string InstanceId,
    string EnemyId,
    string RoomId,
    float CurrentX,
    float CurrentY,
    int CurrentHealth,
    int MaxHealth
)
{
    public GridPosition Cell => new(
        (int)MathF.Floor(CurrentX / 16),
        (int)MathF.Floor(CurrentY / 16)
    );
    public bool Defeated => CurrentHealth <= 0;
}

public sealed record StarfallEnemyDamageResult(
    bool Succeeded,
    string MessageKey,
    string EnemyInstanceId = "",
    string EnemyId = "",
    int DamageDealt = 0,
    int RemainingHealth = 0,
    bool EnemyDefeated = false,
    string ClearedRoomId = ""
);

public static class StarfallRuinsTrialLayout
{
    public const int Width = 40;
    public const int Height = 22;
    public static readonly GridPosition WorldEntryCell = new(127, 104);
    public static readonly GridPosition WorldReturnCell = new(127, 103);
    public static readonly GridPosition ExitCell = new(20, 20);
    public static readonly GridPosition SafeArrivalCell = new(20, 18);
    public static readonly GridPosition WeaponRackCell = new(17, 17);
    public static readonly GridPosition FirstSealCell = new(8, 11);
    public static readonly GridPosition SecondSealCell = new(15, 7);

    public static bool IsWalkable(GridPosition cell)
    {
        var entrance = cell.X is >= 15 and <= 25 &&
            cell.Y is >= 13 and <= 20;
        var shardCourt = cell.X is >= 3 and <= 12 &&
            cell.Y is >= 13 and <= 20;
        var prismGallery = cell.X is >= 3 and <= 12 &&
            cell.Y is >= 3 and <= 10;
        var sentinelHall = cell.X is >= 25 and <= 37 &&
            cell.Y is >= 3 and <= 10;
        var lowerPassage = cell.X is >= 12 and <= 15 &&
            cell.Y is >= 16 and <= 18;
        var firstSealPassage = cell.X is >= 7 and <= 9 &&
            cell.Y is >= 10 and <= 13;
        var upperPassage = cell.X is >= 12 and <= 25 &&
            cell.Y is >= 6 and <= 8;
        return entrance || shardCourt || prismGallery || sentinelHall ||
            lowerPassage || firstSealPassage || upperPassage;
    }

    public static bool IsSealCell(GridPosition cell) =>
        cell == FirstSealCell || cell == SecondSealCell;

    public static bool IsRoomCell(string roomId, GridPosition cell) =>
        roomId switch
        {
            StarfallRuinsTrialCatalog.ShardCourtRoomId =>
                cell.X is >= 3 and <= 12 && cell.Y is >= 13 and <= 20,
            StarfallRuinsTrialCatalog.PrismGalleryRoomId =>
                cell.X is >= 3 and <= 12 && cell.Y is >= 3 and <= 10,
            StarfallRuinsTrialCatalog.SentinelHallRoomId =>
                cell.X is >= 25 and <= 37 && cell.Y is >= 3 and <= 10,
            _ => false
        };
}

public static class StarfallRuinsTrialCatalog
{
    public const string TrialId = "starfall_ruins_trial";
    public const string ShardCourtRoomId =
        "starfall_trial_room_shard_court";
    public const string PrismGalleryRoomId =
        "starfall_trial_room_prism_gallery";
    public const string SentinelHallRoomId =
        "starfall_trial_room_sentinel_hall";
    public const string TrialClearedMilestoneId =
        "starfall_ruins_trial_cleared";

    public const string ShardlingEnemyId = "enemy_shardling";
    public const string PrismWispEnemyId = "enemy_prism_wisp";
    public const string HollowSentinelEnemyId =
        "enemy_hollow_sentinel";
    public const string MoonshardMiteEnemyId = "enemy_moonshard_mite";
    public const string VeilwingBatEnemyId = "enemy_veilwing_bat";
    public const string StarironBurrowerEnemyId =
        "enemy_stariron_burrower";

    public static WeaponDefinition MoonsteelShortblade { get; } = new(
        DataCatalog.MoonsteelShortbladeId,
        10,
        24,
        0.45f
    );

    public static WeaponDefinition CrystalPike { get; } = new(
        DataCatalog.CrystalPikeId,
        14,
        40,
        0.65f
    );

    public static WeaponDefinition MoonarcBow { get; } = new(
        DataCatalog.MoonarcBowId,
        8,
        112,
        0.8f
    );

    public static IReadOnlyList<WeaponDefinition> Weapons { get; } =
        Array.AsReadOnly(
        [
            MoonsteelShortblade,
            CrystalPike,
            MoonarcBow
        ]);

    public static IReadOnlyList<EnemyDefinition> Enemies { get; } =
        Array.AsReadOnly(
        [
            new EnemyDefinition(
                ShardlingEnemyId,
                20,
                6,
                48,
                0.45f,
                EnemyAttackKind.Melee
            ),
            new EnemyDefinition(
                PrismWispEnemyId,
                20,
                7,
                30,
                0.65f,
                EnemyAttackKind.Projectile,
                ProjectileSpeedPixelsPerSecond: 80
            ),
            new EnemyDefinition(
                HollowSentinelEnemyId,
                50,
                12,
                22,
                0.9f,
                EnemyAttackKind.AreaOfEffect,
                AreaRadiusPixels: 36
            ),
            new EnemyDefinition(
                MoonshardMiteEnemyId,
                26,
                7,
                52,
                0.38f,
                EnemyAttackKind.Melee
            ),
            new EnemyDefinition(
                VeilwingBatEnemyId,
                30,
                8,
                44,
                0.55f,
                EnemyAttackKind.Projectile,
                ProjectileSpeedPixelsPerSecond: 92
            ),
            new EnemyDefinition(
                StarironBurrowerEnemyId,
                62,
                14,
                20,
                1.05f,
                EnemyAttackKind.AreaOfEffect,
                AreaRadiusPixels: 42
            )
        ]);

    public static IReadOnlyList<StarfallTrialEnemyDefinition>
        EnemyInstances { get; } = Array.AsReadOnly(
        [
            Instance("starfall_trial_shardling_01", ShardlingEnemyId,
                ShardCourtRoomId, 6, 16),
            Instance("starfall_trial_shardling_02", ShardlingEnemyId,
                ShardCourtRoomId, 10, 16),
            Instance("starfall_trial_shardling_03", ShardlingEnemyId,
                PrismGalleryRoomId, 6, 6),
            Instance("starfall_trial_prism_wisp_01", PrismWispEnemyId,
                PrismGalleryRoomId, 10, 6),
            Instance("starfall_trial_prism_wisp_02", PrismWispEnemyId,
                SentinelHallRoomId, 27, 7),
            Instance("starfall_trial_hollow_sentinel_01",
                HollowSentinelEnemyId, SentinelHallRoomId, 33, 7)
        ]);

    public static IReadOnlyList<StarfallTrialRoomDefinition> Rooms { get; } =
        Array.AsReadOnly(
        [
            Room(ShardCourtRoomId),
            Room(PrismGalleryRoomId),
            Room(SentinelHallRoomId)
        ]);

    public static IReadOnlyList<StarfallTrialArtifactDefinition>
        Artifacts { get; } = Array.AsReadOnly(
        [
            new StarfallTrialArtifactDefinition(
                DataCatalog.DawnpathCompassId,
                new GridPosition(23, 17)
            ),
            new StarfallTrialArtifactDefinition(
                DataCatalog.TideglassTabletId,
                new GridPosition(5, 14),
                ShardCourtRoomId
            ),
            new StarfallTrialArtifactDefinition(
                DataCatalog.HushedGleambellId,
                new GridPosition(4, 5),
                PrismGalleryRoomId
            ),
            new StarfallTrialArtifactDefinition(
                DataCatalog.StarweaveSpindleId,
                new GridPosition(35, 5),
                SentinelHallRoomId
            )
        ]);

    private static readonly IReadOnlyDictionary<string, EnemyDefinition>
        EnemiesById = Enemies.ToDictionary(
            definition => definition.Id,
            StringComparer.Ordinal
        );
    private static readonly IReadOnlyDictionary<string, WeaponDefinition>
        WeaponsByItemId = Weapons.ToDictionary(
            definition => definition.ItemId,
            StringComparer.Ordinal
        );
    private static readonly IReadOnlyDictionary<string,
        StarfallTrialEnemyDefinition> InstancesById =
        EnemyInstances.ToDictionary(
            definition => definition.InstanceId,
            StringComparer.Ordinal
        );
    private static readonly IReadOnlyDictionary<GridPosition,
        StarfallTrialEnemyDefinition> InstancesByCell =
        EnemyInstances.ToDictionary(definition => definition.SpawnCell);
    private static readonly IReadOnlyDictionary<string,
        StarfallTrialRoomDefinition> RoomsById = Rooms.ToDictionary(
            definition => definition.Id,
            StringComparer.Ordinal
        );
    private static readonly IReadOnlyDictionary<string,
        StarfallTrialArtifactDefinition> ArtifactsById =
        Artifacts.ToDictionary(
            definition => definition.ItemId,
            StringComparer.Ordinal
        );
    private static readonly IReadOnlyDictionary<GridPosition,
        StarfallTrialArtifactDefinition> ArtifactsByCell =
        Artifacts.ToDictionary(definition => definition.Cell);

    public static EnemyDefinition Enemy(string enemyId) =>
        EnemiesById.TryGetValue(enemyId, out var definition)
            ? definition
            : throw new KeyNotFoundException(
                $"Unknown enemy id '{enemyId}'."
            );

    public static bool TryWeapon(
        string? itemId,
        out WeaponDefinition weapon
    ) => WeaponsByItemId.TryGetValue(itemId ?? string.Empty, out weapon!);

    public static WeaponDefinition Weapon(string itemId) =>
        WeaponsByItemId.TryGetValue(itemId, out var weapon)
            ? weapon
            : throw new KeyNotFoundException(
                $"Unknown weapon item id '{itemId}'."
            );

    public static bool TryEnemyInstance(
        string? instanceId,
        out StarfallTrialEnemyDefinition definition
    ) => InstancesById.TryGetValue(instanceId ?? string.Empty, out definition!);

    public static bool TryEnemyAt(
        GridPosition cell,
        out StarfallTrialEnemyDefinition definition
    ) => InstancesByCell.TryGetValue(cell, out definition!);

    public static bool TryRoom(
        string? roomId,
        out StarfallTrialRoomDefinition definition
    ) => RoomsById.TryGetValue(roomId ?? string.Empty, out definition!);

    public static bool TryArtifact(
        string? itemId,
        out StarfallTrialArtifactDefinition definition
    ) => ArtifactsById.TryGetValue(itemId ?? string.Empty, out definition!);

    public static bool TryArtifactAt(
        GridPosition cell,
        out StarfallTrialArtifactDefinition definition
    ) => ArtifactsByCell.TryGetValue(cell, out definition!);

    public static string RequiredRoomForSeal(GridPosition cell) =>
        cell == StarfallRuinsTrialLayout.FirstSealCell
            ? ShardCourtRoomId
            : cell == StarfallRuinsTrialLayout.SecondSealCell
                ? PrismGalleryRoomId
                : string.Empty;

    private static StarfallTrialEnemyDefinition Instance(
        string instanceId,
        string enemyId,
        string roomId,
        int x,
        int y
    ) => new(instanceId, enemyId, roomId, new GridPosition(x, y));

    private static StarfallTrialRoomDefinition Room(string roomId) => new(
        roomId,
        EnemyInstances
            .Where(instance => instance.RoomId == roomId)
            .Select(instance => instance.InstanceId)
            .ToArray()
    );
}

public sealed class StarfallRuinsTrialSystem
{
    private readonly HashSet<string> _clearedRoomIds =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> _recoveredArtifactIds =
        new(StringComparer.Ordinal);
    private bool _weaponClaimed;
    private readonly Dictionary<string, int> _enemyHealth =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, (float X, float Y)> _enemyPositions =
        new(StringComparer.Ordinal);

    public IReadOnlySet<string> ClearedRoomIds => _clearedRoomIds;
    public IReadOnlySet<string> RecoveredArtifactIds =>
        _recoveredArtifactIds;
    public bool WeaponClaimed => _weaponClaimed;
    public bool Cleared => StarfallRuinsTrialCatalog.Rooms.All(room =>
        _clearedRoomIds.Contains(room.Id)
    );

    public event Action? Changed;

    public StarfallRuinsTrialSystem() => ResetEnemyHealth();

    public void Reset()
    {
        _clearedRoomIds.Clear();
        _recoveredArtifactIds.Clear();
        _weaponClaimed = false;
        ResetEnemyHealth();
        Changed?.Invoke();
    }

    public void Restore(StarfallRuinsTrialSave? save)
    {
        var normalized = NormalizeSave(save);
        _clearedRoomIds.Clear();
        _clearedRoomIds.UnionWith(normalized.ClearedRoomIds);
        _recoveredArtifactIds.Clear();
        _recoveredArtifactIds.UnionWith(
            normalized.RecoveredArtifactIds
        );
        _weaponClaimed = normalized.WeaponClaimed;
        ResetEnemyHealth();
        Changed?.Invoke();
    }

    public void ResetUnclearedRooms()
    {
        var changed = false;
        foreach (var instance in StarfallRuinsTrialCatalog.EnemyInstances)
        {
            var target = _clearedRoomIds.Contains(instance.RoomId)
                ? 0
                : StarfallRuinsTrialCatalog.Enemy(instance.EnemyId).MaxHealth;
            if (_enemyHealth[instance.InstanceId] == target)
            {
                var current = _enemyPositions[instance.InstanceId];
                var spawnX = instance.SpawnCell.X * 16 + 8;
                var spawnY = instance.SpawnCell.Y * 16 + 8;
                if (current.X == spawnX && current.Y == spawnY)
                {
                    continue;
                }
            }

            _enemyHealth[instance.InstanceId] = target;
            _enemyPositions[instance.InstanceId] = (
                instance.SpawnCell.X * 16 + 8,
                instance.SpawnCell.Y * 16 + 8
            );
            changed = true;
        }

        if (changed)
        {
            Changed?.Invoke();
        }
    }

    public bool IsRoomCleared(string roomId) =>
        _clearedRoomIds.Contains(roomId);

    public bool IsSealOpen(GridPosition cell)
    {
        var requiredRoomId =
            StarfallRuinsTrialCatalog.RequiredRoomForSeal(cell);
        return string.IsNullOrEmpty(requiredRoomId) ||
            IsRoomCleared(requiredRoomId);
    }

    public bool IsCellAccessible(GridPosition cell)
        => IsCellAccessible(cell, _clearedRoomIds);

    public bool IsCellPassable(GridPosition cell)
    {
        if (!IsCellAccessible(cell) ||
            (StarfallRuinsTrialLayout.IsSealCell(cell) &&
                !IsSealOpen(cell)) ||
            cell == StarfallRuinsTrialLayout.WeaponRackCell ||
            EnemyAt(cell) is not null)
        {
            return false;
        }

        return !StarfallRuinsTrialCatalog.TryArtifactAt(
                cell,
                out var artifact
            ) || _recoveredArtifactIds.Contains(artifact.ItemId);
    }

    public static bool IsCellAccessible(
        StarfallRuinsTrialSave? save,
        GridPosition cell
    ) => IsCellAccessible(
        cell,
        NormalizeSave(save).ClearedRoomIds.ToHashSet(StringComparer.Ordinal)
    );

    private static bool IsCellAccessible(
        GridPosition cell,
        IReadOnlySet<string> clearedRoomIds
    )
    {
        if (!StarfallRuinsTrialLayout.IsWalkable(cell))
        {
            return false;
        }

        if (cell.Y <= 10 && cell.X <= 12)
        {
            return clearedRoomIds.Contains(
                StarfallRuinsTrialCatalog.ShardCourtRoomId
            );
        }

        if (cell.Y <= 10 && cell.X >= 15)
        {
            return clearedRoomIds.Contains(
                    StarfallRuinsTrialCatalog.ShardCourtRoomId
                ) && clearedRoomIds.Contains(
                    StarfallRuinsTrialCatalog.PrismGalleryRoomId
                );
        }

        return true;
    }

    public StarfallTrialEnemySnapshot? EnemyAt(GridPosition cell)
    {
        return StarfallRuinsTrialCatalog.EnemyInstances
            .Select(instance => Enemy(instance.InstanceId))
            .FirstOrDefault(enemy => !enemy.Defeated && enemy.Cell == cell);
    }

    public StarfallTrialEnemySnapshot Enemy(string instanceId)
    {
        if (!StarfallRuinsTrialCatalog.TryEnemyInstance(
                instanceId,
                out var instance
            ))
        {
            throw new KeyNotFoundException(
                $"Unknown trial enemy instance '{instanceId}'."
            );
        }

        var definition = StarfallRuinsTrialCatalog.Enemy(instance.EnemyId);
        var position = _enemyPositions[instance.InstanceId];
        return new StarfallTrialEnemySnapshot(
            instance.InstanceId,
            instance.EnemyId,
            instance.RoomId,
            position.X,
            position.Y,
            _enemyHealth[instance.InstanceId],
            definition.MaxHealth
        );
    }

    public IReadOnlyList<StarfallTrialEnemySnapshot> Enemies() =>
        StarfallRuinsTrialCatalog.EnemyInstances
            .Select(instance => Enemy(instance.InstanceId))
            .ToArray();

    public ActionResult CheckDamageEnemy(
        string instanceId,
        GridPosition target
    )
    {
        if (!StarfallRuinsTrialCatalog.TryEnemyInstance(
                instanceId,
                out var instance
            ))
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        var enemy = Enemy(instanceId);
        if (enemy.Cell != target)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        return enemy.CurrentHealth > 0
            ? ActionResult.Success(messageKey: "combat.attack.ready")
            : ActionResult.Fail("combat.enemy_defeated");
    }

    public ActionResult CheckMoveEnemy(
        string instanceId,
        float x,
        float y
    )
    {
        if (!StarfallRuinsTrialCatalog.TryEnemyInstance(
                instanceId,
                out var instance
            ) || Enemy(instanceId).Defeated ||
            !float.IsFinite(x) || !float.IsFinite(y))
        {
            return ActionResult.Fail("combat.enemy_move.invalid");
        }

        var cell = new GridPosition(
            (int)MathF.Floor(x / 16),
            (int)MathF.Floor(y / 16)
        );
        if (!StarfallRuinsTrialLayout.IsRoomCell(instance.RoomId, cell))
        {
            return ActionResult.Fail("combat.enemy_move.blocked");
        }

        if (StarfallRuinsTrialLayout.IsSealCell(cell) ||
            cell == StarfallRuinsTrialLayout.WeaponRackCell ||
            StarfallRuinsTrialCatalog.TryArtifactAt(cell, out _))
        {
            return ActionResult.Fail("combat.enemy_move.blocked");
        }

        var occupied = StarfallRuinsTrialCatalog.EnemyInstances
            .Where(candidate => candidate.InstanceId != instanceId)
            .Select(candidate => Enemy(candidate.InstanceId))
            .Any(enemy => !enemy.Defeated && enemy.Cell == cell);
        return occupied
            ? ActionResult.Fail("combat.enemy_move.blocked")
            : ActionResult.Success(messageKey: "combat.enemy_move.ready");
    }

    public ActionResult MoveEnemyChecked(
        string instanceId,
        float x,
        float y
    )
    {
        var check = CheckMoveEnemy(instanceId, x, y);
        if (!check.Succeeded)
        {
            return check;
        }

        _enemyPositions[instanceId] = (x, y);
        return ActionResult.Success(messageKey: "combat.enemy_move.moved");
    }

    public StarfallEnemyDamageResult ApplyDamageChecked(
        string instanceId,
        int damage
    )
    {
        var instance = StarfallRuinsTrialCatalog.EnemyInstances.Single(
            candidate => candidate.InstanceId == instanceId
        );
        var previous = _enemyHealth[instanceId];
        if (previous <= 0 || damage <= 0)
        {
            throw new InvalidOperationException(
                "Enemy damage must be checked before it is applied."
            );
        }

        var remaining = Math.Max(0, previous - damage);
        _enemyHealth[instanceId] = remaining;
        var defeated = remaining == 0;
        var clearedRoomId = string.Empty;
        if (defeated && StarfallRuinsTrialCatalog.Rooms
            .Single(room => room.Id == instance.RoomId)
            .EnemyInstanceIds.All(id => _enemyHealth[id] <= 0) &&
            _clearedRoomIds.Add(instance.RoomId))
        {
            clearedRoomId = instance.RoomId;
        }

        Changed?.Invoke();
        return new StarfallEnemyDamageResult(
            true,
            defeated ? "combat.enemy_defeated" : "combat.enemy_hit",
            instance.InstanceId,
            instance.EnemyId,
            Math.Min(damage, previous),
            remaining,
            defeated,
            clearedRoomId
        );
    }

    public ActionResult CheckRecoverArtifact(
        GridPosition playerCell,
        GridPosition target,
        string selectedItemId,
        Inventory inventory
    )
    {
        if (!StarfallRuinsTrialCatalog.TryArtifactAt(
                target,
                out var artifact
            ) || Distance(playerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (_recoveredArtifactIds.Contains(artifact.ItemId))
        {
            return ActionResult.Fail("ruins.artifact.already_recovered");
        }

        if (!string.IsNullOrEmpty(artifact.RequiredClearedRoomId) &&
            !IsRoomCleared(artifact.RequiredClearedRoomId))
        {
            return ActionResult.Fail("ruins.artifact.room_not_cleared");
        }

        if (selectedItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        return inventory.CanAdd(artifact.ItemId, 1)
            ? ActionResult.Success(messageKey: "ruins.artifact.ready")
            : ActionResult.Fail("notice.inventory_full");
    }

    public ActionResult RecoverArtifactChecked(
        string itemId,
        Inventory inventory
    )
    {
        if (!StarfallRuinsTrialCatalog.TryArtifact(itemId, out _) ||
            _recoveredArtifactIds.Contains(itemId) ||
            !inventory.Add(itemId, 1))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        _recoveredArtifactIds.Add(itemId);
        Changed?.Invoke();
        return ActionResult.Grant(
            itemId,
            1,
            0,
            "ruins.artifact.recovered"
        );
    }

    public ActionResult CheckRecoverWeapon(
        GridPosition playerCell,
        GridPosition target,
        string selectedItemId,
        Inventory inventory
    )
    {
        if (target != StarfallRuinsTrialLayout.WeaponRackCell ||
            Distance(playerCell, target) != 1)
        {
            return ActionResult.Fail("notice.nothing_to_interact");
        }

        if (selectedItemId != DataCatalog.HandId)
        {
            return ActionResult.Fail("notice.needs_hand");
        }

        if (_weaponClaimed ||
            inventory.Count(DataCatalog.MoonsteelShortbladeId) > 0)
        {
            return ActionResult.Fail("ruins.weapon.already_recovered");
        }

        return inventory.CanAdd(DataCatalog.MoonsteelShortbladeId, 1)
            ? ActionResult.Success(messageKey: "ruins.weapon.ready")
            : ActionResult.Fail("notice.inventory_full");
    }

    public ActionResult RecoverWeaponChecked(Inventory inventory)
    {
        if (!inventory.Add(DataCatalog.MoonsteelShortbladeId, 1))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        inventory.PromoteToHotbar(DataCatalog.MoonsteelShortbladeId);
        _weaponClaimed = true;
        Changed?.Invoke();
        return ActionResult.Grant(
            DataCatalog.MoonsteelShortbladeId,
            1,
            0,
            "ruins.weapon.recovered"
        );
    }

    public IReadOnlySet<string> CompletedMilestoneIds() => Cleared
        ? new HashSet<string>(StringComparer.Ordinal)
        {
            StarfallRuinsTrialCatalog.TrialClearedMilestoneId
        }
        : new HashSet<string>(StringComparer.Ordinal);

    public StarfallRuinsTrialSave Capture() => new()
    {
        WeaponClaimed = _weaponClaimed,
        ClearedRoomIds = StarfallRuinsTrialCatalog.Rooms
            .Select(room => room.Id)
            .Where(_clearedRoomIds.Contains)
            .ToList(),
        RecoveredArtifactIds = StarfallRuinsTrialCatalog.Artifacts
            .Select(artifact => artifact.ItemId)
            .Where(_recoveredArtifactIds.Contains)
            .ToList()
    };

    public static StarfallRuinsTrialSave NormalizeSave(
        StarfallRuinsTrialSave? save
    )
    {
        var requestedRooms = (save?.ClearedRoomIds ?? [])
            .Where(id => StarfallRuinsTrialCatalog.TryRoom(id, out _))
            .ToHashSet(StringComparer.Ordinal);
        var clearedRooms = new List<string>();
        foreach (var room in StarfallRuinsTrialCatalog.Rooms)
        {
            if (!requestedRooms.Contains(room.Id))
            {
                break;
            }
            clearedRooms.Add(room.Id);
        }

        return new StarfallRuinsTrialSave
        {
            WeaponClaimed = save?.WeaponClaimed ?? false,
            ClearedRoomIds = clearedRooms,
            RecoveredArtifactIds = StarfallRuinsTrialCatalog.Artifacts
                .Select(artifact => artifact.ItemId)
                .Where((save?.RecoveredArtifactIds ?? [])
                    .Contains)
                .Distinct(StringComparer.Ordinal)
                .ToList()
        };
    }

    public void EnsureWeaponClaimed()
    {
        if (_weaponClaimed)
        {
            return;
        }

        _weaponClaimed = true;
        Changed?.Invoke();
    }

    public static IEnumerable<string> EvidenceEntryIds(
        StarfallRuinsTrialSave? save
    )
    {
        var normalized = NormalizeSave(save);
        var cleared = normalized.ClearedRoomIds.ToHashSet(
            StringComparer.Ordinal
        );
        foreach (var enemyId in StarfallRuinsTrialCatalog.EnemyInstances
                     .Where(instance => cleared.Contains(instance.RoomId))
                     .Select(instance => instance.EnemyId)
                     .Distinct(StringComparer.Ordinal))
        {
            yield return enemyId;
        }

        foreach (var artifactId in normalized.RecoveredArtifactIds)
        {
            yield return artifactId;
        }
    }

    public static IReadOnlySet<string> CompletedMilestoneIds(
        StarfallRuinsTrialSave? save
    ) => NormalizeSave(save).ClearedRoomIds.Count ==
        StarfallRuinsTrialCatalog.Rooms.Count
            ? new HashSet<string>(StringComparer.Ordinal)
            {
                StarfallRuinsTrialCatalog.TrialClearedMilestoneId
            }
            : new HashSet<string>(StringComparer.Ordinal);

    private void ResetEnemyHealth()
    {
        _enemyHealth.Clear();
        _enemyPositions.Clear();
        foreach (var instance in StarfallRuinsTrialCatalog.EnemyInstances)
        {
            _enemyHealth[instance.InstanceId] =
                _clearedRoomIds.Contains(instance.RoomId)
                    ? 0
                    : StarfallRuinsTrialCatalog.Enemy(instance.EnemyId)
                        .MaxHealth;
            _enemyPositions[instance.InstanceId] = (
                instance.SpawnCell.X * 16 + 8,
                instance.SpawnCell.Y * 16 + 8
            );
        }
    }

    private static int Distance(GridPosition left, GridPosition right) =>
        Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);
}
