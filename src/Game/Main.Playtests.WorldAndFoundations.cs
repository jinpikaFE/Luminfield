using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class Main : Node
{
    private void StartWorldPlaytest()
    {
        StartWorldScenicPlaytest(VillageCatalog.VillageCenterCell);
    }

    private void StartWorldBeginnerArchPlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(52, 52));

    private void StartWorldWoodsGrovePlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(52, 116));

    private void StartWorldMeadowCirclePlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(136, 17));

    private void StartWorldCrystalRidgePlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(92, 178));

    private void StartWorldWetlandIsletPlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(214, 60));

    private void StartWorldRuinsColonnadePlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(184, 182));

    private void StartWorldFacilitiesGatewayPlaytest() =>
        StartWorldScenicPlaytest(new GridPosition(128, 110));

    private void StartWorldScenicPlaytest(GridPosition playerCell)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var safeCell = WorldDefinition.NearestWalkableCell(playerCell);
        _session.SetPlayerState(
            safeCell.X * 16 + 8,
            safeCell.Y * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartWorldAspectBoundaryPlaytest() =>
        StartWorldAspectPlaytest(14);

    private void StartRainveilWorldAspectPlaytest() =>
        StartWorldAspectPlaytest(15);

    private void StartStarharvestWorldAspectPlaytest() =>
        StartWorldAspectPlaytest(29);

    private void StartLongnightWorldAspectPlaytest() =>
        StartWorldAspectPlaytest(43);

    private void StartRainveilWorldTreeRainPlaytest() =>
        StartWorldAspectResourcePlaytest(
            15,
            DataCatalog.RainWeatherId,
            WorldResourceKind.Tree,
            2
        );

    private void StartStarharvestWorldCrystalStardustPlaytest() =>
        StartWorldAspectResourcePlaytest(
            29,
            DataCatalog.StardustWindWeatherId,
            WorldResourceKind.Crystal,
            2
        );

    private void StartWorldAspectPlaytest(int day)
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var playerCell = new GridPosition(70, 64);
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = 14 * 60;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = DataCatalog.ClearWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = playerCell.X * 16 + 8;
        save.Player.Y = playerCell.Y * 16 + 8;
        save.Player.SelectedSlot = 0;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartWorldAspectResourcePlaytest(
        int day,
        string weatherId,
        WorldResourceKind resourceKind,
        int selectedSlot
    )
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var resource = FindResourceWithNorthernApproach(resourceKind);
        var playerCell = new GridPosition(resource.X, resource.Y - 1);
        var save = _session.Capture();
        save.Day = day;
        save.MinuteOfDay = 14 * 60;
        save.Weather = new WeatherSave
        {
            Day = day,
            CurrentId = weatherId,
            ForecastId = weatherId
        };
        save.Player.LocationId = PlayerLocationIds.World;
        save.Player.X = playerCell.X * 16 + 8;
        save.Player.Y = playerCell.Y * 16 + 8;
        save.Player.SelectedSlot = selectedSlot;
        _session.Restore(save);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartGatePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.SetPlayerState(
            19 * 16 + 8,
            30 * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartBackpackPlaytest()
    {
        StartNewGame();
        foreach (var seedId in DataCatalog.SeedItemIds)
        {
            _session.Inventory.Add(seedId, 7);
        }
        foreach (var cropId in DataCatalog.CropIds)
        {
            _session.Inventory.Add(cropId, 3);
        }
        _session.Inventory.Add(DataCatalog.LumenwoodId, 8);
        _session.Inventory.Add(DataCatalog.CrystalShardId, 3);
        Callable.From(OpenBackpack).CallDeferred();
    }

    private void StartResourcePlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var resource = FindResourceWithNorthernApproach(WorldResourceKind.Tree);
        _session.SetPlayerState(
            resource.X * 16 + 8,
            (resource.Y - 1) * 16 + 8,
            false
        );
        _session.Inventory.Select(2);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartTargetPreviewPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.SetPlayerState(12 * 16 + 8, 15 * 16 + 8, false);
        _session.Inventory.Select(1);
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartPhaseAPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.StarbudId, 5);
        _session.Inventory.Add(DataCatalog.MoonrootId, 3);
        _session.Inventory.Add(DataCatalog.StarbudPreserveId, 1);
        _session.SetPlayerState(
            FarmView.ShippingCell.X * 16 + 8,
            (FarmView.ShippingCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(OpenShipping).CallDeferred();
    }

    private void StartPhaseASummaryPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        _session.Inventory.Add(DataCatalog.StarbudId, 3);
        _session.Inventory.Add(DataCatalog.MoonrootId, 2);
        _session.Inventory.Add(DataCatalog.StarbudPreserveId, 1);
        _session.QueueForShipping(DataCatalog.StarbudId);
        _session.QueueForShipping(DataCatalog.StarbudId);
        _session.QueueForShipping(DataCatalog.MoonrootId);
        _session.QueueForShipping(DataCatalog.StarbudPreserveId);
        _session.EndDay();
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(ShowNightlySummary).CallDeferred();
    }

    private void StartPhaseARainPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var save = _session.Capture();
        save.Day = 2;
        save.Weather = new WeatherSave
        {
            Day = 2,
            CurrentId = DataCatalog.RainWeatherId,
            ForecastId = DataCatalog.ClearWeatherId
        };
        _session.Restore(save);
        _session.Inventory.Add(DataCatalog.StarbudId, 1);
        _session.QueueForShipping(DataCatalog.StarbudId);
        _session.SetPlayerState(
            FarmView.ShippingCell.X * 16 + 8,
            (FarmView.ShippingCell.Y + 1) * 16 + 8,
            false
        );
        _playing = true;
        EnsureHud();
        ShowFarm(false);
    }

    private void StartResourceRespawnPlaytest()
    {
        FreeUi(_title);
        _title = null;
        _session.NewGame(_locale.CurrentLocale);
        var crystal = FindResourceWithNorthernApproach(WorldResourceKind.Crystal);
        _session.Inventory.Select(1);
        _session.UseSelected(crystal);
        _session.EndDay();
        _session.EndDay();
        _playing = true;
        EnsureHud();
        ShowFarm(false);
        Callable.From(ShowNightlySummary).CallDeferred();
    }

    private static GridPosition FindResourceWithNorthernApproach(
        WorldResourceKind resourceKind
    )
    {
        for (var y = FarmSystem.MapHeight + 1; y < WorldDefinition.Height - 1; y++)
        {
            for (var x = 1; x < WorldDefinition.Width - 1; x++)
            {
                var resource = new GridPosition(x, y);
                var approach = new GridPosition(x, y - 1);
                if (WorldDefinition.ResourceAt(resource) == resourceKind &&
                    !WorldDefinition.IsBlocked(approach))
                {
                    return resource;
                }
            }
        }

        throw new InvalidOperationException(
            $"No approachable world resource found for {resourceKind}."
        );
    }

    private static FarmTileState CropState(int x, int y, string cropId, int wateredNights) =>
        new()
        {
            X = x,
            Y = y,
            Tilled = true,
            CropId = cropId,
            WateredNights = wateredNights
        };

}
