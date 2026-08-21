namespace Luminfield.Core;

public enum GatheringSkillAction
{
    CollectForage,
    FellTree
}

public sealed record GatheringSpecializationDefinition(
    string Id,
    string NameKey,
    string DescriptionKey
);

public static class GatheringSkillCatalog
{
    public const string GroveWardenId = "gathering_specialization_grove_warden";
    public const string StarseekerId = "gathering_specialization_starseeker";

    public static IReadOnlyList<int> LevelThresholds { get; } =
        [0, 25, 70, 140, 240, 380];

    public static IReadOnlyDictionary<string, GatheringSpecializationDefinition>
        Specializations { get; } =
        new Dictionary<string, GatheringSpecializationDefinition>(
            StringComparer.Ordinal
        )
        {
            [GroveWardenId] = new(
                GroveWardenId,
                "gathering.specialization.grove_warden.name",
                "gathering.specialization.grove_warden.description"
            ),
            [StarseekerId] = new(
                StarseekerId,
                "gathering.specialization.starseeker.name",
                "gathering.specialization.starseeker.description"
            )
        };

    public static int ExperienceFor(GatheringSkillAction action) => action switch
    {
        GatheringSkillAction.CollectForage => 8,
        GatheringSkillAction.FellTree => 6,
        _ => 0
    };
}

public sealed class GatheringSkillSystem
{
    public const int SpecializationUnlockLevel = 3;

    public int Experience { get; private set; }
    public int Level => LevelForExperience(Experience);
    public int MaximumLevel => GatheringSkillCatalog.LevelThresholds.Count - 1;
    public bool IsMaximumLevel => Level >= MaximumLevel;
    public string SpecializationId { get; private set; } = string.Empty;
    public bool CanChooseSpecialization =>
        Level >= SpecializationUnlockLevel &&
        string.IsNullOrWhiteSpace(SpecializationId);
    public int ForageYieldBonus =>
        SpecializationId == GatheringSkillCatalog.StarseekerId ? 1 : 0;
    public int LumberYieldBonus =>
        SpecializationId == GatheringSkillCatalog.GroveWardenId ? 1 : 0;

    public event Action? Changed;

    public void Reset()
    {
        Experience = 0;
        SpecializationId = string.Empty;
        Changed?.Invoke();
    }

    public void Restore(GatheringSkillSave? save)
    {
        var normalized = NormalizeSave(save);
        Experience = normalized.Experience;
        SpecializationId = normalized.SpecializationId;
        Changed?.Invoke();
    }

    public int RecordSuccessfulAction(GatheringSkillAction action)
    {
        var maximum = GatheringSkillCatalog.LevelThresholds[^1];
        var next = Math.Min(
            maximum,
            Experience + GatheringSkillCatalog.ExperienceFor(action)
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

    public ActionResult ChooseSpecialization(string specializationId)
    {
        if (!GatheringSkillCatalog.Specializations.ContainsKey(specializationId))
        {
            return ActionResult.Fail("gathering.specialization.unknown");
        }
        if (!string.IsNullOrWhiteSpace(SpecializationId))
        {
            return ActionResult.Fail("gathering.specialization.already_chosen");
        }
        if (Level < SpecializationUnlockLevel)
        {
            return ActionResult.Fail("gathering.specialization.locked");
        }

        SpecializationId = specializationId;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "gathering.specialization.chosen"
        );
    }

    public GatheringSkillSave Capture() => new()
    {
        Experience = Experience,
        SpecializationId = SpecializationId
    };

    public static GatheringSkillSave NormalizeSave(GatheringSkillSave? save)
    {
        var experience = Math.Clamp(
            save?.Experience ?? 0,
            0,
            GatheringSkillCatalog.LevelThresholds[^1]
        );
        var specializationId = save?.SpecializationId ?? string.Empty;
        var validSpecialization =
            GatheringSkillCatalog.Specializations.ContainsKey(
                specializationId
            ) &&
            LevelForExperience(experience) >= SpecializationUnlockLevel;

        return new GatheringSkillSave
        {
            Experience = experience,
            SpecializationId = validSpecialization
                ? specializationId
                : string.Empty
        };
    }

    public static int LevelForExperience(int experience)
    {
        var normalized = Math.Max(0, experience);
        for (var index = GatheringSkillCatalog.LevelThresholds.Count - 1;
             index >= 0;
             index--)
        {
            if (normalized >= GatheringSkillCatalog.LevelThresholds[index])
            {
                return index;
            }
        }

        return 0;
    }
}
