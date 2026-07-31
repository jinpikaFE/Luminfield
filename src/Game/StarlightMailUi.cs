using Godot;
using Luminfield.Core;

namespace Luminfield.Game;

public sealed partial class StarlightMailOverlay : FullScreenUi
{
    private readonly GameSession _session;
    private readonly LocaleService _locale;
    private readonly Label _title;
    private readonly Label _unread;
    private readonly VBoxContainer _mailList;
    private readonly Label _empty;
    private readonly Label _sender;
    private readonly Label _subject;
    private readonly Label _day;
    private readonly Label _body;
    private readonly Label _attachment;
    private readonly Label _notice;
    private readonly Button _claim;
    private readonly Button _close;
    private string? _selectedMailId;

    public StarlightMailOverlay(
        Theme theme,
        GameSession session,
        LocaleService locale
    ) : base(theme)
    {
        _session = session;
        _locale = locale;
        AddChild(Dim(new Color(0.008f, 0.015f, 0.06f, 0.88f)));

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(560, 332)
        };
        panel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.Box(
                new Color("#0b1734fc"),
                ThemeFactory.Mint,
                2,
                10
            )
        );
        center.AddChild(panel);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 7);
        panel.AddChild(column);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 9);
        header.AddChild(Icon(
            GeneratedArt.CreateStarlightEnvelopeIcon(),
            new Vector2(54, 48)
        ));
        _title = ThemeFactory.Label(size: 21, color: ThemeFactory.Mint);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _title.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_title);
        _unread = ThemeFactory.Label(size: 10, color: ThemeFactory.Gold);
        _unread.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_unread);
        column.AddChild(header);

        var content = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(520, 218),
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        content.AddThemeConstantOverride("separation", 8);
        column.AddChild(content);

        var listPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(190, 214)
        };
        listPanel.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#101e3aee"),
                new Color("#41577c"),
                1,
                6,
                6
            )
        );
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        _mailList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _mailList.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_mailList);
        listPanel.AddChild(scroll);
        content.AddChild(listPanel);

        var paper = new PanelContainer
        {
            CustomMinimumSize = new Vector2(322, 214),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        paper.AddThemeStyleboxOverride(
            "panel",
            ThemeFactory.CompactBox(
                new Color("#172746f5"),
                ThemeFactory.Gold,
                1,
                7,
                9
            )
        );
        var bodyColumn = new VBoxContainer();
        bodyColumn.AddThemeConstantOverride("separation", 4);
        paper.AddChild(bodyColumn);
        content.AddChild(paper);

        var senderRow = new HBoxContainer();
        senderRow.AddThemeConstantOverride("separation", 6);
        senderRow.AddChild(Icon(
            GeneratedArt.CreateRelationshipReplyIcon(),
            new Vector2(32, 32)
        ));
        _sender = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        _sender.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _sender.VerticalAlignment = VerticalAlignment.Center;
        senderRow.AddChild(_sender);
        _day = ThemeFactory.Label(size: 9, color: ThemeFactory.MutedInk);
        _day.VerticalAlignment = VerticalAlignment.Center;
        senderRow.AddChild(_day);
        bodyColumn.AddChild(senderRow);

        _subject = ThemeFactory.Label(size: 16, color: ThemeFactory.Gold);
        bodyColumn.AddChild(_subject);
        _body = ThemeFactory.Label(size: 10);
        _body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _body.CustomMinimumSize = new Vector2(295, 74);
        _body.SizeFlagsVertical = SizeFlags.ExpandFill;
        bodyColumn.AddChild(_body);

        _attachment = ThemeFactory.Label(size: 10, color: ThemeFactory.Mint);
        bodyColumn.AddChild(_attachment);
        _claim = ThemeFactory.Button("");
        _claim.CustomMinimumSize = new Vector2(210, 28);
        _claim.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _claim.Pressed += ClaimSelected;
        bodyColumn.AddChild(_claim);

        _empty = ThemeFactory.Label(size: 11, color: ThemeFactory.MutedInk);
        _empty.HorizontalAlignment = HorizontalAlignment.Center;
        _empty.Visible = false;
        bodyColumn.AddChild(_empty);

        _notice = ThemeFactory.Label(size: 9, color: ThemeFactory.Mint);
        _notice.HorizontalAlignment = HorizontalAlignment.Center;
        _notice.CustomMinimumSize = new Vector2(500, 15);
        column.AddChild(_notice);

        _close = ThemeFactory.Button("");
        _close.CustomMinimumSize = new Vector2(170, 27);
        _close.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _close.Pressed += () => CloseRequested?.Invoke();
        column.AddChild(_close);

        session.Changed += RefreshText;
        locale.LocaleChanged += RefreshText;
        RefreshText();
        Callable.From(ReadInitialSelection).CallDeferred();
        _close.CallDeferred(Control.MethodName.GrabFocus);
    }

    public event Action? CloseRequested;
    public event Action? MailChanged;

    public void PressClaimForPlaytest()
    {
        _claim.EmitSignal(Button.SignalName.Pressed);
    }

    public void RefreshText()
    {
        var delivered = _session.Mail.Delivered;
        if (_selectedMailId is null ||
            delivered.All(mail => mail.Definition.Id != _selectedMailId))
        {
            _selectedMailId = delivered.FirstOrDefault()?.Definition.Id;
        }

        _title.Text = _locale.Tr("mail.ui.title");
        _unread.Text = _locale.Tr(
            "mail.ui.unread_count",
            _session.Mail.UnreadCount
        );
        _close.Text = _locale.Tr("menu.back");
        RebuildMailList(delivered);
        RefreshSelected(delivered);
    }

    public override void _ExitTree()
    {
        _session.Changed -= RefreshText;
        _locale.LocaleChanged -= RefreshText;
    }

    private void RebuildMailList(IReadOnlyList<DeliveredMail> delivered)
    {
        foreach (var child in _mailList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var mail in delivered)
        {
            var mailId = mail.Definition.Id;
            var stateKey = mail.IsRead
                ? "mail.ui.read"
                : "mail.ui.unread";
            var button = ThemeFactory.Button(
                _locale.Tr(
                    "mail.ui.list_entry",
                    _locale.Tr(stateKey),
                    _locale.Tr(mail.Definition.SubjectKey)
                )
            );
            button.CustomMinimumSize = new Vector2(172, 34);
            button.TooltipText = _locale.Tr(mail.Definition.SenderKey);
            button.Disabled = mailId == _selectedMailId;
            button.Pressed += () => SelectMail(mailId);
            _mailList.AddChild(button);
        }
    }

    private void RefreshSelected(IReadOnlyList<DeliveredMail> delivered)
    {
        var selected = delivered.FirstOrDefault(
            mail => mail.Definition.Id == _selectedMailId
        );
        var hasSelection = selected is not null;
        _sender.Visible = hasSelection;
        _subject.Visible = hasSelection;
        _day.Visible = hasSelection;
        _body.Visible = hasSelection;
        _attachment.Visible = hasSelection;
        _claim.Visible = hasSelection;
        _empty.Visible = !hasSelection;
        if (selected is null)
        {
            _empty.Text = _locale.Tr("mail.ui.empty");
            return;
        }

        var definition = selected.Definition;
        _sender.Text = _locale.Tr(
            "mail.ui.from",
            _locale.Tr(definition.SenderKey)
        );
        _subject.Text = _locale.Tr(definition.SubjectKey);
        _day.Text = _locale.Tr(
            "mail.ui.delivered_day",
            selected.DeliveredDay
        );
        _body.Text = _locale.Tr(definition.BodyKey);

        if (!definition.HasAttachment ||
            definition.AttachmentItemId is null)
        {
            _attachment.Text = _locale.Tr("mail.ui.no_attachment");
            _claim.Text = _locale.Tr("mail.ui.no_attachment");
            _claim.Disabled = true;
            return;
        }

        var itemName = _locale.Tr(
            DataCatalog.Item(definition.AttachmentItemId).NameKey
        );
        _attachment.Text = _locale.Tr(
            "mail.ui.attachment",
            itemName,
            definition.AttachmentCount
        );
        if (selected.AttachmentClaimed)
        {
            _claim.Text = _locale.Tr("mail.ui.claimed");
            _claim.Disabled = true;
            return;
        }

        _claim.Text = _locale.Tr("mail.ui.claim");
        _claim.Disabled = false;
    }

    private void SelectMail(string mailId)
    {
        _selectedMailId = mailId;
        var selected = _session.Mail.Delivered.FirstOrDefault(
            mail => mail.Definition.Id == mailId
        );
        if (selected is not null && !selected.IsRead)
        {
            var result = _session.ReadMail(mailId);
            if (result.Succeeded)
            {
                MailChanged?.Invoke();
            }
        }
        RefreshText();
    }

    private void ReadInitialSelection()
    {
        if (_selectedMailId is not null)
        {
            SelectMail(_selectedMailId);
        }
    }

    private void ClaimSelected()
    {
        if (_selectedMailId is null)
        {
            return;
        }

        var result = _session.ClaimMailAttachment(_selectedMailId);
        var selected = _session.Mail.Delivered.FirstOrDefault(
            mail => mail.Definition.Id == _selectedMailId
        );
        if (result.Succeeded && selected is not null &&
            selected.Definition.AttachmentItemId is not null)
        {
            var itemName = _locale.Tr(
                DataCatalog.Item(
                    selected.Definition.AttachmentItemId
                ).NameKey
            );
            _notice.Text = _locale.Tr(
                result.MessageKey,
                itemName,
                selected.Definition.AttachmentCount
            );
            MailChanged?.Invoke();
        }
        else
        {
            _notice.Text = _locale.Tr(result.MessageKey);
        }
        RefreshText();
    }

    private static TextureRect Icon(Texture2D texture, Vector2 size) => new()
    {
        Texture = texture,
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore
    };
}
