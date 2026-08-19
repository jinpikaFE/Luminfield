using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class FarmingSkillSystemTests
{
    [Fact]
    public void LevelThresholdsAreDataDrivenFromZeroThroughFive()
    {
        Assert.Equal(
            [0, 1, 2, 3, 4, 5],
            FarmingSkillCatalog.Levels.Select(level => level.Level).ToArray()
        );
        Assert.Equal(
            [0, 20, 50, 100, 175, 275],
            FarmingSkillCatalog.Levels
                .Select(level => level.RequiredExperience)
                .ToArray()
        );
        Assert.Equal(0, FarmingSkillSystem.LevelForExperience(19));
        Assert.Equal(1, FarmingSkillSystem.LevelForExperience(20));
        Assert.Equal(3, FarmingSkillSystem.LevelForExperience(100));
        Assert.Equal(5, FarmingSkillSystem.LevelForExperience(275));
    }

    [Fact]
    public void OnlySuccessfulFarmingActionsAwardTheirConfiguredExperience()
    {
        var session = new GameSession();
        session.NewGame();
        var position = new GridPosition(12, 16);

        session.Inventory.Select(1);
        Assert.True(session.UseSelected(position).Succeeded);
        Assert.Equal(2, session.FarmingSkill.Experience);
        Assert.False(session.UseSelected(position).Succeeded);
        Assert.Equal(2, session.FarmingSkill.Experience);

        Assert.True(session.Inventory.Add(DataCatalog.StarbudSeedId, 1));
        Assert.True(session.Inventory.PromoteToHotbar(DataCatalog.StarbudSeedId));
        Assert.True(session.UseSelected(position).Succeeded);
        Assert.Equal(5, session.FarmingSkill.Experience);
        Assert.False(session.UseSelected(position).Succeeded);
        Assert.Equal(5, session.FarmingSkill.Experience);

        session.Inventory.Select(3);
        Assert.True(session.UseSelected(position).Succeeded);
        Assert.Equal(6, session.FarmingSkill.Experience);
        Assert.False(session.UseSelected(position).Succeeded);
        Assert.Equal(6, session.FarmingSkill.Experience);

        session.Farm.Restore(
        [
            new FarmTileState
            {
                X = position.X,
                Y = position.Y,
                Tilled = true,
                CropId = DataCatalog.StarbudId,
                WateredNights = DataCatalog.Crop(DataCatalog.StarbudId)
                    .MatureAfterWateredNights,
                QualityRoll = 50
            }
        ]);
        session.Inventory.Select(0);
        Assert.True(session.UseSelected(position).Succeeded);
        Assert.Equal(14, session.FarmingSkill.Experience);
        Assert.False(session.UseSelected(position).Succeeded);
        Assert.Equal(14, session.FarmingSkill.Experience);
    }

    [Fact]
    public void FailedHarvestWithFullBackpackDoesNotAwardExperience()
    {
        var session = new GameSession();
        session.NewGame();
        var position = new GridPosition(12, 16);
        var inventory = Enumerable.Range(0, Inventory.SlotCount)
            .Select(_ => new InventorySlot
            {
                ItemId = DataCatalog.CrystalShardId,
                Count = DataCatalog.Item(DataCatalog.CrystalShardId).MaxStack
            })
            .ToList();
        session.Inventory.Restore(inventory, 0);
        session.Inventory.Select(0);
        session.Farm.Restore(
        [
            new FarmTileState
            {
                X = position.X,
                Y = position.Y,
                Tilled = true,
                CropId = DataCatalog.StarbudId,
                WateredNights = 2,
                QualityRoll = 50
            }
        ]);

        Assert.False(session.UseSelected(position).Succeeded);
        Assert.Equal(0, session.FarmingSkill.Experience);
        Assert.Equal(DataCatalog.StarbudId, session.Farm.Tiles[position].CropId);
    }

    [Fact]
    public void SpecializationsDoNotApplyUntilAChoiceIsMade()
    {
        var skill = new FarmingSkillSystem();
        skill.Restore(new FarmingSkillSave { Experience = 100 });

        Assert.True(skill.CanChooseSpecialization);
        Assert.Equal(2, skill.WateringEnergyCost);
        Assert.Equal(8, skill.ExperienceFor(FarmingSkillAction.Harvest));
    }

    [Fact]
    public void DewkeeperPreviewAndActionUseTheSameReducedWateringCost()
    {
        var position = new GridPosition(12, 16);
        var chosen = PreparedWateringSession(position, chooseDewkeeper: true);
        chosen.Inventory.Select(3);

        Assert.Equal(1, chosen.FarmingSkill.WateringEnergyCost);
        Assert.True(chosen.PreviewSelectedTarget(position).IsAvailable);
        var result = chosen.UseSelected(position);
        Assert.True(result.Succeeded);
        Assert.Equal(1, result.EnergyCost);
        Assert.Equal(0, chosen.Energy);

        var unchosen = PreparedWateringSession(position, chooseDewkeeper: false);
        unchosen.Inventory.Select(3);
        Assert.Equal(
            TargetPreviewState.Blocked,
            unchosen.PreviewSelectedTarget(position).State
        );
        Assert.False(unchosen.UseSelected(position).Succeeded);
        Assert.Equal(1, unchosen.Energy);
    }

    [Fact]
    public void ResonanceScholarAddsFiftyPercentOnlyToSuccessfulHarvests()
    {
        var skill = new FarmingSkillSystem();
        skill.Restore(new FarmingSkillSave { Experience = 100 });
        Assert.True(skill.ChooseSpecialization(
            FarmingSkillCatalog.ResonanceScholarId
        ).Succeeded);

        var before = skill.Experience;
        var awarded = skill.RecordSuccessfulAction(
            FarmingSkillAction.Harvest
        );

        Assert.Equal(12, awarded);
        Assert.Equal(before + 12, skill.Experience);
        Assert.Equal(1, skill.ExperienceFor(FarmingSkillAction.Water));
    }

    [Fact]
    public void SpecializationIsOneTimeAndRequiresLevelThree()
    {
        var locked = new FarmingSkillSystem();
        Assert.False(locked.ChooseSpecialization(
            FarmingSkillCatalog.DewkeeperId
        ).Succeeded);

        var unlocked = new FarmingSkillSystem();
        unlocked.Restore(new FarmingSkillSave { Experience = 100 });
        Assert.True(unlocked.ChooseSpecialization(
            FarmingSkillCatalog.DewkeeperId
        ).Succeeded);
        Assert.False(unlocked.ChooseSpecialization(
            FarmingSkillCatalog.ResonanceScholarId
        ).Succeeded);
        Assert.Equal(FarmingSkillCatalog.DewkeeperId, unlocked.SpecializationId);
    }

    [Fact]
    public void SaveNormalizationClearsInvalidOrPrematureChoices()
    {
        var negative = FarmingSkillSystem.NormalizeSave(
            new FarmingSkillSave
            {
                Experience = -20,
                SpecializationId = FarmingSkillCatalog.DewkeeperId
            }
        );
        var premature = FarmingSkillSystem.NormalizeSave(
            new FarmingSkillSave
            {
                Experience = 99,
                SpecializationId = FarmingSkillCatalog.DewkeeperId
            }
        );
        var unknown = FarmingSkillSystem.NormalizeSave(
            new FarmingSkillSave
            {
                Experience = 100,
                SpecializationId = "unknown"
            }
        );

        Assert.Equal(0, negative.Experience);
        Assert.Empty(negative.SpecializationId);
        Assert.Empty(premature.SpecializationId);
        Assert.Empty(unknown.SpecializationId);
    }

    [Fact]
    public void LegalSpecializationPermanentlyRoundTripsThroughSessionProjection()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.FarmingSkill.Experience = 100;
        session.Restore(save);
        Assert.True(session.ChooseFarmingSpecialization(
            FarmingSkillCatalog.DewkeeperId
        ).Succeeded);

        var restored = new GameSession();
        restored.Restore(session.Capture());

        Assert.Equal(3, restored.FarmingSkill.Level);
        Assert.Equal(
            FarmingSkillCatalog.DewkeeperId,
            restored.FarmingSkill.SpecializationId
        );
        Assert.False(restored.FarmingSkill.CanChooseSpecialization);
        Assert.False(restored.ChooseFarmingSpecialization(
            FarmingSkillCatalog.ResonanceScholarId
        ).Succeeded);
    }

    [Fact]
    public void MaximumLevelProgressHasNoZeroLengthNextLevel()
    {
        var skill = new FarmingSkillSystem();
        skill.Restore(new FarmingSkillSave { Experience = int.MaxValue });

        Assert.True(skill.IsMaximumLevel);
        Assert.Equal(5, skill.Level);
        Assert.Equal(0, skill.ExperienceForCurrentLevel);
        Assert.Equal(0, skill.RecordSuccessfulAction(FarmingSkillAction.Harvest));
    }

    private static GameSession PreparedWateringSession(
        GridPosition position,
        bool chooseDewkeeper
    )
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Player.Energy = 1;
        save.FarmingSkill.Experience = 100;
        save.FarmTiles =
        [
            new FarmTileState
            {
                X = position.X,
                Y = position.Y,
                Tilled = true,
                CropId = DataCatalog.StarbudId,
                QualityRoll = 50
            }
        ];
        session.Restore(save);
        if (chooseDewkeeper)
        {
            Assert.True(session.ChooseFarmingSpecialization(
                FarmingSkillCatalog.DewkeeperId
            ).Succeeded);
        }

        return session;
    }
}
