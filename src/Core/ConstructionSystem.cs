namespace Luminfield.Core;

public enum ConstructionPhase
{
    NotStarted,
    InProgress,
    Completed
}

public sealed record ConstructionProjectDefinition(
    string Id,
    string NameKey,
    string DescriptionKey,
    int CoinCost,
    IReadOnlyList<CraftingIngredient> Materials,
    int RequiredNights,
    string? RequiredProjectId = null,
    string? PrerequisiteFailureKey = null,
    IReadOnlyList<string>? RequiredProjectIds = null
);

public static class ConstructionCatalog
{
    public const string CottageFirstUpgradeId = "cottage_first_upgrade";
    public const string CottageSecondUpgradeId = "cottage_second_upgrade";
    public const string HomesteadWorkshopProjectId = "homestead_workshop";
    public const string HomesteadGreenhouseProjectId =
        "homestead_greenhouse";
    public const string HomesteadStarfeatherCoopProjectId =
        "homestead_starfeather_coop";
    public const string HomesteadMoonfleeceBarnProjectId =
        "homestead_moonfleece_barn";
    public const string HomesteadLivestockAutomationProjectId =
        "homestead_livestock_automation";
    public const string SixfoldStarGateProjectId =
        "sixfold_star_gate";

    public static ConstructionProjectDefinition CottageFirstUpgrade { get; } =
        new(
            CottageFirstUpgradeId,
            "construction.cottage_first_upgrade.name",
            "construction.cottage_first_upgrade.description",
            240,
            Array.AsReadOnly(
            [
                new CraftingIngredient(DataCatalog.LumenwoodId, 12),
                new CraftingIngredient(DataCatalog.CrystalShardId, 4)
            ]),
            2
        );

    public static ConstructionProjectDefinition CottageSecondUpgrade
        { get; } = new(
        CottageSecondUpgradeId,
        "construction.cottage_second_upgrade.name",
        "construction.cottage_second_upgrade.description",
        960,
        Array.AsReadOnly(
        [
            new CraftingIngredient(DataCatalog.LumenwoodId, 32),
            new CraftingIngredient(DataCatalog.CrystalShardId, 14)
        ]),
        4,
        PrerequisiteFailureKey:
            "construction.cottage_second_upgrade.requires_first_and_workshop",
        RequiredProjectIds: Array.AsReadOnly(
        [
            CottageFirstUpgradeId,
            HomesteadWorkshopProjectId
        ])
    );

    public static ConstructionProjectDefinition HomesteadWorkshop { get; } =
        new(
            HomesteadWorkshopProjectId,
            "construction.homestead_workshop.name",
            "construction.homestead_workshop.description",
            480,
            Array.AsReadOnly(
            [
                new CraftingIngredient(DataCatalog.LumenwoodId, 20),
                new CraftingIngredient(DataCatalog.CrystalShardId, 8)
            ]),
            3
        );

    public static ConstructionProjectDefinition HomesteadGreenhouse
        { get; } = new(
        HomesteadGreenhouseProjectId,
        "construction.homestead_greenhouse.name",
        "construction.homestead_greenhouse.description",
        720,
        Array.AsReadOnly(
        [
            new CraftingIngredient(DataCatalog.LumenwoodId, 28),
            new CraftingIngredient(DataCatalog.CrystalShardId, 12)
        ]),
        4,
        HomesteadWorkshopProjectId,
        "construction.homestead_greenhouse.requires_workshop"
    );

    public static ConstructionProjectDefinition HomesteadStarfeatherCoop
        { get; } = new(
        HomesteadStarfeatherCoopProjectId,
        "construction.homestead_starfeather_coop.name",
        "construction.homestead_starfeather_coop.description",
        420,
        Array.AsReadOnly(
        [
            new CraftingIngredient(DataCatalog.LumenwoodId, 18),
            new CraftingIngredient(DataCatalog.CrystalShardId, 6)
        ]),
        3
    );

    public static ConstructionProjectDefinition HomesteadMoonfleeceBarn
        { get; } = new(
        HomesteadMoonfleeceBarnProjectId,
        "construction.homestead_moonfleece_barn.name",
        "construction.homestead_moonfleece_barn.description",
        780,
        Array.AsReadOnly(
        [
            new CraftingIngredient(DataCatalog.LumenwoodId, 30),
            new CraftingIngredient(DataCatalog.CrystalShardId, 12)
        ]),
        4,
        HomesteadStarfeatherCoopProjectId,
        "construction.homestead_moonfleece_barn.requires_coop"
    );

    public static ConstructionProjectDefinition HomesteadLivestockAutomation
        { get; } = new(
        HomesteadLivestockAutomationProjectId,
        "construction.homestead_livestock_automation.name",
        "construction.homestead_livestock_automation.description",
        900,
        Array.AsReadOnly(
        [
            new CraftingIngredient(DataCatalog.LumenwoodId, 24),
            new CraftingIngredient(DataCatalog.CrystalShardId, 16)
        ]),
        4,
        PrerequisiteFailureKey:
            "construction.homestead_livestock_automation.requires_buildings",
        RequiredProjectIds: Array.AsReadOnly(
        [
            HomesteadWorkshopProjectId,
            HomesteadStarfeatherCoopProjectId,
            HomesteadMoonfleeceBarnProjectId
        ])
    );

    public static ConstructionProjectDefinition SixfoldStarGate { get; } =
        new(
            SixfoldStarGateProjectId,
            "construction.sixfold_star_gate.name",
            "construction.sixfold_star_gate.description",
            2400,
            Array.AsReadOnly(
            [
                new CraftingIngredient(DataCatalog.LumenwoodId, 60),
                new CraftingIngredient(DataCatalog.CrystalShardId, 20),
                new CraftingIngredient(DataCatalog.PrismheartOreId, 8),
                new CraftingIngredient(DataCatalog.StarironOreId, 12)
            ]),
            5,
            PrerequisiteFailureKey:
                "construction.sixfold_star_gate.requires_homestead",
            RequiredProjectIds: Array.AsReadOnly(
            [
                HomesteadWorkshopProjectId,
                HomesteadGreenhouseProjectId,
                CottageSecondUpgradeId
            ])
        );

    public static IReadOnlyList<ConstructionProjectDefinition> Projects
        { get; } = Array.AsReadOnly(
        [
            CottageFirstUpgrade,
            CottageSecondUpgrade,
            HomesteadWorkshop,
            HomesteadGreenhouse,
            HomesteadStarfeatherCoop,
            HomesteadMoonfleeceBarn,
            HomesteadLivestockAutomation,
            SixfoldStarGate
        ]);

    private static readonly IReadOnlyDictionary<
        string,
        ConstructionProjectDefinition
    > ProjectsById = Projects.ToDictionary(
        project => project.Id,
        StringComparer.Ordinal
    );

    public static ConstructionProjectDefinition Project(string projectId) =>
        ProjectsById.TryGetValue(projectId, out var project)
            ? project
            : throw new KeyNotFoundException(
                $"Unknown construction project '{projectId}'."
            );

    public static bool TryProject(
        string? projectId,
        out ConstructionProjectDefinition project
    ) => ProjectsById.TryGetValue(projectId ?? string.Empty, out project!);
}

public sealed class ConstructionSystem
{
    private readonly Dictionary<string, ConstructionProjectSave> _states =
        new(StringComparer.Ordinal);

    // Compatibility aliases remain explicitly scoped to the original cottage
    // project. New callers should use the project-id overloads below.
    public ConstructionProjectDefinition Project =>
        ConstructionCatalog.CottageFirstUpgrade;
    public string ProjectId => StateProjectId(
        ConstructionCatalog.CottageFirstUpgradeId
    );
    public int RemainingNights => RemainingNightsFor(
        ConstructionCatalog.CottageFirstUpgradeId
    );
    public bool IsInProgress => IsInProgressFor(
        ConstructionCatalog.CottageFirstUpgradeId
    );
    public bool IsCompleted => IsCompletedFor(
        ConstructionCatalog.CottageFirstUpgradeId
    );
    public ConstructionPhase Phase => PhaseFor(
        ConstructionCatalog.CottageFirstUpgradeId
    );

    public string? ActiveProjectId => ConstructionCatalog.Projects
        .Select(project => project.Id)
        .FirstOrDefault(IsInProgressFor);

    public event Action? Changed;

    public void Reset()
    {
        _states.Clear();
        Changed?.Invoke();
    }

    public void Restore(ConstructionSave? save)
    {
        _states.Clear();
        foreach (var state in NormalizeSave(save).Projects)
        {
            _states[state.ProjectId] = Clone(state);
        }

        Changed?.Invoke();
    }

    public ConstructionPhase PhaseFor(string projectId)
    {
        if (IsCompletedFor(projectId))
        {
            return ConstructionPhase.Completed;
        }

        return IsInProgressFor(projectId)
            ? ConstructionPhase.InProgress
            : ConstructionPhase.NotStarted;
    }

    public int RemainingNightsFor(string projectId) =>
        _states.TryGetValue(projectId, out var state) && !state.Completed
            ? state.RemainingNights
            : 0;

    public bool IsInProgressFor(string projectId) =>
        _states.TryGetValue(projectId, out var state) &&
        !state.Completed &&
        state.RemainingNights > 0;

    public bool IsCompletedFor(string projectId) =>
        _states.TryGetValue(projectId, out var state) && state.Completed;

    public ActionResult CheckStart(
        string projectId,
        Inventory inventory,
        int coins
    )
    {
        if (!ConstructionCatalog.TryProject(projectId, out var project))
        {
            return ActionResult.Fail("construction.unknown_project");
        }

        if (IsCompletedFor(projectId))
        {
            return ActionResult.Fail("construction.already_completed");
        }

        if (IsInProgressFor(projectId))
        {
            return ActionResult.Fail("construction.already_in_progress");
        }

        if (ActiveProjectId is not null)
        {
            return ActionResult.Fail(
                "construction.another_project_in_progress"
            );
        }

        var prerequisites = project.RequiredProjectIds ??
            (string.IsNullOrWhiteSpace(project.RequiredProjectId)
                ? []
                : [project.RequiredProjectId]);
        if (prerequisites.Any(required => !IsCompletedFor(required)))
        {
            return ActionResult.Fail(
                project.PrerequisiteFailureKey ??
                    "construction.prerequisite_incomplete"
            );
        }

        if (coins < project.CoinCost)
        {
            return ActionResult.Fail("construction.insufficient_coins");
        }

        var missing = project.Materials.FirstOrDefault(material =>
            inventory.Count(material.ItemId) < material.Count
        );
        if (missing?.ItemId == DataCatalog.LumenwoodId)
        {
            return ActionResult.Fail("construction.insufficient_lumenwood");
        }

        if (missing?.ItemId == DataCatalog.CrystalShardId)
        {
            return ActionResult.Fail("construction.insufficient_crystal");
        }

        if (missing is not null)
        {
            return ActionResult.Fail("construction.insufficient_materials");
        }

        return ActionResult.Success(
            messageKey: "construction.ready_to_start"
        );
    }

    public ActionResult CheckStart(Inventory inventory, int coins) =>
        CheckStart(
            ConstructionCatalog.CottageFirstUpgradeId,
            inventory,
            coins
        );

    public void BeginChecked(string projectId)
    {
        var project = ConstructionCatalog.Project(projectId);
        if (ActiveProjectId is not null || IsCompletedFor(projectId))
        {
            throw new InvalidOperationException(
                "Construction start was not checked before commit."
            );
        }

        _states[projectId] = new ConstructionProjectSave
        {
            ProjectId = projectId,
            RemainingNights = project.RequiredNights,
            Completed = false
        };
        Changed?.Invoke();
    }

    public void BeginChecked() =>
        BeginChecked(ConstructionCatalog.CottageFirstUpgradeId);

    public bool ResolveNight()
    {
        if (ActiveProjectId is not { } projectId ||
            !_states.TryGetValue(projectId, out var state))
        {
            return false;
        }

        state.RemainingNights--;
        if (state.RemainingNights == 0)
        {
            state.Completed = true;
        }

        Changed?.Invoke();
        return state.Completed;
    }

    public ConstructionSave Capture() => CreateCanonicalSave(
        ConstructionCatalog.Projects
            .Select(project => project.Id)
            .Where(_states.ContainsKey)
            .Select(projectId => Clone(_states[projectId]))
    );

    public static ConstructionSave NormalizeSave(ConstructionSave? save)
    {
        if (save is null)
        {
            return new ConstructionSave();
        }

        var candidates = new List<ConstructionProjectSave>();
        if (save.Projects is not null)
        {
            candidates.AddRange(save.Projects);
        }

        if (save.ProjectId == ConstructionCatalog.CottageFirstUpgradeId)
        {
            candidates.Add(new ConstructionProjectSave
            {
                ProjectId = save.ProjectId,
                RemainingNights = save.RemainingNights,
                Completed = save.Completed
            });
        }

        var normalizedById = new Dictionary<
            string,
            ConstructionProjectSave
        >(StringComparer.Ordinal);
        foreach (var project in ConstructionCatalog.Projects)
        {
            var valid = candidates
                .Where(candidate => candidate is not null)
                .Where(candidate => candidate.ProjectId == project.Id)
                .Select(candidate => NormalizeCandidate(candidate, project))
                .Where(candidate => candidate is not null)
                .Cast<ConstructionProjectSave>()
                .ToList();
            if (valid.Count == 0)
            {
                continue;
            }

            normalizedById[project.Id] = valid.Any(candidate =>
                candidate.Completed
            )
                ? new ConstructionProjectSave
                {
                    ProjectId = project.Id,
                    Completed = true
                }
                : new ConstructionProjectSave
                {
                    ProjectId = project.Id,
                    RemainingNights = valid.Min(candidate =>
                        candidate.RemainingNights
                    )
                };
        }

        if (normalizedById.ContainsKey(
                ConstructionCatalog.CottageSecondUpgradeId
            ) &&
            (!normalizedById.TryGetValue(
                    ConstructionCatalog.CottageFirstUpgradeId,
                    out var firstUpgrade
                ) ||
             !firstUpgrade.Completed ||
             !normalizedById.TryGetValue(
                    ConstructionCatalog.HomesteadWorkshopProjectId,
                    out var workshop
                ) ||
             !workshop.Completed))
        {
            normalizedById.Remove(
                ConstructionCatalog.CottageSecondUpgradeId
            );
        }

        var activeKept = false;
        foreach (var project in ConstructionCatalog.Projects)
        {
            if (!normalizedById.TryGetValue(project.Id, out var state) ||
                state.Completed)
            {
                continue;
            }

            if (!activeKept)
            {
                activeKept = true;
                continue;
            }

            normalizedById.Remove(project.Id);
        }

        return CreateCanonicalSave(
            ConstructionCatalog.Projects
                .Select(project => project.Id)
                .Where(normalizedById.ContainsKey)
                .Select(projectId => normalizedById[projectId])
        );
    }

    private string StateProjectId(string projectId) =>
        _states.ContainsKey(projectId) ? projectId : string.Empty;

    private static ConstructionProjectSave? NormalizeCandidate(
        ConstructionProjectSave candidate,
        ConstructionProjectDefinition project
    )
    {
        if (candidate.Completed)
        {
            return new ConstructionProjectSave
            {
                ProjectId = project.Id,
                Completed = true
            };
        }

        if (candidate.RemainingNights <= 0)
        {
            return null;
        }

        return new ConstructionProjectSave
        {
            ProjectId = project.Id,
            RemainingNights = Math.Clamp(
                candidate.RemainingNights,
                1,
                project.RequiredNights
            )
        };
    }

    private static ConstructionSave CreateCanonicalSave(
        IEnumerable<ConstructionProjectSave> states
    )
    {
        var projects = states.Select(Clone).ToList();
        var cottage = projects.FirstOrDefault(state =>
            state.ProjectId == ConstructionCatalog.CottageFirstUpgradeId
        );
        return new ConstructionSave
        {
            ProjectId = cottage?.ProjectId ?? string.Empty,
            RemainingNights = cottage?.RemainingNights ?? 0,
            Completed = cottage?.Completed ?? false,
            Projects = projects
        };
    }

    private static ConstructionProjectSave Clone(
        ConstructionProjectSave state
    ) => new()
    {
        ProjectId = state.ProjectId,
        RemainingNights = state.RemainingNights,
        Completed = state.Completed
    };
}
