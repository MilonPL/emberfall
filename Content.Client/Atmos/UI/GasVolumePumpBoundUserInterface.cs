﻿using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Binary.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Atmos.UI;

/// <summary>
/// Initializes a <see cref="GasVolumePumpWindow"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public class GasVolumePumpBoundUserInterface : BoundUserInterface
{

    private GasVolumePumpWindow? _window;
    private const float MaxTransferRate = Atmospherics.MaxTransferRate;

    public GasVolumePumpBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new GasVolumePumpWindow();

        if(State != null)
            UpdateState(State);

        _window.OpenCentered();

        _window.OnClose += Close;

        _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
        _window.PumpTransferRateChanged += OnPumpTransferRatePressed;
    }

    private void OnToggleStatusButtonPressed()
    {
        if (_window is null) return;
        SendMessage(new GasVolumePumpToggleStatusMessage(_window.PumpStatus));
    }

    private void OnPumpTransferRatePressed(string value)
    {
        float rate = float.TryParse(value, out var parsed) ? parsed : 0f;
        if (rate > MaxTransferRate) rate = MaxTransferRate;

        SendMessage(new GasVolumePumpChangeTransferRateMessage(rate));
    }

    /// <summary>
    /// Update the UI state based on server-sent info
    /// </summary>
    /// <param name="state"></param>
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not GasVolumePumpBoundUserInterfaceState cast)
            return;

        _window.Title = (cast.PumpLabel);
        _window.SetPumpStatus(cast.Enabled);
        _window.SetTransferRate(cast.TransferRate);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _window?.Dispose();
    }
}