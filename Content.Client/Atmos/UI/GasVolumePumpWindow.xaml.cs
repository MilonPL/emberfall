using System;
using System.Collections.Generic;
using System.Globalization;
using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Localization;

namespace Content.Client.Atmos.UI
{
    /// <summary>
    /// Client-side UI used to control a gas volume pump.
    /// </summary>
    [GenerateTypedNameReferences]
    public partial class GasVolumePumpWindow : DefaultWindow
    {
        public bool PumpStatus = true;

        public event Action? ToggleStatusButtonPressed;
        public event Action<string>? PumpTransferRateChanged;

        public GasVolumePumpWindow()
        {
            RobustXamlLoader.Load(this);

            ToggleStatusButton.OnPressed += _ => SetPumpStatus(!PumpStatus);
            ToggleStatusButton.OnPressed += _ => ToggleStatusButtonPressed?.Invoke();

            PumpTransferRateInput.OnTextChanged += _ => SetTransferRateButton.Disabled = false;
            SetTransferRateButton.OnPressed += _ =>
            {
                PumpTransferRateChanged?.Invoke(PumpTransferRateInput.Text ??= "");
                SetTransferRateButton.Disabled = true;
            };

            SetMaxRateButton.OnPressed += _ =>
            {
                PumpTransferRateInput.Text = Atmospherics.MaxTransferRate.ToString(CultureInfo.InvariantCulture);
                SetTransferRateButton.Disabled = false;
            };
        }

        public void SetTransferRate(float rate)
        {
            PumpTransferRateInput.Text = rate.ToString(CultureInfo.InvariantCulture);
        }

        public void SetPumpStatus(bool enabled)
        {
            PumpStatus = enabled;
            if (enabled)
            {
                ToggleStatusButton.Text = Loc.GetString("comp-gas-pump-ui-status-enabled");
            }
            else
            {
                ToggleStatusButton.Text = Loc.GetString("comp-gas-pump-ui-status-disabled");
            }
        }
    }
}
