using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luminfield.Core;

public enum FishingAssistLevel
{
    Standard,
    Relaxed,
    Story
}

public enum TargetCueMode
{
    Standard,
    HighContrast,
    Deuteranopia
}

public enum DialoguePace
{
    Slow,
    Standard,
    Fast
}

public sealed class AccessibilitySettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public FishingAssistLevel FishingAssist { get; set; } =
        FishingAssistLevel.Standard;
    public int IncomingDamagePercent { get; set; } = 100;
    public int EnemySpeedPercent { get; set; } = 100;
    public int ScreenShakePercent { get; set; } = 100;
    public int MasterVolumePercent { get; set; } = 100;
    public int AmbientVolumePercent { get; set; } = 100;
    public int EffectsVolumePercent { get; set; } = 100;
    public TargetCueMode TargetCues { get; set; } = TargetCueMode.Standard;
    public int FontScalePercent { get; set; } = 100;
    public DialoguePace DialoguePace { get; set; } = DialoguePace.Standard;
    public bool DialogueAutoAdvance { get; set; }
    public bool AutoRun { get; set; }
    public bool TargetLock { get; set; }
    public bool HoldToRepeatTools { get; set; }
    public List<string> DismissedOnboardingCardIds { get; set; } = [];
    public Dictionary<string, long> KeyboardBindings { get; set; } =
        new(StringComparer.Ordinal);

    public float FishingCatchZoneBonus => FishingAssist switch
    {
        FishingAssistLevel.Relaxed => 0.08f,
        FishingAssistLevel.Story => 0.16f,
        _ => 0f
    };

    public float IncomingDamageMultiplier => IncomingDamagePercent / 100f;
    public float EnemySpeedMultiplier => EnemySpeedPercent / 100f;
    public float MovementMultiplier => AutoRun ? 1.25f : 1f;
    public float TextScale => FontScalePercent / 100f;
    public float DialogueSecondsPerPage => DialoguePace switch
    {
        DialoguePace.Slow => 6f,
        DialoguePace.Fast => 2.4f,
        _ => 4f
    };

    public void Normalize()
    {
        SchemaVersion = CurrentSchemaVersion;
        IncomingDamagePercent = NormalizeChoice(
            IncomingDamagePercent,
            [50, 75, 100],
            100
        );
        EnemySpeedPercent = NormalizeChoice(
            EnemySpeedPercent,
            [50, 75, 100],
            100
        );
        ScreenShakePercent = NormalizeChoice(
            ScreenShakePercent,
            [0, 50, 100],
            100
        );
        MasterVolumePercent = NormalizePercent(MasterVolumePercent);
        AmbientVolumePercent = NormalizePercent(AmbientVolumePercent);
        EffectsVolumePercent = NormalizePercent(EffectsVolumePercent);
        FontScalePercent = NormalizeChoice(
            FontScalePercent,
            [100, 110, 120],
            100
        );
        if (!Enum.IsDefined(FishingAssist))
        {
            FishingAssist = FishingAssistLevel.Standard;
        }
        if (!Enum.IsDefined(TargetCues))
        {
            TargetCues = TargetCueMode.Standard;
        }
        if (!Enum.IsDefined(DialoguePace))
        {
            DialoguePace = DialoguePace.Standard;
        }
        DismissedOnboardingCardIds ??= [];
        DismissedOnboardingCardIds = DismissedOnboardingCardIds
            .Where(cardId => !string.IsNullOrWhiteSpace(cardId))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        KeyboardBindings ??= new Dictionary<string, long>(
            StringComparer.Ordinal
        );
        KeyboardBindings = KeyboardBindings
            .Where(binding =>
                !string.IsNullOrWhiteSpace(binding.Key) &&
                binding.Value > 0
            )
            .GroupBy(binding => binding.Key, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Last().Value,
                StringComparer.Ordinal
            );
    }

    public bool DismissOnboardingCard(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return false;
        }

        DismissedOnboardingCardIds ??= [];
        if (DismissedOnboardingCardIds.Contains(cardId, StringComparer.Ordinal))
        {
            return false;
        }

        DismissedOnboardingCardIds.Add(cardId);
        return true;
    }

    private static int NormalizeChoice(
        int value,
        IReadOnlyList<int> choices,
        int fallback
    ) => choices.Contains(value) ? value : fallback;

    private static int NormalizePercent(int value) => Math.Clamp(value, 0, 100);
}

public sealed class AccessibilitySettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public AccessibilitySettingsService(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public AccessibilitySettings Load()
    {
        if (!File.Exists(Path))
        {
            return new AccessibilitySettings();
        }

        try
        {
            var settings = JsonSerializer.Deserialize<AccessibilitySettings>(
                File.ReadAllText(Path),
                JsonOptions
            ) ?? new AccessibilitySettings();
            settings.Normalize();
            return settings;
        }
        catch (Exception exception) when (
            exception is JsonException or IOException
        )
        {
            return new AccessibilitySettings();
        }
    }

    public void Save(AccessibilitySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Normalize();
        var directory = System.IO.Path.GetDirectoryName(Path)
            ?? throw new InvalidOperationException(
                "Settings path has no parent directory."
            );
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{Path}.tmp";
        try
        {
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(settings, JsonOptions)
            );
            File.Move(temporaryPath, Path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
