namespace Luminfield.Core;

public enum DeepMineRoomKind
{
    CrystalHall,
    SplitCavern,
    SunkenVault,
    AnchorSanctum
}

public sealed record DeepMineRoomDefinition(
    int Number,
    string Id,
    DeepMineRoomKind Kind,
    string EnemyId,
    string MineralItemId,
    int RequiredToolRank,
    string RewardWeaponItemId
);

public sealed record DeepMineSnapshot(
    bool Active,
    int Seed,
    int Room,
    int DeepestRoom,
    int StableAnchorRoom,
    string EnemyId,
    int EnemyHealth,
    int EnemyMaxHealth,
    bool EnemyDefeated,
    bool RoomExcavated,
    string MineralItemId,
    string RewardWeaponItemId
);

public sealed record DeepMineAttackResult(
    bool Succeeded,
    string MessageKey,
    int DamageDealt = 0,
    int EnemyHealth = 0,
    bool EnemyDefeated = false,
    int DamageTaken = 0,
    bool PlayerDefeated = false,
    string DropItemId = "",
    string RewardWeaponItemId = ""
);

public enum AdventureSkillKind
{
    CrystalMining,
    Nightwatch
}

public static class AdventureSkillCatalog
{
    public const string ForgekeeperSpecializationId =
        "mining_specialization_forgekeeper";
    public const string GemseekerSpecializationId =
        "mining_specialization_gemseeker";
    public const string GuardianSpecializationId =
        "nightwatch_specialization_guardian";
    public const string SpellbladeSpecializationId =
        "nightwatch_specialization_spellblade";

    public static IReadOnlyList<int> LevelThresholds { get; } =
        [0, 30, 85, 165, 280, 430];

    public static bool IsSpecialization(
        AdventureSkillKind kind,
        string specializationId
    ) => kind switch
    {
        AdventureSkillKind.CrystalMining => specializationId is
            ForgekeeperSpecializationId or GemseekerSpecializationId,
        AdventureSkillKind.Nightwatch => specializationId is
            GuardianSpecializationId or SpellbladeSpecializationId,
        _ => false
    };
}

public sealed class AdventureSkillProgression
{
    public AdventureSkillProgression(AdventureSkillKind kind)
    {
        Kind = kind;
    }

    public AdventureSkillKind Kind { get; }
    public int Experience { get; private set; }
    public int Level { get; private set; }
    public string SpecializationId { get; private set; } = string.Empty;
    public bool CanChooseSpecialization =>
        Level >= 3 && string.IsNullOrWhiteSpace(SpecializationId);
    public event Action? Changed;

    public void Reset()
    {
        Experience = 0;
        Level = 0;
        SpecializationId = string.Empty;
        Changed?.Invoke();
    }

    public void Restore(AdventureSkillSave? save)
    {
        Experience = Math.Clamp(save?.Experience ?? 0, 0, 999999);
        Level = LevelFor(Experience);
        SpecializationId = AdventureSkillCatalog.IsSpecialization(
            Kind,
            save?.SpecializationId ?? string.Empty
        ) && Level >= 3
            ? save!.SpecializationId
            : string.Empty;
        Changed?.Invoke();
    }

    public AdventureSkillSave Capture() => new()
    {
        Experience = Experience,
        Level = Level,
        SpecializationId = SpecializationId
    };

    public void Record(int amount)
    {
        Experience = Math.Min(999999, Experience + Math.Max(1, amount));
        Level = LevelFor(Experience);
        Changed?.Invoke();
    }

    public ActionResult ChooseSpecialization(string specializationId)
    {
        if (!CanChooseSpecialization)
        {
            return ActionResult.Fail("adventure.skill.specialization_locked");
        }
        if (!AdventureSkillCatalog.IsSpecialization(
                Kind,
                specializationId
            ))
        {
            return ActionResult.Fail("adventure.skill.specialization_unknown");
        }

        SpecializationId = specializationId;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "adventure.skill.specialization_chosen"
        );
    }

    public static int LevelFor(int experience)
    {
        var level = 0;
        foreach (var threshold in AdventureSkillCatalog.LevelThresholds)
        {
            if (experience < threshold)
            {
                break;
            }
            level++;
        }
        return Math.Clamp(level - 1, 0, 5);
    }
}

public static class DeepMineCatalog
{
    public const int MaximumRoom = 12;
    public const int AnchorInterval = 3;
    private static readonly string[] EnemyIds =
    [
        StarfallRuinsTrialCatalog.ShardlingEnemyId,
        StarfallRuinsTrialCatalog.PrismWispEnemyId,
        StarfallRuinsTrialCatalog.MoonshardMiteEnemyId,
        StarfallRuinsTrialCatalog.VeilwingBatEnemyId,
        StarfallRuinsTrialCatalog.HollowSentinelEnemyId,
        StarfallRuinsTrialCatalog.StarironBurrowerEnemyId
    ];
    private static readonly string[] MineralIds =
    [
        DataCatalog.LumenSlateOreId,
        DataCatalog.MoonveinOreId,
        DataCatalog.PrismheartOreId,
        DataCatalog.StarironOreId
    ];

    public static DeepMineRoomDefinition Room(int seed, int roomNumber)
    {
        var room = Math.Clamp(roomNumber, 1, MaximumRoom);
        var roll = StableHash(seed, room);
        var kind = room % AnchorInterval == 0
            ? DeepMineRoomKind.AnchorSanctum
            : (DeepMineRoomKind)(roll % 3);
        var enemyId = EnemyIds[(int)((roll / 7) % (uint)EnemyIds.Length)];
        var mineralId = MineralIds[Math.Min(
            MineralIds.Length - 1,
            (room - 1) / 3
        )];
        var rewardWeaponId = room switch
        {
            4 => DataCatalog.CrystalPikeId,
            8 => DataCatalog.MoonarcBowId,
            _ => string.Empty
        };
        return new DeepMineRoomDefinition(
            room,
            $"deep_mine_{seed}_{room:00}",
            kind,
            enemyId,
            mineralId,
            Math.Min(3, (room - 1) / 3),
            rewardWeaponId
        );
    }

    public static int EnemyMaxHealth(DeepMineRoomDefinition room) =>
        StarfallRuinsTrialCatalog.Enemy(room.EnemyId).MaxHealth +
        room.Number * 2;

    public static int SeedForDay(int day) =>
        CalendarSystem.YearNumber(day) * 10007 + 631;

    private static uint StableHash(int seed, int room)
    {
        uint hash = 2166136261;
        foreach (var value in new[] { seed, room, room * 31 + seed })
        {
            hash ^= unchecked((uint)value);
            hash *= 16777619;
        }
        return hash;
    }
}

public sealed class DeepMineSystem
{
    public const int BaseEnemyMineralDrop = 1;
    public const int BaseExcavationYield = 1;
    public const int GemseekerExcavationYield = 2;
    public const int BaseExcavationEnergyCost = 4;

    private readonly HashSet<int> _clearedRooms = [];
    private readonly HashSet<int> _excavatedRooms = [];
    private readonly HashSet<string> _claimedWeaponIds =
        new(StringComparer.Ordinal);
    private bool _dodgePrepared;
    private float _retaliationProgress;

    public int Seed { get; private set; }
    public bool Active { get; private set; }
    public int CurrentRoom { get; private set; }
    public int EnemyHealth { get; private set; }
    public int DeepestRoom { get; private set; }
    public int StableAnchorRoom { get; private set; }
    public AdventureSkillProgression CrystalMiningSkill { get; } = new(
        AdventureSkillKind.CrystalMining
    );
    public AdventureSkillProgression NightwatchSkill { get; } = new(
        AdventureSkillKind.Nightwatch
    );
    public event Action? Changed;

    public DeepMineSystem()
    {
        CrystalMiningSkill.Changed += () => Changed?.Invoke();
        NightwatchSkill.Changed += () => Changed?.Invoke();
    }

    public void Reset()
    {
        Seed = 0;
        Active = false;
        CurrentRoom = 0;
        EnemyHealth = 0;
        DeepestRoom = 0;
        StableAnchorRoom = 0;
        _clearedRooms.Clear();
        _excavatedRooms.Clear();
        _claimedWeaponIds.Clear();
        _dodgePrepared = false;
        _retaliationProgress = 0;
        CrystalMiningSkill.Reset();
        NightwatchSkill.Reset();
        Changed?.Invoke();
    }

    public void Restore(MiningSave? save)
    {
        Seed = Math.Max(0, save?.ExpeditionSeed ?? 0);
        DeepestRoom = Math.Clamp(
            save?.DeepestExpeditionRoom ?? 0,
            0,
            DeepMineCatalog.MaximumRoom
        );
        StableAnchorRoom = Math.Clamp(
            save?.StableAnchorRoom ?? 0,
            0,
            DeepMineCatalog.MaximumRoom
        );
        StableAnchorRoom -= StableAnchorRoom % DeepMineCatalog.AnchorInterval;
        _clearedRooms.Clear();
        _clearedRooms.UnionWith((save?.ClearedExpeditionRooms ?? [])
            .Where(room => room is >= 1 and <= DeepMineCatalog.MaximumRoom));
        _excavatedRooms.Clear();
        _excavatedRooms.UnionWith((save?.ExcavatedExpeditionRooms ?? [])
            .Where(room => room is >= 1 and <= DeepMineCatalog.MaximumRoom));
        _claimedWeaponIds.Clear();
        _claimedWeaponIds.UnionWith(
            (save?.ClaimedExpeditionWeaponIds ?? [])
            .Where(id => StarfallRuinsTrialCatalog.TryWeapon(id, out _))
        );
        CurrentRoom = Math.Clamp(
            save?.ExpeditionRoom ?? 0,
            0,
            DeepMineCatalog.MaximumRoom
        );
        Active = save?.ExpeditionActive == true && CurrentRoom > 0 &&
            CurrentRoom <= DeepMineCatalog.MaximumRoom;
        if (Active)
        {
            var maximum = DeepMineCatalog.EnemyMaxHealth(
                DeepMineCatalog.Room(Seed, CurrentRoom)
            );
            EnemyHealth = _clearedRooms.Contains(CurrentRoom)
                ? 0
                : Math.Clamp(save?.ExpeditionEnemyHealth ?? maximum, 1, maximum);
        }
        else
        {
            CurrentRoom = 0;
            EnemyHealth = 0;
        }
        _dodgePrepared = false;
        _retaliationProgress = Active
            ? Math.Clamp(save?.ExpeditionRetaliationProgress ?? 0, 0, 0.99f)
            : 0;
        CrystalMiningSkill.Restore(save?.CrystalMiningSkill);
        NightwatchSkill.Restore(save?.NightwatchSkill);
        Changed?.Invoke();
    }

    public void CaptureInto(MiningSave save)
    {
        save.ExpeditionSeed = Seed;
        save.ExpeditionActive = Active;
        save.ExpeditionRoom = CurrentRoom;
        save.ExpeditionEnemyHealth = EnemyHealth;
        save.ExpeditionRetaliationProgress = _retaliationProgress;
        save.DeepestExpeditionRoom = DeepestRoom;
        save.StableAnchorRoom = StableAnchorRoom;
        save.ClearedExpeditionRooms = _clearedRooms.Order().ToList();
        save.ExcavatedExpeditionRooms = _excavatedRooms.Order().ToList();
        save.ClaimedExpeditionWeaponIds = _claimedWeaponIds
            .Order(StringComparer.Ordinal)
            .ToList();
        save.CrystalMiningSkill = CrystalMiningSkill.Capture();
        save.NightwatchSkill = NightwatchSkill.Capture();
    }

    public ActionResult Start(int day, Inventory inventory)
    {
        if (Active)
        {
            return ActionResult.Success(messageKey: "deep_mine.resumed");
        }
        if (StableAnchorRoom >= DeepMineCatalog.MaximumRoom)
        {
            return ActionResult.Fail("deep_mine.completed");
        }

        if (!HasWeapon(inventory))
        {
            if (!inventory.CanAdd(DataCatalog.MoonsteelShortbladeId, 1) ||
                !inventory.Add(DataCatalog.MoonsteelShortbladeId, 1))
            {
                return ActionResult.Fail("notice.inventory_full");
            }
            inventory.PromoteToHotbar(DataCatalog.MoonsteelShortbladeId);
            _claimedWeaponIds.Add(DataCatalog.MoonsteelShortbladeId);
        }

        if (Seed == 0)
        {
            Seed = DeepMineCatalog.SeedForDay(day);
        }
        Active = true;
        CurrentRoom = Math.Clamp(
            StableAnchorRoom + 1,
            1,
            DeepMineCatalog.MaximumRoom
        );
        EnterCurrentRoom();
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "deep_mine.started");
    }

    public DeepMineSnapshot Snapshot()
    {
        if (!Active)
        {
            return new DeepMineSnapshot(
                false,
                Seed,
                0,
                DeepestRoom,
                StableAnchorRoom,
                string.Empty,
                0,
                0,
                false,
                false,
                string.Empty,
                string.Empty
            );
        }

        var room = DeepMineCatalog.Room(Seed, CurrentRoom);
        return new DeepMineSnapshot(
            true,
            Seed,
            CurrentRoom,
            DeepestRoom,
            StableAnchorRoom,
            room.EnemyId,
            EnemyHealth,
            DeepMineCatalog.EnemyMaxHealth(room),
            EnemyHealth <= 0,
            _excavatedRooms.Contains(CurrentRoom),
            room.MineralItemId,
            room.RewardWeaponItemId
        );
    }

    public DeepMineAttackResult Attack(
        string weaponItemId,
        Inventory inventory,
        CombatSystem combat,
        CollectionSystem collection,
        int damageBonus = 0,
        float incomingDamageMultiplier = 1f,
        float enemySpeedMultiplier = 1f
    )
    {
        if (!Active || EnemyHealth <= 0)
        {
            return new DeepMineAttackResult(false, "deep_mine.no_enemy");
        }
        var attackCheck = combat.CheckAttack(weaponItemId);
        if (!attackCheck.Succeeded)
        {
            return new DeepMineAttackResult(false, attackCheck.MessageKey);
        }

        var room = DeepMineCatalog.Room(Seed, CurrentRoom);
        var weapon = StarfallRuinsTrialCatalog.Weapon(weaponItemId);
        var damage = weapon.Damage + Math.Max(0, damageBonus) +
            (NightwatchSkill.SpecializationId ==
                AdventureSkillCatalog.SpellbladeSpecializationId
                    ? 3
                    : 0);
        var lethal = damage >= EnemyHealth;
        var additions = new List<CraftingIngredient>();
        if (lethal)
        {
            additions.Add(new CraftingIngredient(
                room.MineralItemId,
                BaseEnemyMineralDrop
            ));
            if (!string.IsNullOrWhiteSpace(room.RewardWeaponItemId) &&
                !_claimedWeaponIds.Contains(room.RewardWeaponItemId) &&
                inventory.Count(room.RewardWeaponItemId) == 0)
            {
                additions.Add(new CraftingIngredient(
                    room.RewardWeaponItemId,
                    1
                ));
            }
            if (!inventory.CanAddMany(additions))
            {
                return new DeepMineAttackResult(
                    false,
                    "notice.inventory_full"
                );
            }
        }

        combat.BeginCheckedAttack(weaponItemId);
        var applied = Math.Min(EnemyHealth, damage);
        EnemyHealth -= applied;
        if (!lethal)
        {
            var enemyDamage = StarfallRuinsTrialCatalog
                .Enemy(room.EnemyId)
                .Damage;
            if (_dodgePrepared)
            {
                _dodgePrepared = false;
                Changed?.Invoke();
                return new DeepMineAttackResult(
                    true,
                    "deep_mine.attack_dodged",
                    applied,
                    EnemyHealth
                );
            }

            _retaliationProgress += Math.Clamp(
                enemySpeedMultiplier,
                0.5f,
                1f
            );
            if (_retaliationProgress < 1f)
            {
                Changed?.Invoke();
                return new DeepMineAttackResult(
                    true,
                    "deep_mine.enemy_slowed",
                    applied,
                    EnemyHealth
                );
            }
            _retaliationProgress -= 1f;

            var reduction = NightwatchSkill.SpecializationId ==
                AdventureSkillCatalog.GuardianSpecializationId
                    ? 2
                    : 0;
            var adjustedDamage = Math.Max(
                1,
                (int)MathF.Ceiling(
                    (enemyDamage - reduction) *
                    Math.Clamp(incomingDamageMultiplier, 0.5f, 1f)
                )
            );
            var hit = combat.ReceiveHit(adjustedDamage);
            Changed?.Invoke();
            return new DeepMineAttackResult(
                true,
                hit.MessageKey,
                applied,
                EnemyHealth,
                DamageTaken: hit.DamageTaken,
                PlayerDefeated: hit.PlayerDefeated
            );
        }

        _clearedRooms.Add(CurrentRoom);
        if (CurrentRoom % DeepMineCatalog.AnchorInterval == 0)
        {
            StableAnchorRoom = Math.Max(StableAnchorRoom, CurrentRoom);
        }
        _ = inventory.TryAddMany(additions);
        collection.RecordDiscovery(room.EnemyId);
        NightwatchSkill.Record(12 + CurrentRoom * 2);
        var rewardWeaponId = room.RewardWeaponItemId;
        if (!string.IsNullOrWhiteSpace(rewardWeaponId) &&
            inventory.Count(rewardWeaponId) > 0)
        {
            _claimedWeaponIds.Add(rewardWeaponId);
            inventory.PromoteToHotbar(rewardWeaponId);
        }
        Changed?.Invoke();
        return new DeepMineAttackResult(
            true,
            "deep_mine.enemy_defeated",
            applied,
            0,
            true,
            DropItemId: room.MineralItemId,
            RewardWeaponItemId: rewardWeaponId
        );
    }

    public CombatDodgeResult PrepareDodge(CombatSystem combat)
    {
        if (!Active || EnemyHealth <= 0)
        {
            return new CombatDodgeResult(false, "deep_mine.no_enemy");
        }

        var result = combat.BeginDodge();
        if (result.Succeeded)
        {
            _dodgePrepared = true;
            Changed?.Invoke();
        }
        return result;
    }

    public ActionResult Excavate(
        string shovelTierId,
        int availableEnergy,
        Inventory inventory,
        int energyReduction = 0
    )
    {
        if (!Active || EnemyHealth > 0)
        {
            return ActionResult.Fail("deep_mine.room_not_clear");
        }
        if (_excavatedRooms.Contains(CurrentRoom))
        {
            return ActionResult.Fail("deep_mine.room_excavated");
        }

        var room = DeepMineCatalog.Room(Seed, CurrentRoom);
        var tier = ToolProgressionCatalog.Tier(shovelTierId);
        if (tier.Rank < room.RequiredToolRank)
        {
            return ActionResult.Fail("deep_mine.shovel_tier_low");
        }
        var energyCost = Math.Max(
            1,
            BaseExcavationEnergyCost + CurrentRoom / 4 -
            (CrystalMiningSkill.SpecializationId ==
                AdventureSkillCatalog.ForgekeeperSpecializationId
                    ? 1
                    : 0) -
            Math.Max(0, energyReduction)
        );
        if (availableEnergy < energyCost)
        {
            return ActionResult.Fail("notice.no_energy");
        }
        var amount = CrystalMiningSkill.SpecializationId ==
            AdventureSkillCatalog.GemseekerSpecializationId
                ? GemseekerExcavationYield
                : BaseExcavationYield;
        if (!inventory.CanAdd(room.MineralItemId, amount) ||
            !inventory.Add(room.MineralItemId, amount))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        _excavatedRooms.Add(CurrentRoom);
        CrystalMiningSkill.Record(10 + CurrentRoom * 2);
        DeepestRoom = Math.Max(DeepestRoom, CurrentRoom);
        Changed?.Invoke();
        return ActionResult.Grant(
            room.MineralItemId,
            amount,
            energyCost,
            "deep_mine.excavated"
        );
    }

    public ActionResult AdvanceRoom()
    {
        if (!Active || !_excavatedRooms.Contains(CurrentRoom))
        {
            return ActionResult.Fail("deep_mine.excavate_first");
        }
        if (CurrentRoom >= DeepMineCatalog.MaximumRoom)
        {
            Active = false;
            CurrentRoom = 0;
            EnemyHealth = 0;
            StableAnchorRoom = DeepMineCatalog.MaximumRoom;
            Changed?.Invoke();
            return ActionResult.Success(messageKey: "deep_mine.completed");
        }

        CurrentRoom++;
        EnterCurrentRoom();
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "deep_mine.room_entered");
    }

    public void Leave()
    {
        _dodgePrepared = false;
        Changed?.Invoke();
    }

    public void RecoverFromDefeat()
    {
        Active = false;
        CurrentRoom = 0;
        EnemyHealth = 0;
        _dodgePrepared = false;
        Changed?.Invoke();
    }

    private void EnterCurrentRoom()
    {
        var room = DeepMineCatalog.Room(Seed, CurrentRoom);
        EnemyHealth = _clearedRooms.Contains(CurrentRoom)
            ? 0
            : DeepMineCatalog.EnemyMaxHealth(room);
        DeepestRoom = Math.Max(DeepestRoom, CurrentRoom);
        _dodgePrepared = false;
    }

    private static bool HasWeapon(Inventory inventory) =>
        StarfallRuinsTrialCatalog.Weapons.Any(weapon =>
            inventory.Count(weapon.ItemId) > 0
        );
}
