using System.Text.Json;
using Luminfield.Core;
using Xunit;

namespace Luminfield.Tests;

public sealed class CollectionSystemTests
{
    [Fact]
    public void CatalogOrdersEightCompleteCategoriesWithStableEntries()
    {
        Assert.Equal(
            [
                CollectionCategoryIds.Crops,
                CollectionCategoryIds.Cooking,
                CollectionCategoryIds.Artisan,
                CollectionCategoryIds.Forage,
                CollectionCategoryIds.Fish,
                CollectionCategoryIds.Minerals,
                CollectionCategoryIds.Artifacts,
                CollectionCategoryIds.Enemies
            ],
            CompendiumCatalog.CategoryIds
        );
        Assert.Equal(20, CompendiumCatalog.CropEntries.Count);
        Assert.Equal(4, CompendiumCatalog.CookingEntries.Count);
        Assert.Equal(4, CompendiumCatalog.ArtisanEntries.Count);
        Assert.Equal(8, CompendiumCatalog.ForageEntries.Count);
        Assert.Equal(24, CompendiumCatalog.FishEntries.Count);
        Assert.Equal(4, CompendiumCatalog.MineralEntries.Count);
        Assert.Equal(4, CompendiumCatalog.ArtifactEntries.Count);
        Assert.Equal(3, CompendiumCatalog.EnemyEntries.Count);
        Assert.Equal(
            DataCatalog.CookedDishItemIds,
            CompendiumCatalog.CookingEntries.Select(entry => entry.Id)
        );
        Assert.Equal(
            71,
            CompendiumCatalog.EntriesInOrder
                .Select(entry => entry.Id)
                .Distinct(StringComparer.Ordinal)
                .Count()
        );
        Assert.All(CompendiumCatalog.CookingEntries, entry =>
        {
            Assert.Equal(CompendiumEntryKind.CookedDish, entry.Kind);
            Assert.Equal(ItemKind.CookedDish, DataCatalog.Item(entry.ItemId).Kind);
            Assert.Single(
                DataCatalog.CookingRecipes.Values,
                recipe => recipe.OutputItemId == entry.ItemId
            );
        });
        Assert.Equal(
            [
                DataCatalog.StarbudPreserveId,
                DataCatalog.MoonrootTonicId,
                DataCatalog.CloudleafTeaId,
                DataCatalog.StarhoneyId
            ],
            CompendiumCatalog.ArtisanEntries.Select(entry => entry.Id)
        );
        Assert.All(CompendiumCatalog.ArtisanEntries, entry =>
        {
            Assert.Equal(CompendiumEntryKind.ArtisanGood, entry.Kind);
            Assert.Equal(ItemKind.Artisan, DataCatalog.Item(entry.ItemId).Kind);
        });
        Assert.All(CompendiumCatalog.ForageEntries, entry =>
        {
            Assert.Equal(CompendiumEntryKind.Forage, entry.Kind);
            Assert.Equal(ItemKind.Resource, DataCatalog.Item(entry.ItemId).Kind);
        });
    }

    [Fact]
    public void CropCatalogUsesAllTwentyStableCropIdsInOrder()
    {
        Assert.Equal(20, CompendiumCatalog.CropEntries.Count);
        Assert.Equal(
            DataCatalog.CropIds,
            CompendiumCatalog.CropEntries.Select(entry => entry.Id)
        );
        Assert.Equal(
            20,
            CompendiumCatalog.CropEntries
                .Select(entry => entry.Id)
                .Distinct(StringComparer.Ordinal)
                .Count()
        );

        foreach (var entry in CompendiumCatalog.CropEntries)
        {
            var crop = DataCatalog.Crop(entry.CropId);
            Assert.Equal(crop.HarvestItemId, entry.HarvestItemId);
            Assert.Equal(ItemKind.Produce, DataCatalog.Item(entry.HarvestItemId).Kind);
            Assert.Equal(ItemKind.Seed, DataCatalog.Item(entry.SeedItemId).Kind);
        }
    }

    [Fact]
    public void ProduceVariantsResolveWhileSeedsDoNot()
    {
        foreach (var crop in DataCatalog.Crops.Values)
        {
            Assert.True(CompendiumCatalog.TryResolveObtainedItem(
                crop.HarvestItemId,
                out var regular
            ));
            Assert.Equal(crop.Id, regular.Id);
            Assert.False(CompendiumCatalog.TryResolveObtainedItem(
                crop.SeedItemId,
                out _
            ));

            foreach (var itemId in DataCatalog.ItemFamilyIds(crop.HarvestItemId))
            {
                Assert.True(CompendiumCatalog.TryResolveObtainedItem(
                    itemId,
                    out var variant
                ));
                Assert.Equal(crop.Id, variant.Id);
            }
        }

        Assert.True(CompendiumCatalog.TryResolveObtainedItem(
            DataCatalog.RainwovenDawnlaceId,
            out var rainwoven
        ));
        Assert.Equal(DataCatalog.DawnlaceId, rainwoven.Id);
        Assert.True(CompendiumCatalog.TryResolveObtainedItem(
            DataCatalog.StarwindGlimmerpodId,
            out var starwind
        ));
        Assert.Equal(DataCatalog.GlimmerpodId, starwind.Id);

        foreach (var itemId in DataCatalog.CookedDishItemIds)
        {
            Assert.True(CompendiumCatalog.TryResolveObtainedItem(
                itemId,
                out var dish
            ));
            Assert.Equal(CollectionCategoryIds.Cooking, dish.CategoryId);
        }
        foreach (var itemId in CompendiumCatalog.ArtisanEntries.Select(
                     entry => entry.ItemId
                 ))
        {
            Assert.True(CompendiumCatalog.TryResolveObtainedItem(
                itemId,
                out var artisan
            ));
            Assert.Equal(CollectionCategoryIds.Artisan, artisan.CategoryId);
        }
        foreach (var itemId in new[]
                 {
                     DataCatalog.StarfeatherEggId,
                     DataCatalog.MeadowFodderId
                 })
        {
            Assert.False(CompendiumCatalog.TryResolveObtainedItem(
                itemId,
                out _
            ));
        }
    }

    [Fact]
    public void InventoryObservationDiscoversOnceAndRestoreIsSilent()
    {
        var session = new GameSession();
        session.NewGame();
        var discoveries = new List<string>();
        session.CollectionEntryDiscovered += discoveries.Add;

        Assert.True(session.Inventory.Add(DataCatalog.StarbudId, 1));
        Assert.Equal([DataCatalog.StarbudId], discoveries);
        Assert.True(session.Inventory.Add(DataCatalog.StarbudLuminousId, 1));
        Assert.Single(discoveries);
        session.Inventory.Select(1);
        Assert.Single(discoveries);

        var restored = new GameSession();
        var restoredDiscoveries = new List<string>();
        restored.CollectionEntryDiscovered += restoredDiscoveries.Add;
        restored.Restore(session.Capture());
        Assert.Empty(restoredDiscoveries);
        Assert.True(restored.Collection.IsDiscovered(DataCatalog.StarbudId));
    }

    [Fact]
    public void FailedInventoryAddDoesNotDiscover()
    {
        var session = new GameSession();
        session.NewGame();
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Distinct(StringComparer.Ordinal)
                     .Where(itemId => !DataCatalog.ItemFamilyIds(
                         DataCatalog.StarbudId
                     ).Contains(itemId, StringComparer.Ordinal))
                     .Where(itemId => DataCatalog.Item(itemId).MaxStack > 1)
                     .Take(Inventory.SlotCount - Inventory.StartingToolCount))
        {
            Assert.True(session.Inventory.Add(itemId, DataCatalog.Item(itemId).MaxStack));
        }

        Assert.Equal(
            Inventory.SlotCount,
            session.Inventory.Slots.Count(slot => !slot.IsEmpty)
        );
        Assert.False(session.Inventory.Add(DataCatalog.StarbudId, 1));
        Assert.False(session.Collection.IsDiscovered(DataCatalog.StarbudId));
    }

    [Fact]
    public void LegacyEvidenceInitializesOnceAndDoesNotAutoClaimReward()
    {
        var save = new GameSaveV1
        {
            Collection = new CollectionSave(),
            Inventory = CompendiumCatalog.CropEntries
                .Select(entry => new InventorySlot
                {
                    ItemId = entry.HarvestItemId,
                    Count = 1
                })
                .ToList()
        };

        var normalized = CollectionSystem.NormalizeSave(
            save.Collection,
            CollectionSystem.LegacyEvidenceItemIds(save)
        );
        Assert.True(normalized.Initialized);
        Assert.Equal(20, normalized.DiscoveredEntryIds.Count);
        Assert.Empty(normalized.ClaimedRewardIds);

        save.Collection = new CollectionSave
        {
            Initialized = true
        };
        var ignored = CollectionSystem.NormalizeSave(
            save.Collection,
            CollectionSystem.LegacyEvidenceItemIds(save)
        );
        Assert.Empty(ignored.DiscoveredEntryIds);
    }

    [Fact]
    public void CategoryInitializationMigratesPhaseTwentyThreeAndOlderSaves()
    {
        var evidence = new[]
        {
            DataCatalog.StarbudId,
            DataCatalog.MoonmistStewId
        };
        var phaseTwentyThree = CollectionSystem.NormalizeSave(
            new CollectionSave { Initialized = true },
            evidence
        );
        Assert.Equal(
            CompendiumCatalog.CategoryIds,
            phaseTwentyThree.InitializedCategoryIds
        );
        Assert.DoesNotContain(
            DataCatalog.StarbudId,
            phaseTwentyThree.DiscoveredEntryIds
        );
        Assert.Contains(
            DataCatalog.MoonmistStewId,
            phaseTwentyThree.DiscoveredEntryIds
        );

        var older = CollectionSystem.NormalizeSave(
            new CollectionSave(),
            evidence
        );
        Assert.Contains(DataCatalog.StarbudId, older.DiscoveredEntryIds);
        Assert.Contains(DataCatalog.MoonmistStewId, older.DiscoveredEntryIds);
        Assert.Equal(
            older.DiscoveredEntryIds,
            CollectionSystem.NormalizeSave(older, []).DiscoveredEntryIds
        );
    }

    [Fact]
    public void PhaseTwentyFourSaveBackfillsOnlyNewArtisanCategoryOnce()
    {
        var phaseTwentyFour = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds =
            [
                CollectionCategoryIds.Crops,
                CollectionCategoryIds.Cooking
            ],
            DiscoveredEntryIds = [DataCatalog.StarbudId]
        };
        var normalized = CollectionSystem.NormalizeSave(
            phaseTwentyFour,
            [
                DataCatalog.MoonmistStewId,
                DataCatalog.StarbudPreserveId,
                DataCatalog.StarhoneyId
            ]
        );

        Assert.Equal(
            CompendiumCatalog.CategoryIds,
            normalized.InitializedCategoryIds
        );
        Assert.Contains(DataCatalog.StarbudId, normalized.DiscoveredEntryIds);
        Assert.DoesNotContain(
            DataCatalog.MoonmistStewId,
            normalized.DiscoveredEntryIds
        );
        Assert.Contains(
            DataCatalog.StarbudPreserveId,
            normalized.DiscoveredEntryIds
        );
        Assert.Contains(DataCatalog.StarhoneyId, normalized.DiscoveredEntryIds);
        Assert.Equal(
            normalized.DiscoveredEntryIds,
            CollectionSystem.NormalizeSave(normalized, []).DiscoveredEntryIds
        );
    }

    [Fact]
    public void ProcessorOutputDiscoversOnlyAfterSuccessfulCollection()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Collection = InitializedCollection();
        save.Processor.Machines =
        [
            new ProcessorMachineSave
            {
                MachineId = ProcessorCatalog.PrismPreserveVatId,
                RecipeId = DataCatalog.StarbudPreserveRecipeId,
                RemainingNights = 0
            }
        ];
        session.Restore(save);

        Assert.False(session.Collection.IsDiscovered(
            DataCatalog.StarbudPreserveId
        ));
        Assert.True(session.CollectProcessedItem(
            ProcessorCatalog.PrismPreserveVatId
        ).Succeeded);
        Assert.True(session.Collection.IsDiscovered(
            DataCatalog.StarbudPreserveId
        ));

        save = session.Capture();
        save.Collection = InitializedCollection();
        save.Processor.Machines =
        [
            new ProcessorMachineSave
            {
                MachineId = ProcessorCatalog.PrismPreserveVatId,
                RecipeId = DataCatalog.StarbudPreserveRecipeId,
                RemainingNights = 0
            }
        ];
        FillInventory(save.Inventory, DataCatalog.StarbudPreserveId);
        session.Restore(save);
        var before = JsonSerializer.Serialize(session.Capture());
        Assert.Equal(
            "notice.inventory_full",
            session.CollectProcessedItem(
                ProcessorCatalog.PrismPreserveVatId
            ).MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
        Assert.False(session.Collection.IsDiscovered(
            DataCatalog.StarbudPreserveId
        ));
    }

    [Fact]
    public void PantryOnlyCookingMatchesPreviewAndDiscoversDish()
    {
        var session = CookingSessionWithMoonmistIngredients();
        var target = new GridPosition(29, 17);
        var discoveries = new List<string>();
        session.CollectionEntryDiscovered += discoveries.Add;

        Assert.True(session.CheckCookRecipe(
            target,
            DataCatalog.MoonmistStewRecipeId
        ).Succeeded);
        Assert.True(session.CookRecipe(
            target,
            DataCatalog.MoonmistStewRecipeId
        ).Succeeded);
        Assert.Equal(0, session.Kitchen.Count(DataCatalog.RipplecapId));
        Assert.Equal(0, session.Kitchen.Count(DataCatalog.MistsongMintId));
        Assert.Equal(0, session.Kitchen.Count(DataCatalog.DewhornMilkId));
        Assert.Equal(1, session.Inventory.Count(DataCatalog.MoonmistStewId));
        Assert.Equal([DataCatalog.MoonmistStewId], discoveries);
        Assert.True(session.Collection.IsDiscovered(DataCatalog.MoonmistStewId));
    }

    [Fact]
    public void FullBackpackPantryCookingChangesNoOwnerOrCollection()
    {
        var session = CookingSessionWithMoonmistIngredients();
        foreach (var itemId in DataCatalog.StorableItemIds
                     .Where(itemId => itemId != DataCatalog.MoonmistStewId)
                     .Where(itemId => DataCatalog.Item(itemId).Kind != ItemKind.Tool)
                     .Distinct(StringComparer.Ordinal)
                     .Take(Inventory.SlotCount - Inventory.StartingToolCount))
        {
            Assert.True(session.Inventory.Add(itemId, 1));
        }
        Assert.Equal(
            Inventory.SlotCount,
            session.Inventory.Slots.Count(slot => !slot.IsEmpty)
        );
        var before = JsonSerializer.Serialize(session.Capture());
        var target = new GridPosition(29, 17);

        Assert.Equal(
            "cooking.backpack_full",
            session.CheckCookRecipe(
                target,
                DataCatalog.MoonmistStewRecipeId
            ).MessageKey
        );
        Assert.Equal(
            "cooking.backpack_full",
            session.CookRecipe(
                target,
                DataCatalog.MoonmistStewRecipeId
            ).MessageKey
        );
        Assert.Equal(before, JsonSerializer.Serialize(session.Capture()));
        Assert.False(session.Collection.IsDiscovered(
            DataCatalog.MoonmistStewId
        ));
    }

    [Fact]
    public void MoonhearthJournalClaimsOnceAndAddsFiveEnergy()
    {
        var session = new GameSession();
        session.NewGame();
        Assert.Equal(60, session.EffectiveDishEnergyRestore(
            DataCatalog.MoonmistStewId
        ));
        foreach (var entry in CompendiumCatalog.CookingEntries.Take(3))
        {
            Assert.True(session.Collection.RecordObtainedItem(entry.ItemId));
        }
        session.SetPlayerLocation(
            20 * 16 + 8,
            12 * 16 + 8,
            PlayerLocationIds.MoonlitArchive
        );
        var desk = VillageCatalog.MoonlitArchiveDeskCell;
        Assert.False(session.ClaimCollectionReward(
            desk,
            CollectionRewardIds.MoonhearthRecipeJournal
        ).Succeeded);
        Assert.True(session.Collection.RecordObtainedItem(
            CompendiumCatalog.CookingEntries[3].ItemId
        ));
        Assert.True(session.ClaimCollectionReward(
            desk,
            CollectionRewardIds.MoonhearthRecipeJournal
        ).Succeeded);
        Assert.False(session.ClaimCollectionReward(
            desk,
            CollectionRewardIds.MoonhearthRecipeJournal
        ).Succeeded);
        Assert.Equal(65, session.EffectiveDishEnergyRestore(
            DataCatalog.MoonmistStewId
        ));

        Assert.True(session.Inventory.Add(DataCatalog.MoonmistStewId, 1));
        var save = session.Capture();
        save.Player.Energy = 30;
        session.Restore(save);
        Assert.True(session.EatCookedDish(
            DataCatalog.MoonmistStewId
        ).Succeeded);
        Assert.Equal(95, session.Energy);
        Assert.True(session.Collection.IsDiscovered(
            DataCatalog.MoonmistStewId
        ));
    }

    [Fact]
    public void StarlitLedgerClaimsOnceAndUsesOneSalePriceFunction()
    {
        var session = ArtisanRewardSession();
        Assert.Equal(61, session.SalePrice(DataCatalog.StarbudPreserveId));
        Assert.Equal(99, session.SalePrice(DataCatalog.MoonrootTonicId));
        Assert.Equal(69, session.SalePrice(DataCatalog.CloudleafTeaId));
        Assert.Equal(130, session.SalePrice(DataCatalog.StarhoneyId));
        Assert.Equal(
            DataCatalog.Item(DataCatalog.StarfeatherEggId).SellPrice,
            session.SalePrice(DataCatalog.StarfeatherEggId)
        );

        var beforeCoins = session.Coins;
        Assert.True(session.Inventory.Add(DataCatalog.StarbudPreserveId, 1));
        Assert.True(session.SellItem(DataCatalog.StarbudPreserveId).Succeeded);
        Assert.Equal(beforeCoins + 61, session.Coins);
        Assert.False(session.Collection.ClaimReward(
            CollectionRewardIds.StarlitAppraisalLedger
        ).Succeeded);
        Assert.Equal(55, DataCatalog.Item(
            DataCatalog.StarbudPreserveId
        ).SellPrice);
    }

    [Fact]
    public void ShippingUsesCurrentLedgerPriceAndFreezesHistoricalUnitPrice()
    {
        var session = ArtisanRewardSession();
        foreach (var entry in CompendiumCatalog.ArtisanEntries)
        {
            Assert.True(session.Inventory.Add(entry.ItemId, 1));
            Assert.True(session.QueueForShipping(entry.ItemId).Succeeded);
        }
        Assert.Equal(359, session.PendingShippingValue);

        var settlement = session.EndDay();
        Assert.Equal(359, settlement.TotalCoins);
        Assert.Equal(
            [61, 99, 69, 130],
            CompendiumCatalog.ArtisanEntries.Select(entry =>
                settlement.Lines.Single(line =>
                    line.ItemId == entry.ItemId
                ).UnitPrice
            )
        );
        var save = session.Capture();
        save.Collection.ClaimedRewardIds.Clear();
        var restored = new GameSession();
        restored.Restore(save);
        Assert.Equal(359, restored.Shipping.LastSettlement.TotalCoins);
        Assert.Equal(55, restored.SalePrice(DataCatalog.StarbudPreserveId));

        save.Shipping.LastSettlement = new ShippingSettlementSave
        {
            Day = save.Day - 1,
            Entries =
            [
                new ShippingEntrySave
                {
                    ItemId = DataCatalog.StarbudPreserveId,
                    Count = 1,
                    UnitPrice = 0
                }
            ]
        };
        restored.Restore(save);
        Assert.Equal(55, restored.Shipping.LastSettlement.TotalCoins);
    }

    [Fact]
    public void LegacyEvidenceCoversPersistentStrongEvidence()
    {
        var save = new GameSaveV1
        {
            Inventory = [Slot(DataCatalog.StarbudId)],
            Storage = new StorageSave
            {
                Chests = [new PlacedChestSave { Items = [Slot(DataCatalog.MoonrootId)] }]
            },
            Kitchen = new KitchenSave
            {
                PantryItems = [Slot(DataCatalog.CloudleafId)]
            },
            Shipping = new ShippingSave
            {
                Pending = [Shipping(DataCatalog.GlowpeaId)],
                LastSettlement = new ShippingSettlementSave
                {
                    Entries = [Shipping(DataCatalog.EmberbellId)]
                }
            },
            Festival = new FestivalSave
            {
                Results =
                [
                    new FestivalYearResultSave
                    {
                        ItemIds = [DataCatalog.PrismcornId],
                        GiftItemId = DataCatalog.DewmelonId,
                        GiftRewardItemId = DataCatalog.DuskbellId
                    }
                ]
            },
            Starlight = new StarlightSave
            {
                Nodes =
                [
                    new StarlightNodeSave
                    {
                        Contributions =
                        [
                            new StarlightContributionSave
                            {
                                ItemId = DataCatalog.DawnlaceId,
                                Count = 1
                            }
                        ]
                    }
                ]
            },
            Processor = new ProcessorSave
            {
                RecipeId = DataCatalog.CloudleafTeaRecipeId
            },
            Quest = new QuestSave { Harvested = 1 }
        };

        var collection = CollectionSystem.NormalizeSave(
            save.Collection,
            CollectionSystem.LegacyEvidenceItemIds(save)
        );
        foreach (var expected in new[]
                 {
                     DataCatalog.StarbudId,
                     DataCatalog.MoonrootId,
                     DataCatalog.CloudleafId,
                     DataCatalog.GlowpeaId,
                     DataCatalog.EmberbellId,
                     DataCatalog.PrismcornId,
                     DataCatalog.DewmelonId,
                     DataCatalog.DuskbellId,
                     DataCatalog.DawnlaceId
                 })
        {
            Assert.Contains(expected, collection.DiscoveredEntryIds);
        }
    }

    [Fact]
    public void RewardClaimIsAtomicAndPersistsWithoutInventoryItem()
    {
        var collection = new CollectionSystem();
        collection.Reset();
        foreach (var entry in CompendiumCatalog.CropEntries.Take(19))
        {
            Assert.True(collection.RecordObtainedItem(entry.HarvestItemId));
        }

        var before = collection.Capture();
        Assert.False(collection.ClaimReward(
            CollectionRewardIds.MoonlitAlmanac
        ).Succeeded);
        Assert.Equal(before.DiscoveredEntryIds, collection.Capture().DiscoveredEntryIds);
        Assert.Empty(collection.ClaimedRewardIds);

        Assert.True(collection.RecordObtainedItem(
            CompendiumCatalog.CropEntries[19].HarvestItemId
        ));
        Assert.True(collection.ClaimReward(
            CollectionRewardIds.MoonlitAlmanac
        ).Succeeded);
        Assert.False(collection.ClaimReward(
            CollectionRewardIds.MoonlitAlmanac
        ).Succeeded);
        Assert.False(collection.ClaimReward("unknown_reward").Succeeded);
        Assert.Contains(
            CollectionRewardIds.MoonlitAlmanac,
            collection.Capture().ClaimedRewardIds
        );
    }

    [Fact]
    public void MoonlitAlmanacDiscountUsesOneRuntimePriceFunction()
    {
        var session = CompletedCollectionSession();
        var seedIds = CompendiumCatalog.CropEntries
            .Select(entry => entry.SeedItemId)
            .ToArray();
        foreach (var seedId in seedIds)
        {
            var basePrice = DataCatalog.Item(seedId).BuyPrice;
            Assert.Equal(
                Math.Max(1, (basePrice * 9 + 9) / 10),
                session.PurchasePrice(seedId)
            );
        }

        Assert.Equal(14, session.PurchasePrice(DataCatalog.StarbudSeedId));
        Assert.Equal(42, session.PurchasePrice(DataCatalog.SunvaultGourdSeedId));
        Assert.Equal(71, session.PurchasePrice(DataCatalog.CrownstarSaffronSeedId));
        foreach (var itemId in new[]
                 {
                     DataCatalog.MoonplumSaplingId,
                     DataCatalog.MeadowFodderId,
                     DataCatalog.StarsoilFertilizerId
                 })
        {
            Assert.Equal(
                DataCatalog.Item(itemId).BuyPrice,
                session.PurchasePrice(itemId)
            );
        }
    }

    [Fact]
    public void PurchaseDeductsDiscountedPriceAndFailureDeductsNothing()
    {
        var session = CompletedCollectionSession();
        var seedId = DataCatalog.StarbudSeedId;
        var price = session.PurchasePrice(seedId);
        var save = session.Capture();
        save.Coins = price;
        session.Restore(save);
        var observed = new List<(int Coins, int Count)>();
        session.Changed += () => observed.Add((
            session.Coins,
            session.Inventory.Count(seedId)
        ));

        Assert.True(session.BuyItem(seedId).Succeeded);
        Assert.Equal(0, session.Coins);
        Assert.Equal(1, session.Inventory.Count(seedId));
        Assert.Equal([(0, 1)], observed);
        observed.Clear();
        Assert.False(session.BuyItem(seedId).Succeeded);
        Assert.Equal(0, session.Coins);
        Assert.Equal(1, session.Inventory.Count(seedId));
        Assert.Empty(observed);
    }

    [Fact]
    public void ArchiveDeskPreviewAndActionShareRealSpatialContract()
    {
        var session = new GameSession();
        session.NewGame();
        session.SetPlayerLocation(
            20 * 16 + 8,
            12 * 16 + 8,
            PlayerLocationIds.MoonlitArchive
        );
        var desk = VillageCatalog.MoonlitArchiveDeskCell;

        var check = session.CheckMoonlitArchiveCompendium(desk);
        var preview = session.PreviewSelectedTarget(desk);
        Assert.True(check.Succeeded);
        Assert.Equal(TargetPreviewState.Available, preview.State);
        Assert.Equal(TargetPreviewKind.ArchiveResearchDesk, preview.Kind);
        Assert.Equal(desk, preview.Target);
        Assert.True(session.OpenMoonlitArchiveCompendium(desk).Succeeded);

        session.Inventory.Select(1);
        Assert.Equal(
            "notice.needs_hand",
            session.CheckMoonlitArchiveCompendium(desk).MessageKey
        );
        Assert.Equal(
            TargetPreviewState.NeedsTool,
            session.PreviewSelectedTarget(desk).State
        );

        session.Inventory.Select(0);
        session.SetPlayerLocation(
            20 * 16 + 8,
            17 * 16 + 8,
            PlayerLocationIds.MoonlitArchive
        );
        Assert.False(session.CheckMoonlitArchiveCompendium(desk).Succeeded);
        Assert.Equal(
            TargetPreviewState.Blocked,
            session.PreviewSelectedTarget(desk).State
        );
    }

    private static GameSession CompletedCollectionSession()
    {
        var session = new GameSession();
        session.NewGame();
        foreach (var entry in CompendiumCatalog.CropEntries)
        {
            session.Collection.RecordObtainedItem(entry.HarvestItemId);
        }
        Assert.True(session.Collection.ClaimReward(
            CollectionRewardIds.MoonlitAlmanac
        ).Succeeded);
        return session;
    }

    private static GameSession CookingSessionWithMoonmistIngredients()
    {
        var session = new GameSession();
        session.NewGame();
        var save = session.Capture();
        save.Construction.Projects =
        [
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.CottageFirstUpgradeId,
                Completed = true
            },
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.HomesteadWorkshopProjectId,
                Completed = true
            },
            new ConstructionProjectSave
            {
                ProjectId = ConstructionCatalog.CottageSecondUpgradeId,
                Completed = true
            }
        ];
        save.Kitchen.PantryItems =
        [
            Slot(DataCatalog.RipplecapId),
            Slot(DataCatalog.MistsongMintId),
            Slot(DataCatalog.DewhornMilkId)
        ];
        save.Collection = new CollectionSave
        {
            Initialized = true,
            InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList()
        };
        save.Player.LocationId = PlayerLocationIds.Cottage;
        save.Player.X = 29 * 16 + 8;
        save.Player.Y = 18 * 16 + 8;
        save.Player.SelectedSlot = 0;
        session.Restore(save);
        return session;
    }

    private static GameSession ArtisanRewardSession()
    {
        var session = new GameSession();
        session.NewGame();
        foreach (var entry in CompendiumCatalog.ArtisanEntries)
        {
            Assert.True(session.Collection.RecordObtainedItem(entry.ItemId));
        }
        session.SetPlayerLocation(
            20 * 16 + 8,
            12 * 16 + 8,
            PlayerLocationIds.MoonlitArchive
        );
        Assert.True(session.ClaimCollectionReward(
            VillageCatalog.MoonlitArchiveDeskCell,
            CollectionRewardIds.StarlitAppraisalLedger
        ).Succeeded);
        return session;
    }

    private static CollectionSave InitializedCollection() => new()
    {
        Initialized = true,
        InitializedCategoryIds = CompendiumCatalog.CategoryIds.ToList()
    };

    private static void FillInventory(
        List<InventorySlot> slots,
        string excludedItemId
    )
    {
        slots.RemoveAll(slot =>
            slot.IsEmpty || DataCatalog.Item(slot.ItemId).Kind != ItemKind.Tool
        );
        var additions = DataCatalog.StorableItemIds
            .Where(itemId => itemId != excludedItemId)
            .Where(itemId => DataCatalog.Item(itemId).Kind != ItemKind.Tool)
            .Distinct(StringComparer.Ordinal)
            .Take(Inventory.SlotCount - Inventory.StartingToolCount)
            .Select(itemId => new InventorySlot
            {
                ItemId = itemId,
                Count = DataCatalog.Item(itemId).MaxStack
            });
        slots.AddRange(additions);
    }

    private static InventorySlot Slot(string itemId) => new()
    {
        ItemId = itemId,
        Count = 1
    };

    private static ShippingEntrySave Shipping(string itemId) => new()
    {
        ItemId = itemId,
        Count = 1
    };
}
