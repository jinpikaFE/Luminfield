namespace Luminfield.Core;

public enum FarmingSkillAction
{
    Till,
    Plant,
    Water,
    Harvest
}

public sealed record FarmingSkillLevelDefinition(
    int Level,
    int RequiredExperience
);

public sealed record FarmingSkillActionDefinition(
    FarmingSkillAction Action,
    int Experience
);

public sealed record FarmingSpecializationDefinition(
    string Id,
    string NameKey,
    string DescriptionKey
);

public static class FarmingSkillCatalog
{
    public const string DewkeeperId = "dewkeeper";
    public const string ResonanceScholarId = "resonance_scholar";

    public static readonly IReadOnlyList<FarmingSkillLevelDefinition> Levels =
    [
        new(0, 0),
        new(1, 20),
        new(2, 50),
        new(3, 100),
        new(4, 175),
        new(5, 275)
    ];

    public static readonly IReadOnlyDictionary<
        FarmingSkillAction,
        FarmingSkillActionDefinition
    > Actions = new Dictionary<FarmingSkillAction, FarmingSkillActionDefinition>
    {
        [FarmingSkillAction.Till] = new(FarmingSkillAction.Till, 2),
        [FarmingSkillAction.Plant] = new(FarmingSkillAction.Plant, 3),
        [FarmingSkillAction.Water] = new(FarmingSkillAction.Water, 1),
        [FarmingSkillAction.Harvest] = new(FarmingSkillAction.Harvest, 8)
    };

    public static readonly IReadOnlyDictionary<
        string,
        FarmingSpecializationDefinition
    > Specializations = new Dictionary<string, FarmingSpecializationDefinition>(
        StringComparer.Ordinal
    )
    {
        [DewkeeperId] = new(
            DewkeeperId,
            "farming.specialization.dewkeeper.name",
            "farming.specialization.dewkeeper.description"
        ),
        [ResonanceScholarId] = new(
            ResonanceScholarId,
            "farming.specialization.resonance_scholar.name",
            "farming.specialization.resonance_scholar.description"
        )
    };
}

public sealed class FarmingSkillSystem
{
    public const int SpecializationUnlockLevel = 3;
    public const int BaseWateringEnergyCost = 2;

    private int _experience;
    private string _specializationId = string.Empty;

    public int Experience => _experience;
    public string SpecializationId => _specializationId;
    public int Level => LevelForExperience(_experience);
    public int MaximumLevel => FarmingSkillCatalog.Levels[^1].Level;
    public bool IsMaximumLevel => Level >= MaximumLevel;
    public bool CanChooseSpecialization =>
        Level >= SpecializationUnlockLevel &&
        string.IsNullOrWhiteSpace(_specializationId);
    public int LevelStartExperience =>
        FarmingSkillCatalog.Levels[Level].RequiredExperience;
    public int ExperienceIntoLevel => _experience - LevelStartExperience;
    public int ExperienceForCurrentLevel => IsMaximumLevel
        ? 0
        : FarmingSkillCatalog.Levels[Level + 1].RequiredExperience -
            LevelStartExperience;
    public int WateringEnergyCost => _specializationId == FarmingSkillCatalog.DewkeeperId
        ? Math.Max(1, BaseWateringEnergyCost - 1)
        : BaseWateringEnergyCost;

    public event Action? Changed;

    public void Reset()
    {
        _experience = 0;
        _specializationId = string.Empty;
        Changed?.Invoke();
    }

    public void Restore(FarmingSkillSave? save)
    {
        var normalized = NormalizeSave(save);
        _experience = normalized.Experience;
        _specializationId = normalized.SpecializationId;
        Changed?.Invoke();
    }

    public int ExperienceFor(FarmingSkillAction action)
    {
        var baseExperience = FarmingSkillCatalog.Actions[action].Experience;
        if (action != FarmingSkillAction.Harvest ||
            _specializationId != FarmingSkillCatalog.ResonanceScholarId)
        {
            return baseExperience;
        }

        return baseExperience + baseExperience / 2;
    }

    public int RecordSuccessfulAction(FarmingSkillAction action)
    {
        var awarded = ExperienceFor(action);
        var maximumExperience = FarmingSkillCatalog.Levels[^1]
            .RequiredExperience;
        var nextExperience = Math.Min(
            maximumExperience,
            _experience + awarded
        );
        if (nextExperience == _experience)
        {
            return 0;
        }

        var applied = nextExperience - _experience;
        _experience = nextExperience;
        Changed?.Invoke();
        return applied;
    }

    public ActionResult ChooseSpecialization(string specializationId)
    {
        if (!FarmingSkillCatalog.Specializations.ContainsKey(specializationId))
        {
            return ActionResult.Fail("farming.specialization.invalid");
        }

        if (!string.IsNullOrWhiteSpace(_specializationId))
        {
            return ActionResult.Fail("farming.specialization.already_chosen");
        }

        if (Level < SpecializationUnlockLevel)
        {
            return ActionResult.Fail("farming.specialization.locked");
        }

        _specializationId = specializationId;
        Changed?.Invoke();
        return ActionResult.Success(
            messageKey: "farming.specialization.chosen"
        );
    }

    public FarmingSkillSave Capture() => new()
    {
        Experience = _experience,
        SpecializationId = _specializationId
    };

    public static FarmingSkillSave NormalizeSave(FarmingSkillSave? save)
    {
        var maximumExperience = FarmingSkillCatalog.Levels[^1]
            .RequiredExperience;
        var experience = Math.Clamp(
            save?.Experience ?? 0,
            0,
            maximumExperience
        );
        var specializationId = save?.SpecializationId ?? string.Empty;
        var specializationIsValid =
            FarmingSkillCatalog.Specializations.ContainsKey(specializationId) &&
            LevelForExperience(experience) >= SpecializationUnlockLevel;
        return new FarmingSkillSave
        {
            Experience = experience,
            SpecializationId = specializationIsValid
                ? specializationId
                : string.Empty
        };
    }

    public static int LevelForExperience(int experience)
    {
        var normalizedExperience = Math.Max(0, experience);
        for (var index = FarmingSkillCatalog.Levels.Count - 1; index >= 0; index--)
        {
            if (normalizedExperience >=
                FarmingSkillCatalog.Levels[index].RequiredExperience)
            {
                return FarmingSkillCatalog.Levels[index].Level;
            }
        }

        return 0;
    }
}
