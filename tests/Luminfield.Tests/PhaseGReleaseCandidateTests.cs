using System.Diagnostics;
using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class PhaseGReleaseCandidateTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"luminfield-phase-g-{Guid.NewGuid():N}"
    );

    [Fact]
    public void AccessibilitySettingsNormalizeAndRoundTrip()
    {
        var path = Path.Combine(_directory, "settings.json");
        var service = new AccessibilitySettingsService(path);
        var settings = new AccessibilitySettings
        {
            FishingAssist = FishingAssistLevel.Story,
            IncomingDamagePercent = 50,
            EnemySpeedPercent = 75,
            ScreenShakePercent = 0,
            TargetCues = TargetCueMode.Deuteranopia,
            FontScalePercent = 120,
            DialoguePace = DialoguePace.Fast,
            DialogueAutoAdvance = true,
            AutoRun = true,
            TargetLock = true,
            HoldToRepeatTools = true,
            DismissedOnboardingCardIds =
            [
                OnboardingPlanSystem.ShippingCardId,
                OnboardingPlanSystem.ShippingCardId,
                ""
            ],
            KeyboardBindings = new Dictionary<string, long>
            {
                ["interact"] = 65,
                ["target_lock"] = 0,
                ["crafting"] = -1
            }
        };

        service.Save(settings);
        var loaded = service.Load();

        Assert.Equal(FishingAssistLevel.Story, loaded.FishingAssist);
        Assert.Equal(0.16f, loaded.FishingCatchZoneBonus);
        Assert.Equal(0.5f, loaded.IncomingDamageMultiplier);
        Assert.Equal(0.75f, loaded.EnemySpeedMultiplier);
        Assert.Equal(1.25f, loaded.MovementMultiplier);
        Assert.Equal(
            [OnboardingPlanSystem.ShippingCardId],
            loaded.DismissedOnboardingCardIds
        );
        Assert.Equal(65, loaded.KeyboardBindings["interact"]);
        Assert.False(loaded.KeyboardBindings.ContainsKey("target_lock"));
        Assert.False(loaded.KeyboardBindings.ContainsKey("crafting"));
    }

    [Fact]
    public void SchemaOneSaveMigratesToTwoWithoutLosingProgress()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "slot_1.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            day = 42,
            coins = 777
        }));

        var result = new SaveService(path).Load();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal(2, result.Save.SchemaVersion);
        Assert.Equal(42, result.Save.Day);
        Assert.Equal(777, result.Save.Coins);
        Assert.NotNull(result.Save.GatheringSkill);
        Assert.NotNull(result.Save.StellarResonance);
    }

    [Fact]
    public void ThreeBackupsRotateAndNewestValidCopyRecoversCorruption()
    {
        var path = Path.Combine(_directory, "slot_1.json");
        var service = new SaveService(path);
        for (var day = 1; day <= 4; day++)
        {
            service.Save(new GameSaveV1 { Day = day });
        }

        Assert.True(File.Exists(service.BackupPath(1)));
        Assert.True(File.Exists(service.BackupPath(2)));
        Assert.True(File.Exists(service.BackupPath(3)));
        File.WriteAllText(path, "{broken");

        var recovered = service.Load();

        Assert.Equal(SaveLoadStatus.Loaded, recovered.Status);
        Assert.Equal(3, recovered.Save!.Day);
        Assert.NotNull(recovered.PreservedPath);
        Assert.True(File.Exists(recovered.PreservedPath));
        Assert.True(File.Exists(path));

        var reloaded = new SaveService(path).Load();
        Assert.Equal(SaveLoadStatus.Loaded, reloaded.Status);
        Assert.Equal(3, reloaded.Save!.Day);
    }

    [Fact]
    public void TenYearSimulationPreservesAValidSessionWithinBudget()
    {
        var session = PreparedCompletedPostgameSession();
        var initial = session.Capture();
        var stopwatch = Stopwatch.StartNew();
        var days = CalendarSystem.DaysPerYear * 10;

        for (var index = 0; index < days; index++)
        {
            session.EndDay();
            if (index % CalendarSystem.DaysPerYear == 0)
            {
                var snapshot = session.Capture();
                var restored = new GameSession();
                restored.Restore(snapshot);
                session = restored;
            }
        }

        stopwatch.Stop();
        var save = session.Capture();
        Assert.Equal(initial.Day + days, save.Day);
        Assert.InRange(save.Player.Energy, 0, GameSession.MaxEnergy);
        Assert.True(float.IsFinite(save.Player.X));
        Assert.True(float.IsFinite(save.Player.Y));
        Assert.True(save.StellarResonance.MainStoryCompleted);
        Assert.True(save.StarGate.Activated);
        Assert.Contains(VillageCatalog.LioraId, save.Village.MetNpcIds);
        Assert.Equal(
            Assert.Single(initial.Village.Relationships, relationship =>
                relationship.NpcId == VillageCatalog.LioraId
            ).Points,
            Assert.Single(save.Village.Relationships, relationship =>
                relationship.NpcId == VillageCatalog.LioraId
            ).Points
        );
        Assert.Contains(save.Construction.Projects, project =>
            project.ProjectId == ConstructionCatalog.SixfoldStarGateProjectId &&
            project.Completed
        );
        Assert.Contains(
            DataCatalog.StarbudId,
            save.Collection.DiscoveredEntryIds
        );
        Assert.Contains(save.Festival.Results, result =>
            result.FestivalId ==
                FestivalCatalog.StarharvestMarketFestivalId &&
            result.Year == 1
        );
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(45));
    }

    private static GameSession PreparedCompletedPostgameSession()
    {
        var session = new GameSession();
        session.NewGame(LocaleService.SimplifiedChinese);
        var save = session.Capture();
        save.Day = CalendarSystem.DaysPerYear * 2;
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
        save.StarGate.Activated = true;
        save.StellarResonance = new StellarResonanceSave
        {
            MainStoryCompleted = true,
            CompletionDay = save.Day,
            Experience = StellarResonanceCatalog.RankThresholds[^1]
        };
        save.FarmingSkill.Experience = 275;
        save.GatheringSkill.Experience = 380;
        save.Fishing.Experience = 380;
        save.Mining.CrystalMiningSkill.Experience = 430;
        save.Mining.NightwatchSkill.Experience = 430;
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
        session.Restore(save);
        return session;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
