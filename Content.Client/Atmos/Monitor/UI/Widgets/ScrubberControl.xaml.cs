using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Stylesheets;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Localization;

namespace Content.Client.Atmos.Monitor.UI.Widgets;

[GenerateTypedNameReferences]
public sealed partial class ScrubberControl : BoxContainer
{
    private GasVentScrubberData _data;
    private string _address;

    public event Action<string, IAtmosDeviceData>? ScrubberDataChanged;
	public event Action<IAtmosDeviceData>? ScrubberDataCopied;

    private CheckBox _enabled => CEnableDevice;
    private CollapsibleHeading _addressLabel => CAddress;
    private OptionButton _pumpDirection => CPumpDirection;
    private FloatSpinBox _volumeRate => CVolumeRate;
    private FloatSpinBox _targetPressure => CTargetPressure;
    private CheckBox _wideNet => CWideNet;
	private Button _copySettings => CCopySettings;
    private GridContainer _gases => CGasContainer;
    private Dictionary<Gas, Button> _gasControls = new();

    public ScrubberControl(GasVentScrubberData data, string address)
    {
        RobustXamlLoader.Load(this);

        Name = address;

        _data = data;
        _address = address;

        _addressLabel.Title = Loc.GetString("air-alarm-ui-atmos-net-device-label", ("address", $"{address}"));

        _enabled.Pressed = data.Enabled;
        _enabled.OnToggled += _ =>
        {
            _data.Enabled = _enabled.Pressed;
            ScrubberDataChanged?.Invoke(_address, _data);
        };

        _wideNet.Pressed = data.WideNet;
        _wideNet.OnToggled += _ =>
        {
            _data.WideNet = _wideNet.Pressed;
            ScrubberDataChanged?.Invoke(_address, _data);
        };

        _volumeRate.Value = _data.VolumeRate;
        _volumeRate.OnValueChanged += _ =>
        {
            _data.VolumeRate = _volumeRate.Value;
            ScrubberDataChanged?.Invoke(_address, _data);
        };
        _volumeRate.IsValid += value => value >= 0;

        _targetPressure.Value = _data.TargetPressure;
        _targetPressure.OnValueChanged += _ =>
        {
            _data.TargetPressure = _targetPressure.Value;
            ScrubberDataChanged?.Invoke(address, _data);
        };

        foreach (var value in Enum.GetValues<ScrubberPumpDirection>())
        {
            _pumpDirection.AddItem(Loc.GetString($"{value}"), (int) value);
        }

        _pumpDirection.SelectId((int) _data.PumpDirection);
        _pumpDirection.OnItemSelected += args =>
        {
            _pumpDirection.SelectId(args.Id);
            _data.PumpDirection = (ScrubberPumpDirection) args.Id;
            ScrubberDataChanged?.Invoke(_address, _data);
        };

		_copySettings.OnPressed += _ =>
		{
			ScrubberDataCopied?.Invoke(_data);
		};

        foreach (var value in Enum.GetValues<Gas>())
        {
            var gasButton = new Button
            {
                Name = value.ToString(),
                Text = Loc.GetString($"{value}"),
                ToggleMode = true,
                HorizontalExpand = true
            };
            if (_data.PriorityGases.Contains(value))
            {
                gasButton.StyleClasses.Add("ButtonColorGreen");
            }
            if (_data.DisabledGases.Contains(value))
            {
                gasButton.StyleClasses.Add(StyleBase.ButtonCaution);
            }
            gasButton.OnPressed += args =>
            {
                int state = 0;
                if (_data.DisabledGases.Contains(value))
                {
                    state = 2;
                }
                else if (_data.PriorityGases.Contains(value))
                {
                    state = 1;
                }

                _data.PriorityGases.Remove(value);
                _data.DisabledGases.Remove(value);
                gasButton.StyleClasses.Remove("ButtonColorGreen");
                gasButton.StyleClasses.Remove(StyleBase.ButtonCaution);
                gasButton.Pressed = false;
                if (state == 0)
                {
                    _data.PriorityGases.Add(value);
                    gasButton.StyleClasses.Add("ButtonColorGreen");
                }
                else if (state == 1)
                {
                    _data.DisabledGases.Add(value);
                    gasButton.StyleClasses.Add(StyleBase.ButtonCaution);
                }

                ScrubberDataChanged?.Invoke(_address, _data);
            };
            _gasControls.Add(value, gasButton);
            _gases.AddChild(gasButton);
        }

    }

    public void ChangeData(GasVentScrubberData data)
    {
        _data.Enabled = data.Enabled;
        _enabled.Pressed = _data.Enabled;

        _data.PumpDirection = data.PumpDirection;
        _pumpDirection.Select((int) _data.PumpDirection);

        _data.VolumeRate = data.VolumeRate;
        _volumeRate.Value = _data.VolumeRate;

        _data.TargetPressure = data.TargetPressure;
        _targetPressure.Value = _data.TargetPressure;

        _data.WideNet = data.WideNet;
        _wideNet.Pressed = _data.WideNet;
        _data.PriorityGases = data.PriorityGases;
        _data.DisabledGases = data.DisabledGases;

        foreach (var value in Enum.GetValues<Gas>())
        {
            _gasControls[value].StyleClasses.Remove("ButtonColorGreen");
            _gasControls[value].StyleClasses.Remove(StyleBase.ButtonCaution);
            if (data.DisabledGases.Contains(value))
            {
                _gasControls[value].StyleClasses.Add(StyleBase.ButtonCaution);
            }
            else if (data.PriorityGases.Contains(value))
            {
                _gasControls[value].StyleClasses.Add("ButtonColorGreen");
            }
        }
    }
}
