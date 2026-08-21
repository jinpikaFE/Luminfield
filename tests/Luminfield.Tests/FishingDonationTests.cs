using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class FishingDonationTests
{
    [Fact]
    public void DonationRequiresArchiveHandDiscoveryAndInventoryAtomically()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.True(session.Inventory.Add(DataCatalog.PondglowMinnowId, 1));

        var outside = session.DonateFishToArchive(DataCatalog.PondglowMinnowId);
        Assert.False(outside.Succeeded);
        Assert.Equal(1, session.Inventory.Count(DataCatalog.PondglowMinnowId));

        var save = session.Capture();
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Fishing.CaughtFishIds = [DataCatalog.PondglowMinnowId];
        session.Restore(save);
        session.Inventory.Select(5);
        var wrongTool = session.DonateFishToArchive(
            DataCatalog.PondglowMinnowId
        );
        Assert.False(wrongTool.Succeeded);
        Assert.Equal("notice.needs_hand", wrongTool.MessageKey);

        session.Inventory.Select(0);
        var donated = session.DonateFishToArchive(
            DataCatalog.PondglowMinnowId
        );
        Assert.True(donated.Succeeded);
        Assert.Equal(0, session.Inventory.Count(DataCatalog.PondglowMinnowId));
        Assert.True(session.Fishing.IsDonated(DataCatalog.PondglowMinnowId));

        var duplicate = session.DonateFishToArchive(
            DataCatalog.PondglowMinnowId
        );
        Assert.False(duplicate.Succeeded);
        Assert.Equal("fishing.donation.already_donated", duplicate.MessageKey);
    }

    [Fact]
    public void DonationEntriesExposeDiscoveryInventoryAndPersistentState()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Player.LocationId = PlayerLocationIds.MoonlitArchive;
        save.Fishing.CaughtFishIds =
        [
            DataCatalog.PondglowMinnowId,
            DataCatalog.CrystalfinDaceId
        ];
        save.Fishing.DonatedFishIds = [DataCatalog.PondglowMinnowId];
        session.Restore(save);
        Assert.True(session.Inventory.Add(DataCatalog.CrystalfinDaceId, 3));

        var entries = session.FishingDonationEntries();
        var donated = Assert.Single(entries, entry =>
            entry.Fish.Id == DataCatalog.PondglowMinnowId
        );
        Assert.True(donated.Caught);
        Assert.True(donated.Donated);
        var ready = Assert.Single(entries, entry =>
            entry.Fish.Id == DataCatalog.CrystalfinDaceId
        );
        Assert.False(ready.Donated);
        Assert.Equal(3, ready.OwnedCount);

        var restored = new GameSession();
        restored.Restore(session.Capture());
        Assert.True(restored.Fishing.IsDonated(DataCatalog.PondglowMinnowId));
    }

    [Fact]
    public void SaveLoadFiltersUnknownAndDuplicateDonationIds()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"luminfield-fish-donation-{Guid.NewGuid():N}.json"
        );
        try
        {
            var session = new GameSession();
            session.NewGame();
            var save = session.Capture();
            save.Fishing.DonatedFishIds =
            [
                DataCatalog.PondglowMinnowId,
                "unknown_fish",
                DataCatalog.PondglowMinnowId
            ];
            File.WriteAllText(path, JsonSerializer.Serialize(save));

            var result = new SaveService(path).Load();

            Assert.Equal(SaveLoadStatus.Loaded, result.Status);
            Assert.NotNull(result.Save);
            Assert.Equal(
                [DataCatalog.PondglowMinnowId],
                result.Save.Fishing.DonatedFishIds
            );
        }
        finally
        {
            File.Delete(path);
        }
    }
}
