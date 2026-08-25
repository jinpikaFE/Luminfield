using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class AccessibilitySettingsServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"luminfield-accessibility-{Guid.NewGuid():N}"
    );

    [Fact]
    public void SaveRoundTripsEveryCurrentField()
    {
        var path = Path.Combine(_directory, "settings.json");
        var service = new AccessibilitySettingsService(path);
        var expected = new AccessibilitySettings
        {
            FishingAssist = FishingAssistLevel.Relaxed,
            IncomingDamagePercent = 75,
            EnemySpeedPercent = 50,
            ScreenShakePercent = 50,
            MasterVolumePercent = 82,
            AmbientVolumePercent = 47,
            EffectsVolumePercent = 63,
            TargetCues = TargetCueMode.HighContrast,
            FontScalePercent = 110,
            DialoguePace = DialoguePace.Slow,
            DialogueAutoAdvance = true,
            AutoRun = true,
            TargetLock = true,
            HoldToRepeatTools = true,
            DismissedOnboardingCardIds = ["shipping", "route"],
            KeyboardBindings = new Dictionary<string, long>
            {
                ["interact"] = 65,
                ["crafting"] = 67
            }
        };

        service.Save(expected);
        var actual = new AccessibilitySettingsService(path).Load();

        Assert.Equal(
            AccessibilitySettings.CurrentSchemaVersion,
            actual.SchemaVersion
        );
        Assert.Equal(expected.FishingAssist, actual.FishingAssist);
        Assert.Equal(
            expected.IncomingDamagePercent,
            actual.IncomingDamagePercent
        );
        Assert.Equal(expected.EnemySpeedPercent, actual.EnemySpeedPercent);
        Assert.Equal(expected.ScreenShakePercent, actual.ScreenShakePercent);
        Assert.Equal(expected.MasterVolumePercent, actual.MasterVolumePercent);
        Assert.Equal(
            expected.AmbientVolumePercent,
            actual.AmbientVolumePercent
        );
        Assert.Equal(
            expected.EffectsVolumePercent,
            actual.EffectsVolumePercent
        );
        Assert.Equal(expected.TargetCues, actual.TargetCues);
        Assert.Equal(expected.FontScalePercent, actual.FontScalePercent);
        Assert.Equal(expected.DialoguePace, actual.DialoguePace);
        Assert.Equal(
            expected.DialogueAutoAdvance,
            actual.DialogueAutoAdvance
        );
        Assert.Equal(expected.AutoRun, actual.AutoRun);
        Assert.Equal(expected.TargetLock, actual.TargetLock);
        Assert.Equal(expected.HoldToRepeatTools, actual.HoldToRepeatTools);
        Assert.Equal(
            expected.DismissedOnboardingCardIds,
            actual.DismissedOnboardingCardIds
        );
        Assert.Equal(expected.KeyboardBindings, actual.KeyboardBindings);
    }

    [Fact]
    public void MissingAndCorruptSettingsReturnDefaults()
    {
        var path = Path.Combine(_directory, "settings.json");
        var service = new AccessibilitySettingsService(path);

        AssertDefaults(service.Load());

        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, "{broken");

        AssertDefaults(service.Load());
    }

    [Fact]
    public void LoadNormalizesInvalidValuesWithoutDroppingLegalFields()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 999,
              "fishingAssist": 999,
              "incomingDamagePercent": 60,
              "enemySpeedPercent": 50,
              "screenShakePercent": -1,
              "masterVolumePercent": -5,
              "ambientVolumePercent": 120,
              "effectsVolumePercent": 42,
              "targetCues": 999,
              "fontScalePercent": 110,
              "dialoguePace": 999,
              "dialogueAutoAdvance": true,
              "targetLock": true,
              "dismissedOnboardingCardIds": ["shipping", "shipping", ""],
              "keyboardBindings": {
                "interact": 65,
                "": 66,
                "crafting": -1
              }
            }
            """
        );

        var actual = new AccessibilitySettingsService(path).Load();

        Assert.Equal(
            AccessibilitySettings.CurrentSchemaVersion,
            actual.SchemaVersion
        );
        Assert.Equal(FishingAssistLevel.Standard, actual.FishingAssist);
        Assert.Equal(100, actual.IncomingDamagePercent);
        Assert.Equal(50, actual.EnemySpeedPercent);
        Assert.Equal(100, actual.ScreenShakePercent);
        Assert.Equal(0, actual.MasterVolumePercent);
        Assert.Equal(100, actual.AmbientVolumePercent);
        Assert.Equal(42, actual.EffectsVolumePercent);
        Assert.Equal(TargetCueMode.Standard, actual.TargetCues);
        Assert.Equal(110, actual.FontScalePercent);
        Assert.Equal(DialoguePace.Standard, actual.DialoguePace);
        Assert.True(actual.DialogueAutoAdvance);
        Assert.True(actual.TargetLock);
        Assert.Equal(["shipping"], actual.DismissedOnboardingCardIds);
        Assert.Equal(65, actual.KeyboardBindings["interact"]);
        Assert.Single(actual.KeyboardBindings);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    private static void AssertDefaults(AccessibilitySettings actual)
    {
        Assert.Equal(
            AccessibilitySettings.CurrentSchemaVersion,
            actual.SchemaVersion
        );
        Assert.Equal(FishingAssistLevel.Standard, actual.FishingAssist);
        Assert.Equal(100, actual.IncomingDamagePercent);
        Assert.Equal(100, actual.EnemySpeedPercent);
        Assert.Equal(100, actual.ScreenShakePercent);
        Assert.Equal(100, actual.MasterVolumePercent);
        Assert.Equal(100, actual.AmbientVolumePercent);
        Assert.Equal(100, actual.EffectsVolumePercent);
        Assert.Equal(TargetCueMode.Standard, actual.TargetCues);
        Assert.Equal(100, actual.FontScalePercent);
        Assert.Equal(DialoguePace.Standard, actual.DialoguePace);
        Assert.False(actual.DialogueAutoAdvance);
        Assert.False(actual.AutoRun);
        Assert.False(actual.TargetLock);
        Assert.False(actual.HoldToRepeatTools);
        Assert.Empty(actual.DismissedOnboardingCardIds);
        Assert.Empty(actual.KeyboardBindings);
    }
}
