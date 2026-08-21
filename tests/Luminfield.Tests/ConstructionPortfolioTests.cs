using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class ConstructionPortfolioTests
{
    [Fact]
    public void CatalogUsesStableUniqueIdsAndValidProjectDefinitions()
    {
        Assert.Equal(8, ConstructionCatalog.Projects.Count);
        Assert.Equal(
            ConstructionCatalog.Projects.Count,
            ConstructionCatalog.Projects
                .Select(project => project.Id)
                .Distinct(StringComparer.Ordinal)
                .Count()
        );
        Assert.Equal(
            "homestead_workshop",
            ConstructionCatalog.HomesteadWorkshopProjectId
        );

        foreach (var project in ConstructionCatalog.Projects)
        {
            Assert.False(string.IsNullOrWhiteSpace(project.Id));
            Assert.True(project.CoinCost > 0);
            Assert.True(project.RequiredNights > 0);
            Assert.NotEmpty(project.Materials);
            Assert.All(project.Materials, material =>
            {
                Assert.True(DataCatalog.Items.ContainsKey(material.ItemId));
                Assert.True(material.Count > 0);
            });
        }
    }

    [Fact]
    public void LegacyCottageRootMigratesIntoProjectsAndRemainsMirrored()
    {
        var normalized = ConstructionSystem.NormalizeSave(
            new ConstructionSave
            {
                ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
                RemainingNights = 99
            }
        );

        Assert.Equal(
            ConstructionCatalog.CottageFirstUpgradeId,
            normalized.ProjectId
        );
        Assert.Equal(2, normalized.RemainingNights);
        Assert.False(normalized.Completed);
        var migrated = Assert.Single(normalized.Projects);
        Assert.Equal(normalized.ProjectId, migrated.ProjectId);
        Assert.Equal(normalized.RemainingNights, migrated.RemainingNights);

        var system = new ConstructionSystem();
        system.Restore(normalized);
        var captured = system.Capture();
        Assert.Equal(normalized.ProjectId, captured.ProjectId);
        Assert.Equal(normalized.RemainingNights, captured.RemainingNights);
        Assert.Equal(normalized.Completed, captured.Completed);
        Assert.Equal(
            ConstructionCatalog.CottageFirstUpgradeId,
            system.ActiveProjectId
        );
    }

    [Fact]
    public void NewProjectsNeverUseTheLegacyCottageRoot()
    {
        var normalized = ConstructionSystem.NormalizeSave(
            new ConstructionSave
            {
                ProjectId = ConstructionCatalog.HomesteadWorkshopProjectId,
                RemainingNights = 2,
                Projects =
                [
                    new ConstructionProjectSave
                    {
                        ProjectId = ConstructionCatalog
                            .HomesteadWorkshopProjectId,
                        RemainingNights = 2
                    }
                ]
            }
        );

        Assert.Empty(normalized.ProjectId);
        Assert.Equal(0, normalized.RemainingNights);
        Assert.False(normalized.Completed);
        Assert.Equal(
            ConstructionCatalog.HomesteadWorkshopProjectId,
            Assert.Single(normalized.Projects).ProjectId
        );
    }

    [Fact]
    public void NormalizationMergesDuplicatesAndKeepsOneDeterministicActive()
    {
        var normalized = ConstructionSystem.NormalizeSave(
            new ConstructionSave
            {
                ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
                RemainingNights = 2,
                Projects =
                [
                    new ConstructionProjectSave
                    {
                        ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
                        RemainingNights = 1
                    },
                    new ConstructionProjectSave
                    {
                        ProjectId = ConstructionCatalog
                            .HomesteadWorkshopProjectId,
                        RemainingNights = 2
                    },
                    new ConstructionProjectSave
                    {
                        ProjectId = "removed_project",
                        Completed = true
                    }
                ]
            }
        );

        var active = Assert.Single(normalized.Projects);
        Assert.Equal(
            ConstructionCatalog.CottageFirstUpgradeId,
            active.ProjectId
        );
        Assert.Equal(1, active.RemainingNights);

        var completedWins = ConstructionSystem.NormalizeSave(
            new ConstructionSave
            {
                Projects =
                [
                    new ConstructionProjectSave
                    {
                        ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
                        Completed = true,
                        RemainingNights = 99
                    },
                    new ConstructionProjectSave
                    {
                        ProjectId = ConstructionCatalog
                            .HomesteadWorkshopProjectId,
                        RemainingNights = 99
                    },
                    new ConstructionProjectSave
                    {
                        ProjectId = ConstructionCatalog
                            .HomesteadWorkshopProjectId,
                        RemainingNights = 2
                    }
                ]
            }
        );

        Assert.Equal(2, completedWins.Projects.Count);
        Assert.True(completedWins.Completed);
        Assert.Equal(0, completedWins.RemainingNights);
        Assert.Equal(
            2,
            completedWins.Projects.Single(project =>
                project.ProjectId == ConstructionCatalog
                    .HomesteadWorkshopProjectId
            ).RemainingNights
        );
    }

    [Fact]
    public void GenericStartIsAtomicAndEachNightAdvancesOnlyTheActiveProject()
    {
        var session = PreparedWorkshopSession(
            coins: 480,
            lumenwood: 20,
            crystal: 8
        );
        var snapshots = new List<string>();
        session.Changed += () => snapshots.Add(Snapshot(session));

        var started = session.StartConstruction(
            ConstructionCatalog.HomesteadWorkshopProjectId
        );

        Assert.True(started.Succeeded);
        Assert.Equal(0, session.Coins);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.LumenwoodId));
        Assert.Equal(0, session.Inventory.Count(DataCatalog.CrystalShardId));
        Assert.Equal(
            ConstructionCatalog.HomesteadWorkshopProjectId,
            session.Construction.ActiveProjectId
        );
        Assert.Equal(
            3,
            session.Construction.RemainingNightsFor(
                ConstructionCatalog.HomesteadWorkshopProjectId
            )
        );
        Assert.Single(snapshots);

        session.EndDay();
        Assert.Equal(
            2,
            session.Construction.RemainingNightsFor(
                ConstructionCatalog.HomesteadWorkshopProjectId
            )
        );
        Assert.Equal(
            ConstructionPhase.NotStarted,
            session.Construction.PhaseFor(
                ConstructionCatalog.CottageFirstUpgradeId
            )
        );
        session.EndDay();
        session.EndDay();
        Assert.True(session.Construction.IsCompletedFor(
            ConstructionCatalog.HomesteadWorkshopProjectId
        ));
        Assert.Null(session.Construction.ActiveProjectId);

        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, 12));
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, 4));
        var nextProject = session.Capture();
        nextProject.Coins = 240;
        session.Restore(nextProject);
        SetAdjacentToHomesteadWorkbench(session);
        Assert.True(session.StartCottageFirstUpgrade().Succeeded);
        Assert.True(session.Construction.IsCompletedFor(
            ConstructionCatalog.HomesteadWorkshopProjectId
        ));
        Assert.Equal(
            ConstructionCatalog.CottageFirstUpgradeId,
            session.Construction.ActiveProjectId
        );

        var captured = session.Capture();
        Assert.Equal(SaveService.CurrentSchemaVersion, captured.SchemaVersion);
        Assert.Equal(
            ConstructionCatalog.CottageFirstUpgradeId,
            captured.Construction.ProjectId
        );
        Assert.Equal(2, captured.Construction.Projects.Count);
        var homestead = captured.Construction.Projects.Single(project =>
            project.ProjectId == ConstructionCatalog
                .HomesteadWorkshopProjectId
        );
        Assert.True(homestead.Completed);
    }

    [Fact]
    public void ASecondActiveProjectIsRejectedWithoutAnyMutation()
    {
        var session = PreparedWorkshopSession(
            coins: 1000,
            lumenwood: 40,
            crystal: 20
        );
        Assert.True(session.StartCottageFirstUpgrade().Succeeded);
        var before = Snapshot(session);
        var changed = 0;
        session.Changed += () => changed++;

        Assert.Throws<InvalidOperationException>(() =>
            session.Construction.BeginChecked(
                ConstructionCatalog.HomesteadWorkshopProjectId
            )
        );
        Assert.Equal(before, Snapshot(session));
        Assert.Equal(0, changed);

        var result = session.StartConstruction(
            ConstructionCatalog.HomesteadWorkshopProjectId
        );

        Assert.False(result.Succeeded);
        Assert.Equal(
            "construction.another_project_in_progress",
            result.MessageKey
        );
        Assert.Equal(before, Snapshot(session));
        Assert.Equal(0, changed);
    }

    [Fact]
    public void GenericStartFailurePathsLeaveTheWholeSaveUnchanged()
    {
        var unknown = PreparedWorkshopSession(1000, 40, 20);
        AssertUnchangedFailure(
            unknown,
            () => unknown.StartConstruction("removed_project"),
            "construction.unknown_project"
        );

        var insufficient = PreparedWorkshopSession(479, 20, 8);
        AssertUnchangedFailure(
            insufficient,
            () => insufficient.StartConstruction(
                ConstructionCatalog.HomesteadWorkshopProjectId
            ),
            "construction.insufficient_coins"
        );

        var wrongLocation = PreparedWorkshopSession(480, 20, 8);
        wrongLocation.SetPlayerLocation(
            39 * 16 + 8,
            9 * 16 + 8,
            PlayerLocationIds.World
        );
        AssertUnchangedFailure(
            wrongLocation,
            () => wrongLocation.StartConstruction(
                ConstructionCatalog.HomesteadWorkshopProjectId
            ),
            "construction.workshop_only"
        );

        var unfinishedHome = PreparedWorkshopSession(480, 20, 8);
        SetAdjacentToHomesteadWorkbench(unfinishedHome);
        AssertUnchangedFailure(
            unfinishedHome,
            () => unfinishedHome.StartConstruction(
                ConstructionCatalog.HomesteadWorkshopProjectId
            ),
            "construction.homestead_workshop.not_started"
        );
    }

    [Fact]
    public void CompletedHomesteadWorkbenchSharesOnePurePreviewActionCheck()
    {
        var session = new GameSession();
        session.NewGame();
        SetAdjacentToHomesteadWorkbench(session);
        var beforeConstruction = Snapshot(session);

        var blocked = session.PreviewSelectedTarget(
            FarmLayout.HomesteadWorkbenchCell
        );
        var blockedAction = session.OpenHomesteadWorkbench(
            FarmLayout.HomesteadWorkbenchCell
        );

        Assert.Equal(TargetPreviewState.Blocked, blocked.State);
        Assert.Equal(TargetPreviewKind.HomesteadWorkshop, blocked.Kind);
        Assert.Equal(
            "construction.homestead_workshop.not_started",
            blocked.LabelKey
        );
        Assert.False(blockedAction.Succeeded);
        Assert.Equal(
            "construction.homestead_workshop.not_started",
            blockedAction.MessageKey
        );
        Assert.Equal(beforeConstruction, Snapshot(session));

        RestoreInProgressHomesteadWorkshop(session);
        SetAdjacentToHomesteadWorkbench(session);
        var duringConstruction = Snapshot(session);
        var inProgress = session.PreviewSelectedTarget(
            FarmLayout.HomesteadWorkbenchCell
        );
        var inProgressAction = session.OpenHomesteadWorkbench(
            FarmLayout.HomesteadWorkbenchCell
        );
        Assert.Equal(TargetPreviewState.Blocked, inProgress.State);
        Assert.Equal(
            "construction.homestead_workshop.in_progress",
            inProgress.LabelKey
        );
        Assert.False(inProgressAction.Succeeded);
        Assert.Equal(
            "construction.homestead_workshop.in_progress",
            inProgressAction.MessageKey
        );
        Assert.Equal(duringConstruction, Snapshot(session));

        RestoreCompletedHomesteadWorkshop(session);
        SetAdjacentToHomesteadWorkbench(session);
        var ready = Snapshot(session);
        var available = session.PreviewSelectedTarget(
            FarmLayout.HomesteadWorkbenchCell
        );
        var opened = session.UseSelected(FarmLayout.HomesteadWorkbenchCell);
        Assert.True(available.IsAvailable);
        Assert.Equal(TargetPreviewKind.HomesteadWorkshop, available.Kind);
        Assert.Equal("target.action.open_construction", available.LabelKey);
        Assert.True(opened.Succeeded);
        Assert.Equal("construction.panel.opened", opened.MessageKey);
        Assert.Equal(ready, Snapshot(session));

        session.Inventory.Select(1);
        AssertWorkbenchFailureIsPure(
            session,
            TargetPreviewState.NeedsTool,
            "notice.needs_hand"
        );

        session.Inventory.Select(0);
        session.SetPlayerLocation(
            39 * 16 + 8,
            9 * 16 + 8,
            PlayerLocationIds.World
        );
        AssertWorkbenchFailureIsPure(
            session,
            TargetPreviewState.Neutral,
            "notice.nothing_to_interact"
        );

        session.SetPlayerLocation(
            41 * 16 + 8,
            9 * 16 + 8,
            PlayerLocationIds.Cottage
        );
        AssertWorkbenchFailureIsPure(
            session,
            TargetPreviewState.Neutral,
            "notice.nothing_to_interact"
        );

        var fakeTarget = new GridPosition(
            FarmLayout.HomesteadWorkbenchCell.X,
            FarmLayout.HomesteadWorkbenchCell.Y + 1
        );
        var beforeFake = Snapshot(session);
        Assert.False(session.CheckHomesteadWorkbench(fakeTarget).Succeeded);
        Assert.Equal(beforeFake, Snapshot(session));
    }

    private static GameSession PreparedWorkshopSession(
        int coins,
        int lumenwood,
        int crystal
    )
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.LumenwoodId, lumenwood));
        Assert.True(session.Inventory.Add(DataCatalog.CrystalShardId, crystal));
        var save = session.Capture();
        save.Coins = coins;
        save.Player.LocationId = PlayerLocationIds.MoonstoneWorkshop;
        session.Restore(save);
        return session;
    }

    private static void RestoreCompletedHomesteadWorkshop(
        GameSession session
    )
    {
        var save = session.Capture();
        save.Construction = new ConstructionSave
        {
            Projects =
            [
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .HomesteadWorkshopProjectId,
                    Completed = true
                }
            ]
        };
        session.Restore(save);
    }

    private static void RestoreInProgressHomesteadWorkshop(
        GameSession session
    )
    {
        var save = session.Capture();
        save.Construction = new ConstructionSave
        {
            Projects =
            [
                new ConstructionProjectSave
                {
                    ProjectId = ConstructionCatalog
                        .HomesteadWorkshopProjectId,
                    RemainingNights = 2
                }
            ]
        };
        session.Restore(save);
    }

    private static void SetAdjacentToHomesteadWorkbench(
        GameSession session
    ) => session.SetPlayerLocation(
        (FarmLayout.HomesteadWorkbenchCell.X - 1) * 16 + 8,
        FarmLayout.HomesteadWorkbenchCell.Y * 16 + 8,
        PlayerLocationIds.World
    );

    private static void AssertUnchangedFailure(
        GameSession session,
        Func<ActionResult> action,
        string expectedMessageKey
    )
    {
        var before = Snapshot(session);
        var changed = 0;
        session.Changed += () => changed++;

        var result = action();

        Assert.False(result.Succeeded);
        Assert.Equal(expectedMessageKey, result.MessageKey);
        Assert.Equal(before, Snapshot(session));
        Assert.Equal(0, changed);
    }

    private static void AssertWorkbenchFailureIsPure(
        GameSession session,
        TargetPreviewState expectedPreviewState,
        string expectedMessageKey
    )
    {
        var before = Snapshot(session);
        var preview = session.PreviewSelectedTarget(
            FarmLayout.HomesteadWorkbenchCell
        );
        var result = session.OpenHomesteadWorkbench(
            FarmLayout.HomesteadWorkbenchCell
        );

        Assert.Equal(expectedPreviewState, preview.State);
        Assert.False(result.Succeeded);
        Assert.Equal(expectedMessageKey, result.MessageKey);
        Assert.Equal(before, Snapshot(session));
    }

    private static string Snapshot(GameSession session) =>
        JsonSerializer.Serialize(session.Capture());
}
