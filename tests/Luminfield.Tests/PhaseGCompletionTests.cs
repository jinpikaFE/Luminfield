using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class PhaseGCompletionTests
{
    private static readonly GridPosition HomesteadWater = new(38, 21);

    [Fact]
    public void FiveSkillCatalogIncludesGatheringAndAllSkillsReachLevelFive()
    {
        var session = PreparedFinaleSession(allSkillsMaximum: true);

        var skills = session.StellarSkillSnapshots();
        Assert.Equal(5, skills.Count);
        Assert.Equal(
            Enum.GetValues<StellarSkillKind>(),
            skills.Select(skill => skill.Kind)
        );
        Assert.All(skills, skill => Assert.True(skill.IsMaximumLevel));
        Assert.True(session.AllFiveSkillsAtMaximum);
    }

    [Fact]
    public void GatheringSpecializationsChangeAtomicRealWorldYields()
    {
        var gathering = new GatheringSkillSystem();
        gathering.Restore(new GatheringSkillSave { Experience = 140 });
        Assert.True(gathering.ChooseSpecialization(
            GatheringSkillCatalog.StarseekerId
        ).Succeeded);

        var forage = new ForageSystem();
        forage.Reset(1, DataCatalog.ClearWeatherId);
        var spawn = Assert.Single(forage.ActiveSpawns.Take(1));
        var inventory = new Inventory();
        inventory.Reset();
        var player = Adjacent(spawn.Cell);

        var collected = forage.TryCollect(
            spawn.Cell,
            PlayerLocationIds.World,
            player,
            DataCatalog.HandId,
            inventory,
            1 + gathering.ForageYieldBonus
        );

        Assert.True(collected.Succeeded);
        Assert.Equal(2, collected.GrantedItemCount);
        Assert.Equal(2, inventory.Count(spawn.ItemId));
        Assert.Null(forage.SpawnAt(spawn.Cell));

        var grove = new GatheringSkillSystem();
        grove.Restore(new GatheringSkillSave
        {
            Experience = 140,
            SpecializationId = GatheringSkillCatalog.GroveWardenId
        });
        var resource = new WorldResourceSystem();
        var tree = FindWorldResource(WorldResourceKind.Tree);
        var woodBefore = inventory.Count(DataCatalog.LumenwoodId);
        var felled = resource.TryGather(
            tree,
            DataCatalog.MacheteId,
            100,
            inventory,
            1,
            grove.LumberYieldBonus
        );

        Assert.True(felled.Succeeded);
        Assert.Equal(3, felled.GrantedItemCount);
        Assert.Equal(
            woodBefore + 3,
            inventory.Count(DataCatalog.LumenwoodId)
        );
    }

    [Fact]
    public void MainStoryRequiresGateAndFiveMaximumSkillsWithoutSideEffects()
    {
        var session = PreparedFinaleSession(allSkillsMaximum: false);
        var before = JsonSerializer.Serialize(session.Capture());

        var result = session.CompleteMainStory();

        Assert.False(result.Succeeded);
        Assert.Equal(
            "stellar.main_story.requires_five_skills",
            result.MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
    }

    [Fact]
    public void FinalePersistsAndPreservesExistingLongTermStateForPostgame()
    {
        var session = PreparedFinaleSession(allSkillsMaximum: true);
        var before = session.Capture();

        var completed = session.CompleteMainStory();

        Assert.True(completed.Succeeded);
        Assert.True(session.StellarResonance.MainStoryCompleted);
        Assert.Equal(session.Clock.Day, session.StellarResonance.CompletionDay);

        var restored = new GameSession();
        restored.Restore(session.Capture());
        var after = restored.Capture();
        Assert.True(restored.StellarResonance.MainStoryCompleted);
        Assert.Equal(before.Day, restored.Clock.Day);
        Assert.Equal(before.Village.MetNpcIds, after.Village.MetNpcIds);
        Assert.Equal(
            before.Village.Relationships.Select(relationship =>
                (relationship.NpcId, relationship.Points)
            ),
            after.Village.Relationships.Select(relationship =>
                (relationship.NpcId, relationship.Points)
            )
        );
        Assert.Equal(
            before.Construction.Projects.Select(project =>
                (project.ProjectId, project.Completed)
            ),
            after.Construction.Projects.Select(project =>
                (project.ProjectId, project.Completed)
            )
        );
        Assert.Equal(
            before.Collection.DiscoveredEntryIds,
            after.Collection.DiscoveredEntryIds
        );
        Assert.Equal(
            before.Festival.Results.Select(result =>
                (result.FestivalId, result.Year, result.Score)
            ),
            after.Festival.Results.Select(result =>
                (result.FestivalId, result.Year, result.Score)
            )
        );
        Assert.True(restored.StarGate.Activated);
    }

    [Fact]
    public void PostgameResonanceRanksAreCappedAndUnlockHorizontalEffects()
    {
        var resonance = new StellarResonanceSystem();
        resonance.Restore(
            new StellarResonanceSave
            {
                MainStoryCompleted = true,
                CompletionDay = 8,
                Experience = 749
            },
            starGateActivated: true,
            currentDay: 20
        );

        Assert.Equal(4, resonance.Rank);
        Assert.Equal(1, resonance.GatheringYieldBonus);
        Assert.Equal(0.05f, resonance.FishingCatchZoneBonus);
        Assert.Equal(1, resonance.MiningEnergyReduction);
        Assert.Equal(1, resonance.WateringEnergyReduction);
        Assert.Equal(0, resonance.CombatDamageBonus);

        Assert.Equal(1, resonance.RecordPostgameActivity(
            StellarSkillKind.Nightwatch
        ));
        Assert.Equal(5, resonance.Rank);
        Assert.Equal(2, resonance.CombatDamageBonus);
        Assert.Equal(0, resonance.RecordPostgameActivity(
            StellarSkillKind.Nightwatch
        ));
        Assert.Equal(750, resonance.Experience);
    }

    [Fact]
    public void FishingAndWateringActionsApplyResonanceAndAccessibilityBonuses()
    {
        var baselineFishing = PreparedResonanceSession(0);
        var assistedFishing = PreparedResonanceSession(140);
        assistedFishing.ConfigureAccessibility(0.08f, 1f, 1f);

        Assert.True(baselineFishing.BeginFishingChallenge(
            HomesteadWater
        ).Succeeded);
        Assert.True(assistedFishing.BeginFishingChallenge(
            HomesteadWater
        ).Succeeded);
        Assert.True(
            assistedFishing.FishingMinigame.Snapshot().CatchZoneSize >
            baselineFishing.FishingMinigame.Snapshot().CatchZoneSize
        );

        var cropCell = new GridPosition(12, 16);
        var baselineWatering = PreparedWateringSession(cropCell, 0);
        var resonantWatering = PreparedWateringSession(cropCell, 480);
        var baselineEnergy = baselineWatering.Energy;
        var resonantEnergy = resonantWatering.Energy;

        var baselineResult = baselineWatering.UseSelected(cropCell);
        var resonantResult = resonantWatering.UseSelected(cropCell);

        Assert.True(baselineResult.Succeeded);
        Assert.True(resonantResult.Succeeded);
        Assert.Equal(2, baselineResult.EnergyCost);
        Assert.Equal(1, resonantResult.EnergyCost);
        Assert.Equal(
            baselineEnergy - baselineResult.EnergyCost,
            baselineWatering.Energy
        );
        Assert.Equal(
            resonantEnergy - resonantResult.EnergyCost,
            resonantWatering.Energy
        );
    }

    [Fact]
    public void DeepMineActionsApplyResonanceAndAccessibilityBonuses()
    {
        var baselineMining = PreparedDeepMineSession(
            resonanceExperience: 0,
            enemyCleared: true
        );
        var resonantMining = PreparedDeepMineSession(
            resonanceExperience: 280,
            enemyCleared: true
        );

        var baselineExcavation = baselineMining.ExcavateDeepMineRoom();
        var resonantExcavation = resonantMining.ExcavateDeepMineRoom();

        Assert.True(baselineExcavation.Succeeded);
        Assert.True(resonantExcavation.Succeeded);
        Assert.Equal(4, baselineExcavation.EnergyCost);
        Assert.Equal(3, resonantExcavation.EnergyCost);

        var baselineCombat = PreparedDeepMineSession(
            resonanceExperience: 0,
            enemyCleared: false
        );
        var assistedCombat = PreparedDeepMineSession(
            resonanceExperience: 750,
            enemyCleared: false
        );
        SelectWeapon(baselineCombat);
        SelectWeapon(assistedCombat);
        assistedCombat.ConfigureAccessibility(0, 0.5f, 0.5f);

        var baselineAttack = baselineCombat.AttackDeepMineEnemy();
        var assistedAttack = assistedCombat.AttackDeepMineEnemy();

        Assert.True(baselineAttack.Succeeded);
        Assert.True(assistedAttack.Succeeded);
        Assert.Equal(
            baselineAttack.DamageDealt + 2,
            assistedAttack.DamageDealt
        );
        Assert.True(baselineAttack.DamageTaken > 0);
        Assert.Equal(0, assistedAttack.DamageTaken);
    }

    private static GameSession PreparedFinaleSession(bool allSkillsMaximum)
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = 112;
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = FarmLayout.StarGateCell.X * 16 + 8;
        save.Player.Y = (FarmLayout.StarGateCell.Y - 1) * 16 + 8;
        save.Construction.Projects =
        [
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.HomesteadWorkshopProjectId,
                Completed = true
            },
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.SixfoldStarGateProjectId,
                Completed = true
            }
        ];
        save.Village = new VillageSave
        {
            MetNpcIds = [VillageCatalog.LioraId],
            Relationships =
            [
                new VillageRelationshipSave
                {
                    NpcId = VillageCatalog.LioraId,
                    Points = 180,
                    LastTalkDay = 100,
                    LastGiftDay = 99
                }
            ]
        };
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList(),
            DiscoveredEntryIds = [DataCatalog.StarbudId]
        };
        save.Festival.Results =
        [
            new FestivalYearResultSave
            {
                FestivalId = FestivalCatalog.StarharvestMarketFestivalId,
                Year = 1,
                Score = 12
            }
        ];
        save.StarGate.Activated = true;
        save.FarmingSkill.Experience = 275;
        save.GatheringSkill.Experience = allSkillsMaximum ? 380 : 379;
        save.Fishing.Experience = 380;
        save.Mining.CrystalMiningSkill.Experience = 430;
        save.Mining.NightwatchSkill.Experience = 430;
        session.Restore(save);
        session.Inventory.Select(0);
        Assert.Equal(DataCatalog.HandId, session.Inventory.Selected.ItemId);
        return session;
    }

    private static GameSession PreparedResonanceSession(
        int resonanceExperience
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Day = 112;
        save.Construction.Projects =
        [
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.SixfoldStarGateProjectId,
                Completed = true
            }
        ];
        save.StarGate.Activated = true;
        save.StellarResonance = new StellarResonanceSave
        {
            MainStoryCompleted = true,
            CompletionDay = 112,
            Experience = resonanceExperience
        };
        session.Restore(save);
        return session;
    }

    private static GameSession PreparedWateringSession(
        GridPosition cropCell,
        int resonanceExperience
    )
    {
        var session = PreparedResonanceSession(resonanceExperience);
        var save = session.Capture();
        save.FarmTiles =
        [
            new FarmTileState
            {
                X = cropCell.X,
                Y = cropCell.Y,
                Tilled = true,
                CropId = DataCatalog.StarbudId,
                PlantedDay = save.Day
            }
        ];
        session.Restore(save);
        session.Inventory.Select(3);
        return session;
    }

    private static GameSession PreparedDeepMineSession(
        int resonanceExperience,
        bool enemyCleared
    )
    {
        var session = PreparedResonanceSession(resonanceExperience);
        var save = session.Capture();
        save.Mining.ExpeditionSeed = DeepMineCatalog.SeedForDay(save.Day);
        save.Mining.ExpeditionActive = true;
        save.Mining.ExpeditionRoom = 1;
        save.Mining.ExpeditionEnemyHealth = DeepMineCatalog.EnemyMaxHealth(
            DeepMineCatalog.Room(save.Mining.ExpeditionSeed, 1)
        );
        save.Mining.ClearedExpeditionRooms = enemyCleared ? [1] : [];
        session.Restore(save);
        return session;
    }

    private static void SelectWeapon(GameSession session)
    {
        Assert.True(session.Inventory.Add(
            DataCatalog.MoonsteelShortbladeId,
            1
        ));
        Assert.True(session.Inventory.PromoteToHotbar(
            DataCatalog.MoonsteelShortbladeId
        ));
        var weaponIndex = session.Inventory.Slots
            .Select((slot, index) => (slot, index))
            .Single(entry =>
                entry.slot.ItemId == DataCatalog.MoonsteelShortbladeId
            )
            .index;
        session.Inventory.Select(weaponIndex);
    }

    private static GridPosition Adjacent(GridPosition cell)
    {
        var candidates = new[]
        {
            new GridPosition(cell.X, cell.Y - 1),
            new GridPosition(cell.X + 1, cell.Y),
            new GridPosition(cell.X, cell.Y + 1),
            new GridPosition(cell.X - 1, cell.Y)
        };
        return candidates.First(WorldDefinition.IsInBounds);
    }

    private static GridPosition FindWorldResource(WorldResourceKind kind)
    {
        for (var y = 0; y < WorldDefinition.Height; y++)
        {
            for (var x = 0; x < WorldDefinition.Width; x++)
            {
                var cell = new GridPosition(x, y);
                if (WorldDefinition.ResourceAt(cell) == kind)
                {
                    return cell;
                }
            }
        }

        throw new InvalidOperationException($"No resource found for {kind}.");
    }
}
