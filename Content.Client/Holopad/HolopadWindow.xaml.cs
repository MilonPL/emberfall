using Content.Client.UserInterface.Controls;
using Content.Shared.Holopad;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Telephone;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Client.Holopad;

[GenerateTypedNameReferences]
public sealed partial class HolopadWindow : FancyWindow
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    //playerManager

    private EntityUid? _owner = null;
    private HolopadUiKey _currentUiKey;
    private TelephoneState _currentState;
    private TimeSpan _buttonUnlockTime;
    private float _updateTimer = 0.1f;

    private const float UpdateTime = 0.1f;
    private TimeSpan _buttonUnlockDelay = TimeSpan.FromSeconds(0.5f);

    public event Action<NetEntity>? SendHolopadStartNewCallMessageAction;
    public event Action? SendHolopadAnswerCallMessageAction;
    public event Action? SendHolopadEndCallMessageAction;
    public event Action? SendHolopadStartBroadcastMessageAction;
    public event Action? SendHolopadActivateProjectorMessageAction;
    public event Action? SendHolopadRequestStationAiMessageAction;

    public HolopadWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _buttonUnlockTime = _gameTiming.RealTime + _buttonUnlockDelay;

        // Assign button actions
        AnswerCallButton.OnPressed += args => { OnHolopadAnswerCallMessage(); };
        EndCallButton.OnPressed += args => { OnHolopadEndCallMessage(); };
        StartBroadcastButton.OnPressed += args => { OnHolopadStartBroadcastMessage(); };
        ActivateProjectorButton.OnPressed += args => { OnHolopadActivateProjectorMessage(); };
        RequestStationAiButton.OnPressed += args => { OnHolopadRequestStationAiMessage(); };

        // XML formatting
        AnswerCallButton.AddStyleClass("ButtonColorGreen");
        EndCallButton.AddStyleClass("ButtonColorRed");
        StartBroadcastButton.AddStyleClass("ButtonColorRed");

        HolopadContactListPanel.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = new Color(47, 47, 59) * Color.DarkGray,
            BorderColor = new Color(82, 82, 82), //new Color(70, 73, 102),
            BorderThickness = new Thickness(2),
        };

        HolopadContactListHeaderPanel.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = new Color(82, 82, 82),
        };

        SubtitleText.SetMessage(FormattedMessage.FromMarkupOrThrow(Loc.GetString("holopad-window-subtitle")));
        OptionsText.SetMessage(FormattedMessage.FromMarkupOrThrow(Loc.GetString("holopad-window-options")));
    }

    #region: Button actions

    private void OnSendHolopadStartNewCallMessage(NetEntity receiver)
    {
        SendHolopadStartNewCallMessageAction?.Invoke(receiver);
    }

    private void OnHolopadAnswerCallMessage()
    {
        SendHolopadAnswerCallMessageAction?.Invoke();
    }

    private void OnHolopadEndCallMessage()
    {
        SendHolopadEndCallMessageAction?.Invoke();

        if (_currentUiKey == HolopadUiKey.AiRequestWindow)
            Close();
    }

    private void OnHolopadStartBroadcastMessage()
    {
        SendHolopadStartBroadcastMessageAction?.Invoke();
    }

    private void OnHolopadActivateProjectorMessage()
    {
        SendHolopadActivateProjectorMessageAction?.Invoke();
    }

    private void OnHolopadRequestStationAiMessage()
    {
        SendHolopadRequestStationAiMessageAction?.Invoke();
    }

    #endregion

    public void SetState(EntityUid owner, HolopadUiKey uiKey)
    {
        _owner = owner;
        _currentUiKey = uiKey;

        // Determines what UI containers are available to the user.
        // Components of these will be toggled on and off when
        // UpdateAppearance() is called

        switch (uiKey)
        {
            case HolopadUiKey.InteractionWindow:
                RequestStationAiContainer.Visible = true;
                HolopadContactListContainer.Visible = true;
                StartBroadcastContainer.Visible = true;
                break;

            case HolopadUiKey.InteractionWindowForAi:
                ActivateProjectorContainer.Visible = true;
                StartBroadcastContainer.Visible = true;
                break;

            case HolopadUiKey.AiActionWindow:
                HolopadContactListContainer.Visible = true;
                StartBroadcastContainer.Visible = true;
                break;

            case HolopadUiKey.AiRequestWindow:
                break;
        }
    }

    public void UpdateState(Dictionary<NetEntity, string> holopads, string? callerId = null)
    {
        if (_owner == null || !_entManager.TryGetComponent<TelephoneComponent>(_owner.Value, out var telephone))
            return;

        // Caller ID
        CallerIdText.SetMessage(FormattedMessage.FromMarkupOrThrow(callerId ?? Loc.GetString("holopad-window-unknown-caller")));
        CallerIdText.Visible = (callerId != null);

        // Sort holopads alphabetically
        var holopadArray = holopads.ToArray();
        Array.Sort(holopadArray, AlphabeticalSort);

        // Clear excess children from the contact list
        while (ContactsList.ChildCount > holopadArray.Length)
            ContactsList.RemoveChild(ContactsList.GetChild(ContactsList.ChildCount - 1));

        // Make / update required children
        for (int i = 0; i < holopadArray.Length; i++)
        {
            var (netEntity, label) = holopadArray[i];

            if (i >= ContactsList.ChildCount)
            {
                var newContactButton = new HolopadContactButton();
                newContactButton.OnPressed += args => { OnSendHolopadStartNewCallMessage(newContactButton.NetEntity); };

                ContactsList.AddChild(newContactButton);
            }

            var child = ContactsList.GetChild(i);

            if (child is not HolopadContactButton)
                continue;

            var contactButton = (HolopadContactButton)child;
            contactButton.UpdateValues(netEntity, label);
        }

        // Update buttons
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (_owner == null || !_entManager.TryGetComponent<TelephoneComponent>(_owner.Value, out var telephone))
            return;

        var hasBroadcastAccess = telephone.IsBroadcaster;

        // Temporarily disable the interface buttons when the call state changes to prevent any misclicks 
        if (_currentState != telephone.CurrentState)
        {
            _currentState = telephone.CurrentState;
            _buttonUnlockTime = _gameTiming.RealTime + _buttonUnlockDelay;
        }

        var lockButtons = _gameTiming.RealTime < _buttonUnlockTime;

        // Make / update required children
        foreach (var child in ContactsList.Children)
        {
            if (child is not HolopadContactButton)
                continue;

            var contactButton = (HolopadContactButton)child;
            contactButton.Disabled = (_currentState != TelephoneState.Idle || lockButtons);
        }

        // Update control text
        switch (_currentState)
        {
            case TelephoneState.Idle:
                CallStatusText.Text = Loc.GetString("holopad-window-no-calls-in-progress"); break;

            case TelephoneState.Calling:
                CallStatusText.Text = Loc.GetString("holopad-window-outgoing-call"); break;

            case TelephoneState.Ringing:
                CallStatusText.Text = (_currentUiKey == HolopadUiKey.AiRequestWindow) ?
                    Loc.GetString("holopad-window-ai-request") : Loc.GetString("holopad-window-incoming-call"); break;

            case TelephoneState.InCall:
                CallStatusText.Text = Loc.GetString("holopad-window-call-in-progress"); break;

            case TelephoneState.EndingCall:
                CallStatusText.Text = Loc.GetString("holopad-window-call-ending"); break;
        }

        // Update control disability
        AnswerCallButton.Disabled = (_currentState != TelephoneState.Ringing || lockButtons);
        EndCallButton.Disabled = (_currentState == TelephoneState.Idle || _currentState == TelephoneState.EndingCall || lockButtons);
        StartBroadcastButton.Disabled = (_currentState != TelephoneState.Idle || !hasBroadcastAccess || lockButtons);
        RequestStationAiButton.Disabled = (_currentState != TelephoneState.Idle || lockButtons);
        ActivateProjectorButton.Disabled = (_currentState != TelephoneState.Idle || lockButtons);

        // Update control visibility
        FetchingAvailableHolopadsContainer.Visible = (ContactsList.ChildCount == 0);
        ActiveCallControlsContainer.Visible = (_currentState != TelephoneState.Idle || _currentUiKey == HolopadUiKey.AiRequestWindow);
        CallPlacementControlsContainer.Visible = !ActiveCallControlsContainer.Visible;

        AnswerCallButton.Visible = (_currentState == TelephoneState.Ringing);
        StartBroadcastButton.Visible = telephone.IsBroadcaster;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _updateTimer += args.DeltaSeconds;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;
            UpdateAppearance();
        }
    }

    private sealed class HolopadContactButton : Button
    {
        public NetEntity NetEntity;

        public HolopadContactButton()
        {
            HorizontalExpand = true;
            SetHeight = 32;
            Margin = new Thickness(0f, 1f, 0f, 1f);
        }

        public void UpdateValues(NetEntity netEntity, string label)
        {
            NetEntity = netEntity;
            Text = Loc.GetString("holopad-window-contact-label", ("label", label));
        }
    }

    private int AlphabeticalSort(KeyValuePair<NetEntity, string> x, KeyValuePair<NetEntity, string> y)
    {
        if (string.IsNullOrEmpty(x.Value))
            return -1;

        if (string.IsNullOrEmpty(y.Value))
            return 1;

        return x.Value.CompareTo(y.Value);
    }
}
