namespace Luminfield.Core;

public static class AnimalMoodIds
{
    public const string Content = "content";
    public const string Happy = "happy";
    public const string Hungry = "hungry";

    public static bool IsValid(string? moodId) =>
        moodId is Content or Happy or Hungry;
}

public sealed class StarfeatherChickenState
{
    public StarfeatherChickenState(string chickenId)
    {
        ChickenId = chickenId;
    }

    public string ChickenId { get; }
    public int Affection { get; internal set; }
    public int LastFedDay { get; internal set; }
    public int LastPettedDay { get; internal set; }
    public int PendingEggs { get; internal set; }
    public string MoodId { get; internal set; } = AnimalMoodIds.Content;

    public bool FedToday(int day) => LastFedDay == day;
    public bool PettedToday(int day) => LastPettedDay == day;
}

public sealed class AnimalSystem
{
    public const int MaxAffection = 100;
    public const int MaxPendingEggs = 3;
    public const int FeedAffectionGain = 3;
    public const int PetAffectionGain = 1;

    private readonly List<StarfeatherChickenState> _chickens = [];

    public bool CoopBuilt { get; private set; }
    public IReadOnlyList<StarfeatherChickenState> Chickens => _chickens;
    public StarfeatherChickenState? FirstChicken => _chickens.FirstOrDefault();

    public event Action? Changed;

    public void Reset()
    {
        CoopBuilt = false;
        _chickens.Clear();
        Changed?.Invoke();
    }

    public bool IsCoopCell(GridPosition position) =>
        AnimalCatalog.CoopCells.Contains(position);

    public bool IsInteractiveCell(GridPosition position) =>
        CoopBuilt && IsCoopCell(position);

    public bool BlocksMovement(GridPosition position) =>
        CoopBuilt && IsCoopCell(position);

    public bool CanBuildCoop(
        Inventory inventory,
        int coins,
        out string failureKey
    )
    {
        if (CoopBuilt)
        {
            failureKey = "animal.coop.already_built";
            return false;
        }

        if (coins < AnimalCatalog.CoopBuildCostCoins)
        {
            failureKey = "animal.coop.need_coins";
            return false;
        }

        if (AnimalCatalog.CoopBuildMaterials.Any(ingredient =>
                inventory.Count(ingredient.ItemId) < ingredient.Count))
        {
            failureKey = "animal.coop.need_materials";
            return false;
        }

        failureKey = string.Empty;
        return true;
    }

    public ActionResult BuildCoopAfterPayment()
    {
        if (CoopBuilt)
        {
            return ActionResult.Fail("animal.coop.already_built");
        }

        CoopBuilt = true;
        _chickens.Clear();
        _chickens.Add(new StarfeatherChickenState(AnimalCatalog.FirstChickenId)
        {
            MoodId = AnimalMoodIds.Content
        });
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "animal.coop.built");
    }

    public ActionResult FeedFirstChicken(
        Inventory inventory,
        int day
    )
    {
        var chicken = FirstChicken;
        if (!CoopBuilt || chicken is null)
        {
            return ActionResult.Fail("animal.coop.not_built");
        }

        if (chicken.FedToday(day))
        {
            return ActionResult.Fail("animal.chicken.already_fed");
        }

        if (!inventory.Remove(DataCatalog.StargrainFeedId, 1))
        {
            return ActionResult.Fail("animal.chicken.need_feed");
        }

        chicken.LastFedDay = day;
        chicken.Affection = Math.Min(
            MaxAffection,
            chicken.Affection + FeedAffectionGain
        );
        chicken.MoodId = AnimalMoodIds.Content;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "animal.chicken.fed");
    }

    public ActionResult PetFirstChicken(int day)
    {
        var chicken = FirstChicken;
        if (!CoopBuilt || chicken is null)
        {
            return ActionResult.Fail("animal.coop.not_built");
        }

        if (chicken.PettedToday(day))
        {
            return ActionResult.Fail("animal.chicken.already_cared");
        }

        chicken.LastPettedDay = day;
        chicken.Affection = Math.Min(
            MaxAffection,
            chicken.Affection + PetAffectionGain
        );
        chicken.MoodId = AnimalMoodIds.Happy;
        Changed?.Invoke();
        return ActionResult.Success(messageKey: "animal.chicken.petted");
    }

    public ActionResult CollectEggs(Inventory inventory)
    {
        var chicken = FirstChicken;
        if (!CoopBuilt || chicken is null)
        {
            return ActionResult.Fail("animal.coop.not_built");
        }

        if (chicken.PendingEggs <= 0)
        {
            return ActionResult.Fail("animal.chicken.no_eggs");
        }

        if (!inventory.CanAdd(DataCatalog.StarfeatherEggId, chicken.PendingEggs))
        {
            return ActionResult.Fail("notice.inventory_full");
        }

        var eggs = chicken.PendingEggs;
        inventory.Add(DataCatalog.StarfeatherEggId, eggs);
        chicken.PendingEggs = 0;
        Changed?.Invoke();
        return ActionResult.Grant(
            DataCatalog.StarfeatherEggId,
            eggs,
            0,
            "animal.chicken.eggs_collected"
        );
    }

    public void ResolveNight(int endingDay)
    {
        if (!CoopBuilt)
        {
            return;
        }

        var changed = false;
        foreach (var chicken in _chickens)
        {
            if (chicken.FedToday(endingDay))
            {
                if (chicken.PendingEggs < MaxPendingEggs)
                {
                    chicken.PendingEggs++;
                }
                chicken.MoodId = AnimalMoodIds.Happy;
                changed = true;
                continue;
            }

            chicken.MoodId = AnimalMoodIds.Hungry;
            changed = true;
        }

        if (changed)
        {
            Changed?.Invoke();
        }
    }

    public void Restore(AnimalSave? save)
    {
        var normalized = NormalizeSave(save);
        CoopBuilt = normalized.CoopBuilt;
        _chickens.Clear();
        foreach (var entry in normalized.Chickens)
        {
            _chickens.Add(new StarfeatherChickenState(entry.ChickenId)
            {
                Affection = entry.Affection,
                LastFedDay = entry.LastFedDay,
                LastPettedDay = entry.LastPettedDay,
                PendingEggs = entry.PendingEggs,
                MoodId = entry.MoodId
            });
        }
        Changed?.Invoke();
    }

    public AnimalSave Capture() => new()
    {
        CoopBuilt = CoopBuilt,
        Chickens = _chickens
            .OrderBy(chicken => chicken.ChickenId, StringComparer.Ordinal)
            .Select(chicken => new StarfeatherChickenSave
            {
                ChickenId = chicken.ChickenId,
                Affection = chicken.Affection,
                LastFedDay = chicken.LastFedDay,
                LastPettedDay = chicken.LastPettedDay,
                PendingEggs = chicken.PendingEggs,
                MoodId = chicken.MoodId
            })
            .ToList()
    };

    public static AnimalSave NormalizeSave(AnimalSave? save)
    {
        if (save?.CoopBuilt != true)
        {
            return new AnimalSave();
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var chickens = (save.Chickens ?? [])
            .Where(entry =>
                entry is not null &&
                AnimalCatalog.ChickenIds.Contains(
                    entry.ChickenId,
                    StringComparer.Ordinal
                ) &&
                seen.Add(entry.ChickenId))
            .Select(entry => new StarfeatherChickenSave
            {
                ChickenId = entry.ChickenId,
                Affection = Math.Clamp(entry.Affection, 0, MaxAffection),
                LastFedDay = Math.Max(0, entry.LastFedDay),
                LastPettedDay = Math.Max(0, entry.LastPettedDay),
                PendingEggs = Math.Clamp(
                    entry.PendingEggs,
                    0,
                    MaxPendingEggs
                ),
                MoodId = AnimalMoodIds.IsValid(entry.MoodId)
                    ? entry.MoodId
                    : AnimalMoodIds.Content
            })
            .ToList();

        if (chickens.Count == 0)
        {
            chickens.Add(new StarfeatherChickenSave
            {
                ChickenId = AnimalCatalog.FirstChickenId,
                MoodId = AnimalMoodIds.Content
            });
        }

        return new AnimalSave
        {
            CoopBuilt = true,
            Chickens = chickens
        };
    }
}
