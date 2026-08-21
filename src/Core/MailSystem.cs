namespace Luminfield.Core;

public enum MailDeliveryTriggerKind
{
    MetNpc,
    RelationshipTier,
    CharacterEventCompleted
}

public sealed record MailDeliveryRule(
    MailDeliveryTriggerKind Kind,
    string ReferenceId,
    RelationshipTier MinimumTier = RelationshipTier.NewAcquaintance
);

public sealed record MailDefinition(
    string Id,
    string SenderKey,
    string SubjectKey,
    string BodyKey,
    string? AttachmentItemId = null,
    int AttachmentCount = 0,
    MailDeliveryRule? DeliveryRule = null
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
    public const string HaldenKindredId = "halden_kindred";
    public const string MaveaKindredId = "mavea_kindred";
    public const string SivrenKindredId = "sivren_kindred";
    public const string DorrikKindredId = "dorrik_kindred";
    public const string KaelKindredId = "kael_kindred";
    public const string SelaKindredId = "sela_kindred";
    public const string ElowenKindredId = "elowen_kindred";
    public const string VessaKindredId = "vessa_kindred";
    public const string OrinKindredId = "orin_kindred";
    public const string YvaraKindredId = "yvara_kindred";
    public const string BrialKindredId = "brial_kindred";
    public const string PavriKindredId = "pavri_kindred";
    public const string RovenKindredId = "roven_kindred";

    public static readonly IReadOnlyList<MailDefinition> Definitions =
    [
        new(
            NemiWelcomeId,
            "village.npc.nemi.name",
            "mail.nemi_welcome.subject",
            "mail.nemi_welcome.body",
            DeliveryRule: new(
                MailDeliveryTriggerKind.MetNpc,
                VillageCatalog.NemiId
            )
        ),
        new(
            LioraTrustedId,
            "village.npc.liora.name",
            "mail.liora_trusted.subject",
            "mail.liora_trusted.body",
            DataCatalog.CrystalShardId,
            2,
            new(
                MailDeliveryTriggerKind.RelationshipTier,
                VillageCatalog.LioraId,
                RelationshipTier.TrustedFriend
            )
        ),
        new(
            TaviTrustedId,
            "village.npc.tavi.name",
            "mail.tavi_trusted.subject",
            "mail.tavi_trusted.body",
            DataCatalog.LumenwoodId,
            4,
            new(
                MailDeliveryTriggerKind.RelationshipTier,
                VillageCatalog.TaviId,
                RelationshipTier.TrustedFriend
            )
        ),
        new(
            NemiTrustedId,
            "village.npc.nemi.name",
            "mail.nemi_trusted.subject",
            "mail.nemi_trusted.body",
            DataCatalog.StarbudSeedId,
            3,
            new(
                MailDeliveryTriggerKind.RelationshipTier,
                VillageCatalog.NemiId,
                RelationshipTier.TrustedFriend
            )
        ),
        new(
            HaldenKindredId,
            "village.npc.halden.name",
            "mail.halden_kindred.subject",
            "mail.halden_kindred.body",
            DataCatalog.MeadowFodderId,
            14,
            new(
                MailDeliveryTriggerKind.CharacterEventCompleted,
                CharacterEventCatalog.HaldenThreeBreathsOneRhythmId
            )
        ),
        new(
            MaveaKindredId,
            "village.npc.mavea.name",
            "mail.mavea_kindred.subject",
            "mail.mavea_kindred.body",
            DataCatalog.StarhoneyCustardId,
            1,
            new(
                MailDeliveryTriggerKind.CharacterEventCompleted,
                CharacterEventCatalog.MaveaWarmthThatKeepsId
            )
        ),
        new(
            SivrenKindredId,
            "village.npc.sivren.name",
            "mail.sivren_kindred.subject",
            "mail.sivren_kindred.body",
            DataCatalog.StarlightTorchId,
            2,
            new(
                MailDeliveryTriggerKind.CharacterEventCompleted,
                CharacterEventCatalog.SivrenYearInThreeLightsId
            )
        ),
        new(
            DorrikKindredId,
            "village.npc.dorrik.name",
            "mail.dorrik_kindred.subject",
            "mail.dorrik_kindred.body",
            DataCatalog.LumenwoodId,
            8,
            new(
                MailDeliveryTriggerKind.CharacterEventCompleted,
                CharacterEventCatalog.DorrikRoomsThatBreatheId
            )
        ),
        new(
            KaelKindredId,
            "village.npc.kael.name",
            "mail.kael_kindred.subject",
            "mail.kael_kindred.body",
            DataCatalog.MoonstonePathId,
            12,
            new(
                MailDeliveryTriggerKind.CharacterEventCompleted,
                CharacterEventCatalog.KaelSafeReturnRouteId
            )
        ),
        new(
            SelaKindredId,
            "village.npc.sela.name",
            "mail.sela_kindred.subject",
            "mail.sela_kindred.body",
            DataCatalog.CrystalShardId,
            4,
            new(
                MailDeliveryTriggerKind.CharacterEventCompleted,
                CharacterEventCatalog.SelaSharedForgeRhythmId
            )
        ),
        new(
            ElowenKindredId,
            "village.npc.elowen.name",
            "mail.elowen_kindred.subject",
            "mail.elowen_kindred.body",
            DataCatalog.DewfallSprinklerId,
            1,
            new(
                MailDeliveryTriggerKind.CharacterEventCompleted,
                CharacterEventCatalog.ElowenWaterlineReadTogetherId
            )
        ),
        new(
            VessaKindredId,
            "village.npc.vessa.name",
            "mail.vessa_kindred.subject",
            "mail.vessa_kindred.body",
            DataCatalog.CloudleafTeaId,
            2,
            new(
                MailDeliveryTriggerKind.CharacterEventCompleted,
                CharacterEventCatalog.VessaPathThatListensBackId
            )
        ),
        new(
            OrinKindredId,
            "village.npc.orin.name",
            "mail.orin_kindred.subject",
            "mail.orin_kindred.body",
            DataCatalog.StarbudPreserveId,
            2,
            new(
                MailDeliveryTriggerKind.CharacterEventCompleted,
                CharacterEventCatalog.OrinSharedLanternRouteId
            )
        ),
        new(
            YvaraKindredId,
            "village.npc.yvara.name",
            "mail.yvara_kindred.subject",
            "mail.yvara_kindred.body",
            DataCatalog.MoonplumSaplingId,
            1,
            new(
                MailDeliveryTriggerKind.CharacterEventCompleted,
                CharacterEventCatalog.YvaraASeasonCarriedGentlyId
            )
        ),
        new(
            BrialKindredId,
            "village.npc.brial.name",
            "mail.brial_kindred.subject",
            "mail.brial_kindred.body",
            DataCatalog.StarhoneyId,
            1,
            new(
                MailDeliveryTriggerKind.CharacterEventCompleted,
                CharacterEventCatalog.BrialAPathLeftForTheBeesId
            )
        ),
        new(
            PavriKindredId,
            "village.npc.pavri.name",
            "mail.pavri_kindred.subject",
            "mail.pavri_kindred.body",
            DataCatalog.MoonfleeceId,
            1,
            new(
                MailDeliveryTriggerKind.CharacterEventCompleted,
                CharacterEventCatalog.PavriClothThatKeepsWarmthId
            )
        ),
        new(
            RovenKindredId,
            "village.npc.roven.name",
            "mail.roven_kindred.subject",
            "mail.roven_kindred.body",
            DataCatalog.StarlightTorchId,
            4,
            new(
                MailDeliveryTriggerKind.CharacterEventCompleted,
                CharacterEventCatalog.RovenLightsThatWaitForReturnId
            )
        )
    ];

    public static readonly IReadOnlyDictionary<string, MailDefinition>
        ById = BuildById();

    private static IReadOnlyDictionary<string, MailDefinition> BuildById()
    {
        var byId = new Dictionary<string, MailDefinition>(
            StringComparer.Ordinal
        );
        foreach (var definition in Definitions)
        {
            var rule = definition.DeliveryRule;
            var validRule = rule is not null && rule.Kind switch
            {
                MailDeliveryTriggerKind.MetNpc or
                    MailDeliveryTriggerKind.RelationshipTier =>
                    VillageCatalog.Npcs.ContainsKey(rule.ReferenceId),
                MailDeliveryTriggerKind.CharacterEventCompleted =>
                    CharacterEventCatalog.ById.ContainsKey(rule.ReferenceId),
                _ => false
            };
            var validSender = validRule && rule is not null &&
                (rule.Kind ==
                    MailDeliveryTriggerKind.CharacterEventCompleted
                    ? CharacterEventCatalog.ById.TryGetValue(
                        rule.ReferenceId,
                        out var eventDefinition
                    ) && VillageCatalog.Npcs[eventDefinition.NpcId].NameKey ==
                        definition.SenderKey
                    : VillageCatalog.Npcs[rule.ReferenceId].NameKey ==
                        definition.SenderKey);
            var validAttachment = definition.AttachmentItemId is null
                ? definition.AttachmentCount == 0
                : definition.AttachmentCount > 0 &&
                    DataCatalog.Items.ContainsKey(
                        definition.AttachmentItemId
                    );
            if (string.IsNullOrWhiteSpace(definition.Id) ||
                string.IsNullOrWhiteSpace(definition.SenderKey) ||
                string.IsNullOrWhiteSpace(definition.SubjectKey) ||
                string.IsNullOrWhiteSpace(definition.BodyKey) ||
                !validRule ||
                !validSender ||
                !validAttachment ||
                !byId.TryAdd(definition.Id, definition))
            {
                throw new InvalidOperationException(
                    $"Invalid mail catalog entry: {definition.Id}."
                );
            }
        }

        foreach (var npc in VillageCatalog.Npcs.Values)
        {
            var hasRelationshipReward = Definitions.Any(definition =>
                definition.SenderKey == npc.NameKey &&
                definition.DeliveryRule?.Kind is
                    MailDeliveryTriggerKind.RelationshipTier or
                    MailDeliveryTriggerKind.CharacterEventCompleted
            );
            if (!hasRelationshipReward)
            {
                throw new InvalidOperationException(
                    $"Village NPC {npc.Id} requires a relationship reward mail."
                );
            }
        }

        return byId;
    }
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

    public int DeliverForDay(
        int day,
        VillageSystem village,
        CharacterEventSystem characterEvents
    )
    {
        var delivered = 0;
        foreach (var definition in MailCatalog.Definitions)
        {
            if (ShouldDeliver(
                    definition,
                    day,
                    village,
                    characterEvents
                ))
            {
                delivered += Deliver(definition.Id, day);
            }
        }

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

    private static bool ShouldDeliver(
        MailDefinition definition,
        int day,
        VillageSystem village,
        CharacterEventSystem characterEvents
    )
    {
        var rule = definition.DeliveryRule;
        if (rule is null)
        {
            return false;
        }

        return rule.Kind switch
        {
            MailDeliveryTriggerKind.MetNpc =>
                village.MetNpcIds.Contains(rule.ReferenceId),
            MailDeliveryTriggerKind.RelationshipTier =>
                VillageSystem.TierFor(
                    village.Relationship(rule.ReferenceId).Points
                ) >= rule.MinimumTier,
            MailDeliveryTriggerKind.CharacterEventCompleted =>
                characterEvents.CompletedDay(rule.ReferenceId) is int
                    completedDay && completedDay < day,
            _ => false
        };
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
