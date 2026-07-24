namespace Luminfield.Core;

public sealed class QuestSystem
{
    public QuestStage Stage { get; private set; } = QuestStage.TalkToMira;
    public int Tilled { get; private set; }
    public int Planted { get; private set; }
    public int Watered { get; private set; }
    public int GrownNights { get; private set; }
    public int Harvested { get; private set; }

    public event Action? Changed;

    public void Reset() => Restore(new QuestSave());

    public void Restore(QuestSave? save)
    {
        save ??= new QuestSave();
        Stage = save.Stage;
        Tilled = Math.Clamp(save.Tilled, 0, 3);
        Planted = Math.Clamp(save.Planted, 0, 3);
        Watered = Math.Clamp(save.Watered, 0, 3);
        GrownNights = Math.Clamp(save.GrownNights, 0, 2);
        Harvested = Math.Max(0, save.Harvested);
        Changed?.Invoke();
    }

    public bool InteractWithMira()
    {
        if (Stage == QuestStage.TalkToMira)
        {
            Stage = QuestStage.Till;
            Changed?.Invoke();
            return true;
        }

        if (Stage == QuestStage.ReturnToMira)
        {
            Stage = QuestStage.Complete;
            Changed?.Invoke();
        }

        return false;
    }

    public void OnTilled()
    {
        if (Stage != QuestStage.Till)
        {
            return;
        }

        Tilled = Math.Min(3, Tilled + 1);
        if (Tilled >= 3)
        {
            Stage = QuestStage.Plant;
        }

        Changed?.Invoke();
    }

    public void OnPlanted(string cropId)
    {
        if (Stage != QuestStage.Plant || cropId != DataCatalog.StarbudId)
        {
            return;
        }

        Planted = Math.Min(3, Planted + 1);
        if (Planted >= 3)
        {
            Stage = QuestStage.Water;
        }

        Changed?.Invoke();
    }

    public void OnWatered(string? cropId)
    {
        if (Stage != QuestStage.Water || cropId != DataCatalog.StarbudId)
        {
            return;
        }

        Watered = Math.Min(3, Watered + 1);
        if (Watered >= 3)
        {
            Stage = QuestStage.Grow;
        }

        Changed?.Invoke();
    }

    public void OnNightResolved(int matureStarbuds)
    {
        if (Stage != QuestStage.Grow)
        {
            return;
        }

        if (matureStarbuds >= 3)
        {
            GrownNights = 2;
            Stage = QuestStage.Harvest;
        }
        else
        {
            GrownNights = Math.Min(2, GrownNights + 1);
        }

        Changed?.Invoke();
    }

    public void OnHarvested(string itemId)
    {
        if (Stage != QuestStage.Harvest || itemId != DataCatalog.StarbudId)
        {
            return;
        }

        Harvested++;
        if (Harvested >= 3)
        {
            Stage = QuestStage.ReturnToMira;
        }

        Changed?.Invoke();
    }

    public string ObjectiveKey => Stage switch
    {
        QuestStage.TalkToMira => "objective.talk",
        QuestStage.Till => "objective.till",
        QuestStage.Plant => "objective.plant",
        QuestStage.Water => "objective.water",
        QuestStage.Grow => "objective.grow",
        QuestStage.Harvest => "objective.harvest",
        QuestStage.ReturnToMira => "objective.return",
        QuestStage.Complete => "objective.complete",
        _ => "objective.talk"
    };

    public int ObjectiveCount => Stage switch
    {
        QuestStage.Till => Tilled,
        QuestStage.Plant => Planted,
        QuestStage.Water => Watered,
        QuestStage.Grow => GrownNights,
        QuestStage.Harvest => Harvested,
        _ => 0
    };

    public QuestSave Capture() => new()
    {
        Stage = Stage,
        Tilled = Tilled,
        Planted = Planted,
        Watered = Watered,
        GrownNights = GrownNights,
        Harvested = Harvested
    };
}
