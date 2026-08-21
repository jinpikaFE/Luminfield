using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class StarfallRuinsTrialTests
{
    [Fact]
    public void CatalogFreezesRoomsExpandedEnemiesArtifactsWeaponsAndSixthLight()
    {
        Assert.Equal("starfall_ruins_trial", PlayerLocationIds.StarfallRuinsTrial);
        Assert.Equal(new GridPosition(127, 104),
            StarfallRuinsTrialLayout.WorldEntryCell);
        Assert.Equal(new GridPosition(127, 103),
            StarfallRuinsTrialLayout.WorldReturnCell);
        Assert.Equal(40, StarfallRuinsTrialLayout.Width);
        Assert.Equal(22, StarfallRuinsTrialLayout.Height);
        Assert.False(WorldDefinition.IsBlocked(
            StarfallRuinsTrialLayout.WorldReturnCell));
        Assert.Equal(3, StarfallRuinsTrialCatalog.Rooms.Count);
        Assert.Equal(6, StarfallRuinsTrialCatalog.Enemies.Count);
        Assert.Equal(6, StarfallRuinsTrialCatalog.EnemyInstances.Count);
        Assert.Equal(4, StarfallRuinsTrialCatalog.Artifacts.Count);
        Assert.Equal(
            StarfallRuinsTrialCatalog.EnemyInstances.Count,
            StarfallRuinsTrialCatalog.EnemyInstances
                .Select(enemy => enemy.InstanceId)
                .Distinct(StringComparer.Ordinal)
                .Count()
        );

        var weapon = DataCatalog.Item(DataCatalog.MoonsteelShortbladeId);
        Assert.Equal(ItemKind.Weapon, weapon.Kind);
        Assert.Equal(1, weapon.MaxStack);
        Assert.Equal(0, weapon.SellPrice);
        Assert.Equal(10,
            StarfallRuinsTrialCatalog.MoonsteelShortblade.Damage);
        Assert.Equal(24,
            StarfallRuinsTrialCatalog.MoonsteelShortblade.RangePixels);
        Assert.Equal(0.45f,
            StarfallRuinsTrialCatalog.MoonsteelShortblade.CooldownSeconds);

        Assert.Collection(
            StarfallRuinsTrialCatalog.Enemies.Take(3),
            shardling =>
            {
                Assert.Equal("enemy_shardling", shardling.Id);
                Assert.Equal(20, shardling.MaxHealth);
                Assert.Equal(6, shardling.Damage);
                Assert.Equal(48, shardling.MovementSpeedPixelsPerSecond);
                Assert.Equal(EnemyAttackKind.Melee, shardling.AttackKind);
            },
            wisp =>
            {
                Assert.Equal("enemy_prism_wisp", wisp.Id);
                Assert.Equal(20, wisp.MaxHealth);
                Assert.Equal(7, wisp.Damage);
                Assert.Equal(80, wisp.ProjectileSpeedPixelsPerSecond);
                Assert.Equal(EnemyAttackKind.Projectile, wisp.AttackKind);
            },
            sentinel =>
            {
                Assert.Equal("enemy_hollow_sentinel", sentinel.Id);
                Assert.Equal(50, sentinel.MaxHealth);
                Assert.Equal(12, sentinel.Damage);
                Assert.Equal(36, sentinel.AreaRadiusPixels);
                Assert.Equal(EnemyAttackKind.AreaOfEffect,
                    sentinel.AttackKind);
            }
        );

        Assert.All(StarfallRuinsTrialCatalog.Artifacts, artifact =>
        {
            Assert.Equal(ItemKind.Artifact,
                DataCatalog.Item(artifact.ItemId).Kind);
            Assert.Equal(1, DataCatalog.Item(artifact.ItemId).MaxStack);
            Assert.Equal(0, DataCatalog.Item(artifact.ItemId).SellPrice);
            Assert.Contains(artifact.ItemId, DataCatalog.StorableItemIds);
        });
        Assert.Equal(4, CompendiumCatalog.ArtifactEntries.Count);
        Assert.Equal(6, CompendiumCatalog.EnemyEntries.Count);
        Assert.Equal(4, DataCatalog.StarfallRuinsStarlight.Nodes.Count);
        Assert.True(DataCatalog.StarfallRuinsStarlight
            .RequiresManualActivation);
        Assert.Equal(
            DataCatalog.StarfallSixfoldConvergenceRewardId,
            DataCatalog.StarfallRuinsStarlight.RewardId
        );
    }

    [Fact]
    public void TrialGatePreviewAndActionShareCrystalPassageRule()
    {
        var locked = new GameSession();
        locked.NewGame();
        PositionBesideWorldGate(locked);
        var before = Snapshot(locked);

        var lockedPreview = locked.PreviewSelectedTarget(
            StarfallRuinsTrialLayout.WorldEntryCell
        );
        var lockedAction = locked.TryEnterStarfallRuinsTrial(
            StarfallRuinsTrialLayout.WorldEntryCell
        );

        Assert.Equal(TargetPreviewKind.StarfallRuinsPortal,
            lockedPreview.Kind);
        Assert.Equal(TargetPreviewState.Blocked, lockedPreview.State);
        Assert.Equal("ruins.trial.passage_locked", lockedPreview.LabelKey);
        Assert.False(lockedAction.Succeeded);
        Assert.Equal(lockedPreview.LabelKey, lockedAction.MessageKey);
        Assert.Equal(before, Snapshot(locked));

        var unlocked = CrystalPassageSession();
        PositionBesideWorldGate(unlocked);
        var available = unlocked.PreviewSelectedTarget(
            StarfallRuinsTrialLayout.WorldEntryCell
        );
        Assert.Equal(TargetPreviewState.Available, available.State);
        Assert.True(unlocked.TryEnterStarfallRuinsTrial(
            StarfallRuinsTrialLayout.WorldEntryCell
        ).Succeeded);

        unlocked.Inventory.Select(1);
        var wrongTool = unlocked.PreviewSelectedTarget(
            StarfallRuinsTrialLayout.WorldEntryCell
        );
        Assert.Equal(TargetPreviewState.NeedsTool, wrongTool.State);
        Assert.False(unlocked.TryEnterStarfallRuinsTrial(
            StarfallRuinsTrialLayout.WorldEntryCell
        ).Succeeded);
    }

    [Fact]
    public void WeaponRackIsAtomicAndInventoryIsTheSingleOwnershipProof()
    {
        var session = TrialSession();
        PositionBeside(session, StarfallRuinsTrialLayout.WeaponRackCell);

        var preview = session.PreviewSelectedTarget(
            StarfallRuinsTrialLayout.WeaponRackCell
        );
        var result = session.RecoverMoonsteelShortblade(
            StarfallRuinsTrialLayout.WeaponRackCell
        );

        Assert.Equal(TargetPreviewKind.RuinsWeaponRack, preview.Kind);
        Assert.Equal(TargetPreviewState.Available, preview.State);
        Assert.True(result.Succeeded);
        Assert.Equal(1,
            session.Inventory.Count(DataCatalog.MoonsteelShortbladeId));
        Assert.Equal(DataCatalog.MoonsteelShortbladeId,
            session.Inventory.Selected.ItemId);
        Assert.Contains(DataCatalog.MoonsteelShortbladeId,
            DataCatalog.StorableItemIds);
        Assert.DoesNotContain(DataCatalog.MoonsteelShortbladeId,
            DataCatalog.SellableItemIds);

        session.Inventory.Select(0);
        Assert.Equal("ruins.weapon.already_recovered",
            session.CheckRecoverMoonsteelShortblade(
                StarfallRuinsTrialLayout.WeaponRackCell
            ).MessageKey);

        var chestCell = Enumerable.Range(1, FarmSystem.MapHeight - 2)
            .SelectMany(y => Enumerable.Range(1, FarmSystem.MapWidth - 2)
                .Select(x => new GridPosition(x, y)))
            .First(cell => session.Storage.CheckPlacement(
                cell,
                session.Farm
            ) == ChestPlacementIssue.None);
        Assert.True(session.Inventory.Add(DataCatalog.StarwovenChestId, 1));
        Assert.True(session.Storage.Place(
            chestCell,
            session.Farm,
            session.Inventory
        ).Succeeded);
        Assert.True(session.Storage.StoreOne(
            chestCell,
            DataCatalog.MoonsteelShortbladeId,
            session.Inventory
        ).Succeeded);
        Assert.Equal(0,
            session.Inventory.Count(DataCatalog.MoonsteelShortbladeId));
        Assert.True(session.StarfallRuinsTrial.WeaponClaimed);
        var legacyStorageSave = session.Capture();
        legacyStorageSave.StarfallRuinsTrial.WeaponClaimed = false;
        var restoredFromStorageEvidence = new GameSession();
        restoredFromStorageEvidence.Restore(legacyStorageSave);
        Assert.True(restoredFromStorageEvidence.StarfallRuinsTrial
            .WeaponClaimed);
        restoredFromStorageEvidence.SetPlayerLocation(
            (StarfallRuinsTrialLayout.WeaponRackCell.X - 1) * 16 + 8,
            StarfallRuinsTrialLayout.WeaponRackCell.Y * 16 + 8,
            PlayerLocationIds.StarfallRuinsTrial
        );
        restoredFromStorageEvidence.Inventory.Select(0);
        Assert.Equal("ruins.weapon.already_recovered",
            restoredFromStorageEvidence.CheckRecoverMoonsteelShortblade(
                StarfallRuinsTrialLayout.WeaponRackCell
            ).MessageKey);

        var full = TrialSession();
        FillInventoryExcept(full.Inventory, DataCatalog.MoonsteelShortbladeId);
        PositionBeside(full, StarfallRuinsTrialLayout.WeaponRackCell);
        var fullBefore = Snapshot(full);
        var blocked = full.RecoverMoonsteelShortblade(
            StarfallRuinsTrialLayout.WeaponRackCell
        );
        Assert.False(blocked.Succeeded);
        Assert.Equal("notice.inventory_full", blocked.MessageKey);
        Assert.Equal(fullBefore, Snapshot(full));
    }

    [Fact]
    public void EnemyTransientPositionDrivesPreviewRangeAndActualAttack()
    {
        var session = ArmedTrialSession();
        var instanceId = "starfall_trial_shardling_01";
        var spawn = session.StarfallRuinsTrial.Enemy(instanceId).Cell;
        var movedX = 7 * 16 + 8;
        var movedY = 16 * 16 + 8;
        Assert.True(session.MoveStarfallEnemyChecked(
            instanceId,
            movedX,
            movedY
        ).Succeeded);
        var moved = session.StarfallRuinsTrial.Enemy(instanceId);
        Assert.Equal(new GridPosition(7, 16), moved.Cell);
        Assert.Equal(TargetPreviewState.Neutral,
            session.PreviewSelectedTarget(spawn).State);

        session.SetPlayerLocation(
            movedX - 16,
            movedY,
            PlayerLocationIds.StarfallRuinsTrial
        );
        var preview = session.PreviewSelectedTarget(moved.Cell);
        var hit = session.AttackStarfallEnemy(instanceId, moved.Cell);
        Assert.Equal(TargetPreviewKind.RuinsEnemy, preview.Kind);
        Assert.Equal(TargetPreviewState.Available, preview.State);
        Assert.True(hit.Succeeded);
        Assert.Equal(10, hit.DamageDealt);
        Assert.Equal(10, hit.RemainingHealth);

        session.SetPlayerLocation(
            StarfallRuinsTrialLayout.WorldReturnCell.X * 16 + 8,
            StarfallRuinsTrialLayout.WorldReturnCell.Y * 16 + 8,
            PlayerLocationIds.World
        );
        var reset = session.StarfallRuinsTrial.Enemy(instanceId);
        Assert.Equal(spawn, reset.Cell);
        Assert.Equal(reset.MaxHealth, reset.CurrentHealth);
    }

    [Fact]
    public void FirstKillDiscoversEnemyAndOnlyClearedRoomsPersist()
    {
        var session = ArmedTrialSession();
        KillEnemy(session, "starfall_trial_shardling_01");

        Assert.True(session.Collection.IsDiscovered(
            StarfallRuinsTrialCatalog.ShardlingEnemyId
        ));
        Assert.False(session.StarfallRuinsTrial.IsRoomCleared(
            StarfallRuinsTrialCatalog.ShardCourtRoomId
        ));
        var midRoom = session.Capture();
        var reloadedMidRoom = new GameSession();
        reloadedMidRoom.Restore(midRoom);
        Assert.Equal(
            reloadedMidRoom.StarfallRuinsTrial
                .Enemy("starfall_trial_shardling_01").MaxHealth,
            reloadedMidRoom.StarfallRuinsTrial
                .Enemy("starfall_trial_shardling_01").CurrentHealth
        );
        Assert.True(reloadedMidRoom.Collection.IsDiscovered(
            StarfallRuinsTrialCatalog.ShardlingEnemyId
        ));

        session = ArmedTrialSession();
        KillEnemy(session, "starfall_trial_shardling_01");
        KillEnemy(session, "starfall_trial_shardling_02");
        Assert.True(session.StarfallRuinsTrial.IsRoomCleared(
            StarfallRuinsTrialCatalog.ShardCourtRoomId
        ));
        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.True(restored.StarfallRuinsTrial.IsRoomCleared(
            StarfallRuinsTrialCatalog.ShardCourtRoomId
        ));
        Assert.True(restored.StarfallRuinsTrial
            .Enemy("starfall_trial_shardling_01").Defeated);
        Assert.False(restored.StarfallRuinsTrial.IsRoomCleared(
            StarfallRuinsTrialCatalog.PrismGalleryRoomId
        ));
    }

    [Fact]
    public void ArtifactRecoveryAndArchiveDonationAreAtomicAndPersistent()
    {
        var session = TrialSession();
        var compass = StarfallRuinsTrialCatalog.Artifacts[0];
        PositionBeside(session, compass.Cell);
        var recovered = session.RecoverStarfallArtifact(compass.Cell);
        Assert.True(recovered.Succeeded);
        Assert.Equal(1, session.Inventory.Count(compass.ItemId));
        Assert.True(session.Collection.IsDiscovered(compass.ItemId));
        Assert.Contains(compass.ItemId,
            session.StarfallRuinsTrial.RecoveredArtifactIds);

        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.Contains(compass.ItemId,
            restored.StarfallRuinsTrial.RecoveredArtifactIds);

        restored.Inventory.Select(0);
        PositionBesideArchiveDesk(restored);
        var donated = restored.DonateCollectionEntry(
            VillageCatalog.MoonlitArchiveDeskCell,
            compass.ItemId
        );
        Assert.True(donated.Succeeded);
        Assert.Equal(0, restored.Inventory.Count(compass.ItemId));
        Assert.True(restored.Collection.IsDonated(compass.ItemId));
        Assert.True(restored.Collection.IsDiscovered(compass.ItemId));
        var afterDonation = new GameSession();
        afterDonation.Restore(restored.Capture());
        Assert.True(afterDonation.Collection.IsDonated(compass.ItemId));
        Assert.Contains(compass.ItemId,
            afterDonation.StarfallRuinsTrial.RecoveredArtifactIds);

        var repeatBefore = Snapshot(afterDonation);
        var repeat = afterDonation.DonateCollectionEntry(
            VillageCatalog.MoonlitArchiveDeskCell,
            compass.ItemId
        );
        Assert.False(repeat.Succeeded);
        Assert.Equal(repeatBefore, Snapshot(afterDonation));
    }

    [Fact]
    public void LegacyEvidenceAddsBothNewCategoriesWithoutChangingSchema()
    {
        var save = new GameSaveV1
        {
            Collection = new CollectionSave
            {
                Initialized = true,
                InitializedCategoryIds = CompendiumCatalog.CategoryIds
                    .Where(id => id is not CollectionCategoryIds.Artifacts and
                        not CollectionCategoryIds.Enemies)
                    .ToList()
            },
            StarfallRuinsTrial = new StarfallRuinsTrialSave
            {
                ClearedRoomIds =
                [
                    StarfallRuinsTrialCatalog.ShardCourtRoomId,
                    StarfallRuinsTrialCatalog.PrismGalleryRoomId
                ],
                RecoveredArtifactIds = [DataCatalog.DawnpathCompassId]
            }
        };

        var normalized = CollectionSystem.NormalizeSave(
            save.Collection,
            CollectionSystem.LegacyEvidenceItemIds(save)
        );

        Assert.Contains(CollectionCategoryIds.Artifacts,
            normalized.InitializedCategoryIds);
        Assert.Contains(CollectionCategoryIds.Enemies,
            normalized.InitializedCategoryIds);
        Assert.Contains(DataCatalog.DawnpathCompassId,
            normalized.DiscoveredEntryIds);
        Assert.Contains(StarfallRuinsTrialCatalog.ShardlingEnemyId,
            normalized.DiscoveredEntryIds);
        Assert.Contains(StarfallRuinsTrialCatalog.PrismWispEnemyId,
            normalized.DiscoveredEntryIds);
        Assert.DoesNotContain(StarfallRuinsTrialCatalog.HollowSentinelEnemyId,
            normalized.DiscoveredEntryIds);
        Assert.Equal(1, SaveService.CurrentSchemaVersion);
    }

    [Fact]
    public void SixthLightReadsFirstFiveFromSameSaveButRequiresRealActivation()
    {
        var session = CompletedSixthLightRequirementsSession();
        var pedestal = WorldDefinition.StarfallRuinsStarlightCell;

        Assert.Equal(3, session.StarlightNodeProgress(
            DataCatalog.StarfallRuinsStarlightId,
            DataCatalog.StarfallMemoryArchiveNodeId));
        Assert.Equal(3, session.StarlightNodeProgress(
            DataCatalog.StarfallRuinsStarlightId,
            DataCatalog.StarfallNightwatchTrialNodeId));
        Assert.Equal(2, session.StarlightNodeProgress(
            DataCatalog.StarfallRuinsStarlightId,
            DataCatalog.StarfallTrustedPathsNodeId));
        Assert.Equal(5, session.StarlightNodeProgress(
            DataCatalog.StarfallRuinsStarlightId,
            DataCatalog.StarfallFiveLightsNodeId));
        Assert.False(session.Starlight.StarfallSixfoldConvergenceUnlocked);

        Assert.True(session.OpenStarlightPedestal(
            DataCatalog.StarfallRuinsStarlightId,
            pedestal
        ).Succeeded);
        Assert.False(session.Starlight.StarfallSixfoldConvergenceUnlocked);
        var beforeActivation = new GameSession();
        beforeActivation.Restore(session.Capture());
        Assert.False(beforeActivation.Starlight
            .StarfallSixfoldConvergenceUnlocked);

        var activated = beforeActivation.ActivateStarlightPedestal(
            DataCatalog.StarfallRuinsStarlightId,
            pedestal
        );
        Assert.True(activated.Succeeded);
        Assert.True(beforeActivation.Starlight
            .StarfallSixfoldConvergenceUnlocked);
        var restored = new GameSession();
        restored.Restore(beforeActivation.Capture());
        Assert.True(restored.Starlight.StarfallSixfoldConvergenceUnlocked);
    }

    [Fact]
    public void DefeatResolutionAdvancesOneDayAndResetsTrialOnlyOnce()
    {
        var session = TrialSession();
        var day = session.Clock.Day;
        while (!session.Combat.IsDefeated)
        {
            var hit = session.ReceiveStarfallEnemyHit(
                "starfall_trial_hollow_sentinel_01"
            );
            Assert.True(hit.Succeeded);
            session.AdvanceStarfallCombat(
                CombatSystem.HurtInvulnerabilitySeconds
            );
        }

        var resolved = session.ResolveStarfallTrialDefeat();
        Assert.True(resolved.Succeeded);
        Assert.Equal(day + 1, session.Clock.Day);
        Assert.True(session.InsideCottage);
        Assert.Equal(50, session.Energy);
        Assert.Equal(CombatSystem.MaxHealth, session.Combat.CurrentHealth);
        Assert.False(session.ResolveStarfallTrialDefeat().Succeeded);
    }

    private static GameSession TrialSession()
    {
        var session = CrystalPassageSession();
        session.SetPlayerLocation(
            StarfallRuinsTrialLayout.SafeArrivalCell.X * 16 + 8,
            StarfallRuinsTrialLayout.SafeArrivalCell.Y * 16 + 8,
            PlayerLocationIds.StarfallRuinsTrial
        );
        return session;
    }

    private static GameSession ArmedTrialSession()
    {
        var session = TrialSession();
        PositionBeside(session, StarfallRuinsTrialLayout.WeaponRackCell);
        Assert.True(session.RecoverMoonsteelShortblade(
            StarfallRuinsTrialLayout.WeaponRackCell
        ).Succeeded);
        return session;
    }

    private static GameSession CrystalPassageSession()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Mining.DeepestRoomReached = 5;
        save.ToolProgression = new ToolProgressionSave
        {
            Tools =
            [
                new ToolProgressionEntrySave
                {
                    ToolId = DataCatalog.ShovelId,
                    TierId = ToolProgressionCatalog.BronzeStarTierId
                }
            ]
        };
        save.Starlight = new StarlightSave
        {
            Pedestals =
            [CompletedInventoryPedestal(DataCatalog.CrystalValeStarlight)]
        };
        session.Restore(save);
        Assert.True(session.Starlight.CrystalRuinsPassageUnlocked);
        return session;
    }

    private static GameSession CompletedSixthLightRequirementsSession()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Mining.DeepestRoomReached = 5;
        save.ToolProgression = new ToolProgressionSave
        {
            Tools =
            [
                new ToolProgressionEntrySave
                {
                    ToolId = DataCatalog.ShovelId,
                    TierId = ToolProgressionCatalog.BronzeStarTierId
                }
            ]
        };
        save.Festival.Results =
        [
            new FestivalYearResultSave
            {
                FestivalId = FestivalCatalog.StarharvestMarketFestivalId,
                Year = 1
            }
        ];
        save.Starlight = new StarlightSave
        {
            Pedestals = DataCatalog.StarlightPedestals.Values
                .Where(pedestal =>
                    pedestal.Id != DataCatalog.StarfallRuinsStarlightId)
                .Select(CompletedInventoryPedestal)
                .ToList()
        };
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = CompendiumCatalog.EnemyEntries
                .Select(entry => entry.Id)
                .ToList(),
            DonatedEntryIds = CompendiumCatalog.ArtifactEntries
                .Take(3)
                .Select(entry => entry.Id)
                .ToList()
        };
        save.Village.Relationships =
        [
            new VillageRelationshipSave
            {
                NpcId = VillageCatalog.KaelId,
                Points = 60
            },
            new VillageRelationshipSave
            {
                NpcId = VillageCatalog.LioraId,
                Points = 60
            }
        ];
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = (WorldDefinition.StarfallRuinsStarlightCell.X - 1) *
            16 + 8;
        save.Player.Y = WorldDefinition.StarfallRuinsStarlightCell.Y * 16 + 8;
        session.Restore(save);
        return session;
    }

    private static StarlightPedestalSave CompletedInventoryPedestal(
        StarlightPedestalDefinition definition
    ) => new()
    {
        PedestalId = definition.Id,
        Nodes = definition.Nodes.Select(node => new StarlightNodeSave
        {
            NodeId = node.Id,
            Contributions = CompleteContributions(node)
        }).ToList()
    };

    private static List<StarlightContributionSave> CompleteContributions(
        StarlightNodeDefinition node
    )
    {
        if (node.SourceKind != StarlightNodeSourceKind.Inventory)
        {
            return [];
        }

        var remaining = node.RequiredCount;
        var contributions = new List<StarlightContributionSave>();
        foreach (var option in node.Options)
        {
            var count = Math.Min(remaining, option.MaximumCount);
            if (count <= 0)
            {
                continue;
            }
            contributions.Add(new StarlightContributionSave
            {
                ItemId = option.ItemId,
                Count = count
            });
            remaining -= count;
            if (remaining == 0)
            {
                break;
            }
        }
        Assert.Equal(0, remaining);
        return contributions;
    }

    private static void KillEnemy(GameSession session, string instanceId)
    {
        while (!session.StarfallRuinsTrial.Enemy(instanceId).Defeated)
        {
            var enemy = session.StarfallRuinsTrial.Enemy(instanceId);
            session.SetPlayerLocation(
                enemy.CurrentX - 16,
                enemy.CurrentY,
                PlayerLocationIds.StarfallRuinsTrial
            );
            var result = session.AttackStarfallEnemy(instanceId, enemy.Cell);
            Assert.True(result.Succeeded, result.MessageKey);
            session.AdvanceStarfallCombat(
                StarfallRuinsTrialCatalog.MoonsteelShortblade.CooldownSeconds
            );
        }
    }

    private static void PositionBesideWorldGate(GameSession session) =>
        session.SetPlayerLocation(
            (StarfallRuinsTrialLayout.WorldEntryCell.X - 1) * 16 + 8,
            StarfallRuinsTrialLayout.WorldEntryCell.Y * 16 + 8,
            PlayerLocationIds.World
        );

    private static void PositionBeside(
        GameSession session,
        GridPosition target
    ) => session.SetPlayerLocation(
        (target.X - 1) * 16 + 8,
        target.Y * 16 + 8,
        PlayerLocationIds.StarfallRuinsTrial
    );

    private static void PositionBesideArchiveDesk(GameSession session)
    {
        var desk = VillageCatalog.MoonlitArchiveDeskCell;
        session.SetPlayerLocation(
            (desk.X - 1) * 16 + 8,
            desk.Y * 16 + 8,
            PlayerLocationIds.MoonlitArchive
        );
    }

    private static void FillInventoryExcept(
        Inventory inventory,
        string excludedItemId
    )
    {
        var items = DataCatalog.Items.Values
            .Where(item => item.Kind != ItemKind.Tool &&
                item.Id != excludedItemId &&
                item.Id != DataCatalog.MoonsteelShortbladeId)
            .Select(item => item.Id)
            .Distinct(StringComparer.Ordinal)
            .Take(Inventory.SlotCount - Inventory.StartingToolCount)
            .ToArray();
        Assert.Equal(Inventory.SlotCount - Inventory.StartingToolCount,
            items.Length);
        foreach (var itemId in items)
        {
            Assert.True(inventory.Add(
                itemId,
                DataCatalog.Item(itemId).MaxStack
            ));
        }
        Assert.False(inventory.CanAdd(excludedItemId, 1));
    }

    private static string Snapshot(GameSession session) =>
        JsonSerializer.Serialize(session.Capture());
}
