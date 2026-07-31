namespace Luminfield.Core;

public sealed record MailDefinition(
    string Id,
    string SenderKey,
    string SubjectKey,
    string BodyKey,
    string? AttachmentItemId = null,
    int AttachmentCount = 0
)
{
    public bool HasAttachment =>
        !string.IsNullOrWhiteSpace(AttachmentItemId) &&
        AttachmentCount > 0;
}

public sealed record DeliveredMail(
    MailDefinition Definition,
    int DeliveredDay,
    bool IsRead,
    bool AttachmentClaimed
);

public static class MailCatalog
{
    public const string MailboxId = "starlight_mailbox";
    public const string NemiWelcomeId = "nemi_welcome";
    public const string LioraTrustedId = "liora_trusted";
    public const string TaviTrustedId = "tavi_trusted";
    public const string NemiTrustedId = "nemi_trusted";

    public static readonly IReadOnlyList<MailDefinition> Definitions =
    [
        new(
            NemiWelcomeId,
            "village.npc.nemi.name",
            "mail.nemi_welcome.subject",
            "mail.nemi_welcome.body"
        ),
        new(
            LioraTrustedId,
            "village.npc.liora.name",
            "mail.liora_trusted.subject",
            "mail.liora_trusted.body",
            DataCatalog.CrystalShardId,
            2
        ),
        new(
            TaviTrustedId,
            "village.npc.tavi.name",
            "mail.tavi_trusted.subject",
            "mail.tavi_trusted.body",
            DataCatalog.LumenwoodId,
            4
        ),
        new(
            NemiTrustedId,
            "village.npc.nemi.name",
            "mail.nemi_trusted.subject",
            "mail.nemi_trusted.body",
            DataCatalog.StarbudSeedId,
            3
        )
    ];

    public static readonly IReadOnlyDictionary<string, MailDefinition>
        ById = Definitions.ToDictionary(
            definition => definition.Id,
            StringComparer.Ordinal
        );
}

public sealed class MailSystem
{
    private readonly Dictionary<string, MailEntrySave> _entries =
        new(StringComparer.Ordinal);

    public event Action? Changed;

    public IReadOnlyList<DeliveredMail> Delivered =>
        _entries.Values
            .OrderByDescending(entry => entry.DeliveredDay)
            .ThenBy(
                entry => MailOrder(entry.MailId)
            )
            .Select(ToDelivered)
            .ToList();

    public int UnreadCount => _entries.Values.Count(entry => !entry.IsRead);
    public bool HasUnread => UnreadCount > 0;

    public void Reset()
    {
        _entries.Clear();
        Changed?.Invoke();
    }

    public void Restore(MailSave? save)
    {
        _entries.Clear();
        foreach (var entry in NormalizeSave(save).Entries)
        {
            _entries[entry.MailId] = Clone(entry);
        }
        Changed?.Invoke();
    }

    public int DeliverForDay(int day, VillageSystem village)
    {
        var delivered = 0;
        if (village.MetNpcIds.Contains(VillageCatalog.NemiId))
        {
            delivered += Deliver(MailCatalog.NemiWelcomeId, day);
        }

        delivered += DeliverTrustedReward(
            MailCatalog.LioraTrustedId,
            VillageCatalog.LioraId,
            day,
            village
        );
        delivered += DeliverTrustedReward(
            MailCatalog.TaviTrustedId,
            VillageCatalog.TaviId,
            day,
            village
        );
        delivered += DeliverTrustedReward(
            MailCatalog.NemiTrustedId,
            VillageCatalog.NemiId,
            day,
            village
        );

        if (delivered > 0)
        {
            Changed?.Invoke();
        }
        return delivered;
    }

    public ActionResult Read(string mailId)
    {
        if (!_entries.TryGetValue(mailId, out var entry))
        {
            return ActionResult.Fail("mail.notice.not_delivered");
        }

        if (!entry.IsRead)
        {
            entry.IsRead = true;
            Changed?.Invoke();
        }
        return ActionResult.Success(messageKey: "mail.notice.read");
    }

    public ActionResult ClaimAttachment(
        string mailId,
        Inventory inventory
    )
    {
        if (!_entries.TryGetValue(mailId, out var entry))
        {
            return ActionResult.Fail("mail.notice.not_delivered");
        }

        var definition = MailCatalog.ById[mailId];
        if (!definition.HasAttachment ||
            definition.AttachmentItemId is null)
        {
            return ActionResult.Fail("mail.notice.no_attachment");
        }

        if (entry.AttachmentClaimed)
        {
            return ActionResult.Fail("mail.notice.already_claimed");
        }

        if (!inventory.Add(
                definition.AttachmentItemId,
                definition.AttachmentCount
            ))
        {
            return ActionResult.Fail("mail.notice.backpack_full");
        }

        entry.IsRead = true;
        entry.AttachmentClaimed = true;
        Changed?.Invoke();
        return ActionResult.Grant(
            definition.AttachmentItemId,
            definition.AttachmentCount,
            0,
            "mail.notice.claimed"
        );
    }

    public MailSave Capture() => new()
    {
        Entries = _entries.Values
            .OrderBy(entry => entry.DeliveredDay)
            .ThenBy(entry => MailOrder(entry.MailId))
            .Select(Clone)
            .ToList()
    };

    public static MailSave NormalizeSave(MailSave? save)
    {
        var entries = (save?.Entries ?? [])
            .Where(entry => MailCatalog.ById.ContainsKey(entry.MailId))
            .GroupBy(entry => entry.MailId, StringComparer.Ordinal)
            .Select(group => new MailEntrySave
            {
                MailId = group.Key,
                DeliveredDay = Math.Max(
                    1,
                    group.Max(entry => entry.DeliveredDay)
                ),
                IsRead = group.Any(entry => entry.IsRead),
                AttachmentClaimed = group.Any(
                    entry => entry.AttachmentClaimed
                )
            })
            .OrderBy(entry => entry.DeliveredDay)
            .ThenBy(entry => MailOrder(entry.MailId))
            .ToList();
        return new MailSave { Entries = entries };
    }

    private int DeliverTrustedReward(
        string mailId,
        string npcId,
        int day,
        VillageSystem village
    )
    {
        var relationship = village.Relationship(npcId);
        var tier = VillageSystem.TierFor(relationship.Points);
        if (tier < RelationshipTier.TrustedFriend)
        {
            return 0;
        }

        return Deliver(mailId, day);
    }

    private int Deliver(string mailId, int day)
    {
        if (_entries.ContainsKey(mailId))
        {
            return 0;
        }

        _entries[mailId] = new MailEntrySave
        {
            MailId = mailId,
            DeliveredDay = Math.Max(1, day)
        };
        return 1;
    }

    private static DeliveredMail ToDelivered(MailEntrySave entry) => new(
        MailCatalog.ById[entry.MailId],
        entry.DeliveredDay,
        entry.IsRead,
        entry.AttachmentClaimed
    );

    private static MailEntrySave Clone(MailEntrySave entry) => new()
    {
        MailId = entry.MailId,
        DeliveredDay = entry.DeliveredDay,
        IsRead = entry.IsRead,
        AttachmentClaimed = entry.AttachmentClaimed
    };

    private static int MailOrder(string mailId)
    {
        for (var index = 0; index < MailCatalog.Definitions.Count; index++)
        {
            if (MailCatalog.Definitions[index].Id == mailId)
            {
                return index;
            }
        }
        return int.MaxValue;
    }
}
