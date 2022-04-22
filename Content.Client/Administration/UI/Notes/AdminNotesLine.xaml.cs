﻿using Content.Shared.Administration.Notes;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Input;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client.Administration.UI.Notes;

[GenerateTypedNameReferences]
public sealed partial class AdminNotesLine : BoxContainer
{
    private RichTextLabel? _label;
    private LineEdit? _edit;

    public AdminNotesLine(SharedAdminNote note)
    {
        RobustXamlLoader.Load(this);

        Note = note;
        MouseFilter = MouseFilterMode.Pass;

        AddLabel();
    }

    public SharedAdminNote Note { get; private set; }
    public int Id => Note.Id;
    public string OriginalMessage => Note.Message;
    public string EditText => _edit?.Text ?? OriginalMessage;

    public event Action<AdminNotesLine>? OnSubmitted;
    public event Func<AdminNotesLine, bool>? OnRightClicked;

    private void AddLabel()
    {
        if (_edit != null)
        {
            _edit.OnTextEntered -= Submitted;
            _edit.OnFocusExit -= Submitted;

            RemoveChild(_edit);
            _edit = null;
        }

        _label = new RichTextLabel();
        _label.SetMessage(Note.Message);

        AddChild(_label);
        _label.SetPositionFirst();

        Separator.Visible = true;
    }

    private void AddLineEdit()
    {
        if (_label != null)
        {
            RemoveChild(_label);
            _label = null;
        }

        _edit = new LineEdit {Text = Note.Message};
        _edit.OnTextEntered += Submitted;
        _edit.OnFocusExit += Submitted;

        AddChild(_edit);
        _edit.SetPositionFirst();
        _edit.GrabKeyboardFocus();
        _edit.CursorPosition = _edit.Text.Length;

        Separator.Visible = false;
    }

    private void Submitted(LineEditEventArgs args)
    {
        OnSubmitted?.Invoke(this);

        AddLabel();

        var note = Note with {Message = args.Text};
        UpdateNote(note);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIRightClick &&
            args.Function != EngineKeyFunctions.UIClick)
        {
            return;
        }

        if (OnRightClicked?.Invoke(this) == true)
        {
            args.Handle();
        }
    }

    public void UpdateNote(SharedAdminNote note)
    {
        Note = note;
        _label?.SetMessage(note.Message);

        if (_edit != null && _edit.Text != note.Message)
        {
            _edit.Text = note.Message;
        }
    }

    public void SetEditable(bool editable)
    {
        if (editable)
        {
            AddLineEdit();
        }
        else
        {
            AddLabel();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        if (_edit != null)
        {
            _edit.OnTextEntered -= Submitted;
            _edit.OnFocusExit -= Submitted;
        }

        OnSubmitted = null;
        OnRightClicked = null;
    }
}
