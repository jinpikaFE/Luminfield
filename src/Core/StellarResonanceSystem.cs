namespace Luminfield.Core;

public enum StellarSkillKind
{
    Farming,
    Gathering,
    CrystalMining,
    Fishing,
    Nightwatch
}

public sealed record StellarSkillSnapshot(
    StellarSkillKind Kind,
    string NameKey,
    int Level,
    int MaximumLevel
)
{
    public bool IsMaximumLevel => Level >= MaximumLevel;
}

public static class StellarResonanceCatalog
{
    public static IReadOnlyList<int> RankThresholds { get; } =
        [0, 50, 140, 280, 480, 750];

    public static IReadOnlyDictionary<StellarSkillKind, string> SkillNameKeys
        { get; } = new Dictionary<StellarSkillKind, string>
        {
            [StellarSkillKind.Farming] = "stellar.skill.farming",
            [StellarSkillKind.Gathering] = "stellar.skill.gathering",
            [StellarSkillKind.CrystalMining] = "stellar.skill.crystal_mining",
            [StellarSkillKind.Fishing] = "stellar.skill.fishing",
            [StellarSkillKind.Nightwatch] = "stellar.skill.nightwatch"
        };

    public static int ExperienceFor(StellarSkillKind kind) => kind switch
    {
        StellarSkillKind.Farming => 2,
        StellarSkillKind.Gathering => 3,
        StellarSkillKind.CrystalMining => 3,
        StellarSkillKind.Fishing => 4,
        StellarSkillKind.Nightwatch => 4,
        _ => 0
    };
}

public sealed class StellarResonanceSystem
{
    private readonly HashSet<string> _completedMilestoneIds =
        new(StringComparer.Ordinal);

    public bool MainStoryCompleted { get; private set; }
    public int CompletionDay { get; private set; }
    public int Experience { get; private set; }
    public int Rank => RankForExperience(Experience);
    public int MaximumRank => StellarResonanceCatalog.RankThresholds.Count - 1;
    public bool IsMaximumRank => Rank >= MaximumRank;
    public IReadOnlySet<string> CompletedMilestoneIds =>
        _completedMilestoneIds;
    public int GatheringYieldBonus => Rank >= 1 ? 1 : 0;
    public float FishingCatchZoneBonus => Rank >= 2 ? 0.05f : 0f;
    public int MiningEnergyReduction => Rank >= 3 ? 1 : 0;
    public int WateringEnergyReduction => Rank >= 4 ? 1 : 0;
    public int CombatDamageBonus => Rank >= 5 ? 2 : 0;

    public event Action? Changed;

    public void Reset()
    {
        MainStoryCompleted = false;
        CompletionDay = 0;
        Experience = 0;
        _completedMilestoneIds.Clear();
        Changed?.Invoke();
    }

    public void Restore(
        StellarResonanceSave? save,
        bool starGateActivated,
        int currentDay
    )
    {
        var normalized = NormalizeSave(save, starGateActivated, currentDay);
        MainStoryCompleted = normalized.MainStoryCompleted;
        CompletionDay = normalized.CompletionDay;
        Experience = normalized.Experience;
        _completedMilestoneIds.Clear();
        _completedMilestoneIds.UnionWith(
            normalized.CompletedMilestoneIds
        );
        Changed?.Invoke();
    }

    public ActionResult CheckMainStoryCompletion(
        bool starGateActivated,
        bool allSkillsAtMaximum
    )
    {
        if (MainStoryCompleted)
        {
            return ActionResult.Fail("stellar.main_story.already_completed");
        }
        if (!starGateActivated)
        {
            return ActionResult.Fail("stellar.main_story.requires_star_gate");
        }
        if (!allSkillsAtMaximum)
        {
            return ActionResult.Fail("stellar.main_story.requires_five_skills");
        }

        return ActionResult.Success(
            messageKey: "stellar.main_story.ready"
        );
    }

    public ActionResult CompleteMainStory(
        int day,
        bool starGateActivated,
        bool allSkillsAtMaximum
    )
    {
        var check = CheckMainStoryCompletion(
            starGateActivated,
            allSkillsAtMaximum
        );
        if (!check.Succeeded)
        {
            return check;
        }

        MainStoryCompleted = true;
        CompletionDay = Math.Max(1, day);
        Experience = 0;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "stellar.main_story.completed"
        );
    }

    public int RecordPostgameActivity(StellarSkillKind kind)
    {
        if (!MainStoryCompleted || IsMaximumRank)
        {
            return 0;
        }

        var maximum = StellarResonanceCatalog.RankThresholds[^1];
        var next = Math.Min(
            maximum,
            Experience + StellarResonanceCatalog.ExperienceFor(kind)
        );
        if (next == Experience)
        {
            return 0;
        }

        var applied = next - Experience;
        Experience = next;
        Changed?.Invoke();
        return applied;
    }

    public int RecordPostgameMilestone(string milestoneId, int experience)
    {
        if (!MainStoryCompleted ||
            string.IsNullOrWhiteSpace(milestoneId) ||
            !_completedMilestoneIds.Add(milestoneId))
        {
            return 0;
        }

        var maximum = StellarResonanceCatalog.RankThresholds[^1];
        var next = Math.Min(maximum, Experience + Math.Max(0, experience));
        var applied = next - Experience;
        Experience = next;
        Changed?.Invoke();
        return applied;
    }

    public StellarResonanceSave Capture() => new()
    {
        MainStoryCompleted = MainStoryCompleted,
        CompletionDay = CompletionDay,
        Experience = Experience,
        CompletedMilestoneIds = _completedMilestoneIds
            .Order(StringComparer.Ordinal)
            .ToList()
    };

    public static StellarResonanceSave NormalizeSave(
        StellarResonanceSave? save,
        bool starGateActivated,
        int currentDay
    )
    {
        var completed = starGateActivated && save?.MainStoryCompleted == true;
        if (!completed)
        {
            return new StellarResonanceSave();
        }

        return new StellarResonanceSave
        {
            MainStoryCompleted = true,
            CompletionDay = Math.Clamp(
                save?.CompletionDay ?? 1,
                1,
                Math.Max(1, currentDay)
            ),
            Experience = Math.Clamp(
                save?.Experience ?? 0,
                0,
                StellarResonanceCatalog.RankThresholds[^1]
            ),
            CompletedMilestoneIds = (save?.CompletedMilestoneIds ?? [])
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList()
        };
    }

    public static int RankForExperience(int experience)
    {
        var normalized = Math.Max(0, experience);
        for (var index = StellarResonanceCatalog.RankThresholds.Count - 1;
             index >= 0;
             index--)
        {
            if (normalized >= StellarResonanceCatalog.RankThresholds[index])
            {
                return index;
            }
        }

        return 0;
    }
}
