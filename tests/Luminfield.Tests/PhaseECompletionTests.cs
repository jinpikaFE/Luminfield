using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class PhaseECompletionTests
{
    [Fact]
    public void DeepMineRoomsAreDeterministicAndCatalogHasRequiredBreadth()
    {
        Assert.Equal(6, StarfallRuinsTrialCatalog.Enemies.Count);
        Assert.Equal(3, StarfallRuinsTrialCatalog.Weapons.Count);
        Assert.Equal(4, MiningCatalog.Minerals.Count);
        Assert.Equal(4, ToolProgressionCatalog.Tiers.Count);
        Assert.Equal(3, ToolProgressionCatalog.Upgrades.Count);

        var first = Enumerable.Range(1, DeepMineCatalog.MaximumRoom)
            .Select(room => DeepMineCatalog.Room(42, room))
            .ToArray();
        var repeated = Enumerable.Range(1, DeepMineCatalog.MaximumRoom)
            .Select(room => DeepMineCatalog.Room(42, room))
            .ToArray();
        var alternate = Enumerable.Range(1, DeepMineCatalog.MaximumRoom)
            .Select(room => DeepMineCatalog.Room(43, room))
            .ToArray();

        Assert.Equal(first, repeated);
        Assert.NotEqual(
            first.Select(room => room.EnemyId),
            alternate.Select(room => room.EnemyId)
        );
        Assert.Equal(
            [3, 6, 9, 12],
            first.Where(room => room.Kind == DeepMineRoomKind.AnchorSanctum)
                .Select(room => room.Number)
        );
        Assert.All(first, room =>
            Assert.Contains(
                room.EnemyId,
                StarfallRuinsTrialCatalog.Enemies.Select(enemy => enemy.Id)
            )
        );
    }

    [Fact]
    public void TwelveRoomExpeditionAnchorsWeaponsDropsAndSkillsPersist()
    {
        var session = StarforgedSession();
        Assert.True(session.DeepMine.Start(
            session.Clock.Day,
            session.Inventory
        ).Succeeded);

        while (session.DeepMine.Active)
        {
            var snapshot = session.DeepMine.Snapshot();
            while (!snapshot.EnemyDefeated)
            {
                session.AdvanceDeepMineCombat(2);
                var dodge = session.DodgeInDeepMine();
                Assert.True(dodge.Succeeded);
                var attack = session.AttackDeepMineEnemy();
                Assert.True(attack.Succeeded);
                Assert.False(attack.PlayerDefeated);
                snapshot = session.DeepMine.Snapshot();
            }

            var excavated = session.ExcavateDeepMineRoom();
            Assert.True(excavated.Succeeded);
            var advanced = session.AdvanceDeepMineRoom();
            Assert.True(advanced.Succeeded);
        }

        Assert.Equal(
            DeepMineCatalog.MaximumRoom,
            session.DeepMine.StableAnchorRoom
        );
        Assert.Equal(
            DeepMineCatalog.MaximumRoom,
            session.DeepMine.DeepestRoom
        );
        Assert.True(session.Inventory.Count(DataCatalog.CrystalPikeId) > 0);
        Assert.True(session.Inventory.Count(DataCatalog.MoonarcBowId) > 0);
        Assert.True(session.DeepMine.CrystalMiningSkill.Experience > 0);
        Assert.True(session.DeepMine.NightwatchSkill.Experience > 0);
        Assert.Equal(
            6,
            CompendiumCatalog.EnemyEntries.Count
        );

        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.Equal(12, restored.DeepMine.StableAnchorRoom);
        Assert.Equal(
            session.DeepMine.CrystalMiningSkill.Experience,
            restored.DeepMine.CrystalMiningSkill.Experience
        );
        Assert.Equal(
            session.DeepMine.NightwatchSkill.Experience,
            restored.DeepMine.NightwatchSkill.Experience
        );
    }

    [Fact]
    public void ShovelProgressesAcrossThreeAtomicUpgradesAndFourTiers()
    {
        var session = RichGrottoSession();
        foreach (var upgrade in ToolProgressionCatalog.Upgrades)
        {
            var result = session.StartToolUpgrade(
                CrystalGrottoSurveyLayout.UpgradeBenchCell,
                upgrade.Id
            );
            Assert.True(result.Succeeded);
            for (var night = 0; night < upgrade.RequiredNights; night++)
            {
                session.EndDay();
            }
            Assert.True(session.ToolProgression.IsUpgradeCompleted(upgrade.Id));
        }

        Assert.Equal(
            ToolProgressionCatalog.StarforgedTierId,
            session.ToolProgression.TierIdFor(DataCatalog.ShovelId)
        );
        Assert.Equal(
            ToolProgressionCatalog.Upgrades.Select(upgrade => upgrade.Id)
                .ToHashSet(StringComparer.Ordinal),
            session.ToolProgression.CompletedMilestoneIds()
                .ToHashSet(StringComparer.Ordinal)
        );
    }

    [Fact]
    public void MiningAndNightwatchSpecializationsAreIndependentAndSaved()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Mining.CrystalMiningSkill.Experience = 500;
        save.Mining.NightwatchSkill.Experience = 500;
        session.Restore(save);

        Assert.True(session.ChooseAdventureSpecialization(
            AdventureSkillKind.CrystalMining,
            AdventureSkillCatalog.GemseekerSpecializationId
        ).Succeeded);
        Assert.True(session.ChooseAdventureSpecialization(
            AdventureSkillKind.Nightwatch,
            AdventureSkillCatalog.GuardianSpecializationId
        ).Succeeded);

        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.Equal(
            AdventureSkillCatalog.GemseekerSpecializationId,
            restored.DeepMine.CrystalMiningSkill.SpecializationId
        );
        Assert.Equal(
            AdventureSkillCatalog.GuardianSpecializationId,
            restored.DeepMine.NightwatchSkill.SpecializationId
        );
    }

    [Fact]
    public void DeepMineDefeatLosesDayRestoresHealthAndKeepsStableAnchor()
    {
        var session = StarforgedSession();
        Assert.True(session.Inventory.Add(
            DataCatalog.MoonsteelShortbladeId,
            1
        ));
        var save = session.Capture();
        save.Mining.ExpeditionSeed = 42;
        save.Mining.ExpeditionActive = true;
        save.Mining.ExpeditionRoom = 4;
        save.Mining.ExpeditionEnemyHealth = 60;
        save.Mining.DeepestExpeditionRoom = 4;
        save.Mining.StableAnchorRoom = 3;
        save.Combat.CurrentHealth = 1;
        session.Restore(save);
        SelectWeapon(session, DataCatalog.MoonsteelShortbladeId);
        var dayBefore = session.Clock.Day;

        var attack = session.AttackDeepMineEnemy();
        Assert.True(attack.PlayerDefeated);
        var recovered = session.ResolveDeepMineDefeat();

        Assert.True(recovered.Succeeded);
        Assert.Equal(dayBefore + 1, session.Clock.Day);
        Assert.Equal(CombatSystem.MaxHealth, session.Combat.CurrentHealth);
        Assert.Equal(3, session.DeepMine.StableAnchorRoom);
        Assert.False(session.DeepMine.Active);
        Assert.True(session.InsideCrystalGrottoSurvey);
    }

    private static GameSession StarforgedSession()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.ToolProgression.Tools =
        [
            new ToolProgressionEntrySave
            {
                ToolId = DataCatalog.ShovelId,
                TierId = ToolProgressionCatalog.StarforgedTierId
            }
        ];
        session.Restore(save);
        return session;
    }

    private static GameSession RichGrottoSession()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Coins = 10000;
        save.Player.LocationId = PlayerLocationIds.CrystalGrottoSurvey;
        save.Player.X = 17 * 16 + 8;
        save.Player.Y = 17 * 16 + 8;
        var inventory = new Inventory();
        inventory.Restore(save.Inventory, 0);
        Assert.True(inventory.Add(DataCatalog.LumenSlateOreId, 20));
        Assert.True(inventory.Add(DataCatalog.MoonveinOreId, 20));
        Assert.True(inventory.Add(DataCatalog.PrismheartOreId, 30));
        Assert.True(inventory.Add(DataCatalog.StarironOreId, 30));
        save.Inventory = inventory.Capture();
        session.Restore(save);
        return session;
    }

    private static void SelectWeapon(GameSession session, string itemId)
    {
        session.Inventory.PromoteToHotbar(itemId);
        Assert.Equal(itemId, session.Inventory.Selected.ItemId);
    }
}
