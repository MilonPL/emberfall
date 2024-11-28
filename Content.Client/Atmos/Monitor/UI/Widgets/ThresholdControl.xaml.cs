using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

// holy FUCK
// this technically works because some of this you can *not* do in XAML but holy FUCK

namespace Content.Client.Atmos.Monitor.UI.Widgets;

[GenerateTypedNameReferences]
public sealed partial class ThresholdControl : BoxContainer
{
    public event Action<AtmosMonitorThresholdType, AtmosAlarmThreshold, Gas?>? ThresholdDataChanged;

    private CollapsibleHeading Heading => CName;
    private CheckBox Enabled => CEnabled;
    private BoxContainer DangerBounds => CDangerBounds;
    private BoxContainer WarningBounds => CWarningBounds;
    private ThresholdBoundControl _upperBoundControl;
    private ThresholdBoundControl _lowerBoundControl;
    private ThresholdBoundControl _upperWarningBoundControl;
    private ThresholdBoundControl _lowerWarningBoundControl;

    // i have played myself by making threshold values nullable to
    // indicate validity/disabled status, with several layers of side effect
    // dependent on the other three values when you change one :HECK:
    public ThresholdControl(string name, AtmosAlarmThreshold threshold, AtmosMonitorThresholdType type, Gas? gas = null, float modifier = 1)
    {
        RobustXamlLoader.Load(this);

        var alarmThreshold = threshold;
        var thresholdType = type;
        var gasType = gas;

        Heading.Title = name;

        // i miss rust macros

        _upperBoundControl = new ThresholdBoundControl(LabelForBound("upper-bound"), alarmThreshold.UpperBound.Value, modifier);
        _upperBoundControl.OnBoundChanged += (value) =>
        {
            alarmThreshold.SetLimit(AtmosMonitorLimitType.UpperDanger, value);
        };
        _upperBoundControl.OnBoundEnabled += (isEnabled) =>
        {
            alarmThreshold.SetEnabled(AtmosMonitorLimitType.UpperDanger, isEnabled);
        };
        _upperBoundControl.OnValidBoundChanged += () =>
        {
            ThresholdDataChanged!.Invoke(thresholdType, alarmThreshold, gasType);
        };
        DangerBounds.AddChild(_upperBoundControl);

        _lowerBoundControl = new ThresholdBoundControl(LabelForBound("lower-bound"), alarmThreshold.LowerBound.Value, modifier);
        _lowerBoundControl.OnBoundChanged += value =>
        {
            alarmThreshold.SetLimit(AtmosMonitorLimitType.LowerDanger, value);
        };
        _lowerBoundControl.OnBoundEnabled += (isEnabled) =>
        {
            alarmThreshold.SetEnabled(AtmosMonitorLimitType.LowerDanger, isEnabled);
        };
        _lowerBoundControl.OnValidBoundChanged += () =>
        {
            ThresholdDataChanged!.Invoke(thresholdType, alarmThreshold, gasType);
        };
        DangerBounds.AddChild(_lowerBoundControl);

        _upperWarningBoundControl = new ThresholdBoundControl(LabelForBound("upper-warning-bound"), alarmThreshold.UpperWarningBound.Value, modifier);
        _upperWarningBoundControl.OnBoundChanged += value =>
        {
            alarmThreshold.SetLimit(AtmosMonitorLimitType.UpperWarning, value);
        };
        _upperWarningBoundControl.OnBoundEnabled += (isEnabled) =>
        {
            alarmThreshold.SetEnabled(AtmosMonitorLimitType.UpperWarning, isEnabled);
        };
        _upperWarningBoundControl.OnValidBoundChanged += () =>
        {
            ThresholdDataChanged!.Invoke(thresholdType, alarmThreshold, gasType);
        };
        WarningBounds.AddChild(_upperWarningBoundControl);

        _lowerWarningBoundControl = new ThresholdBoundControl(LabelForBound("lower-warning-bound"), alarmThreshold.LowerWarningBound.Value, modifier);
        _lowerWarningBoundControl.OnBoundChanged += value =>
        {
            alarmThreshold.SetLimit(AtmosMonitorLimitType.LowerWarning, value);
        };
        _lowerWarningBoundControl.OnBoundEnabled += (isEnabled) =>
        {
            alarmThreshold.SetEnabled(AtmosMonitorLimitType.LowerWarning, isEnabled);
        };
        _lowerWarningBoundControl.OnValidBoundChanged += () =>
        {
            ThresholdDataChanged!.Invoke(thresholdType, alarmThreshold, gasType);
        };

        WarningBounds.AddChild(_lowerWarningBoundControl);

        Enabled.OnToggled += args =>
        {
            alarmThreshold.Ignore = !args.Pressed;
            ThresholdDataChanged!.Invoke(thresholdType, alarmThreshold, gasType);
        };
        Enabled.Pressed = !alarmThreshold.Ignore;
    }

    private string LabelForBound(string boundType) //<todo.eoin Replace this with enums
    {
        return Loc.GetString($"air-alarm-ui-thresholds-{boundType}");
    }

    public void UpdateThresholdData(AtmosAlarmThreshold threshold, float currentAmount)
    {
        threshold.CheckThreshold(currentAmount, out var alarm, out var which);

        var upperDangerState = AtmosAlarmType.Normal;
        var lowerDangerState = AtmosAlarmType.Normal;
        var upperWarningState = AtmosAlarmType.Normal;
        var lowerWarningState = AtmosAlarmType.Normal;

        switch (alarm)
        {
            case AtmosAlarmType.Danger when which == AtmosMonitorThresholdBound.Upper:
                upperDangerState = alarm;
                break;
            case AtmosAlarmType.Danger:
                lowerDangerState = alarm;
                break;
            case AtmosAlarmType.Warning when which == AtmosMonitorThresholdBound.Upper:
                upperWarningState = alarm;
                break;
            case AtmosAlarmType.Warning:
                lowerWarningState = alarm;
                break;
        }

        _upperBoundControl.SetValue(threshold.UpperBound.Value);
        _upperBoundControl.SetEnabled(threshold.UpperBound.Enabled);
        _upperBoundControl.SetWarningState(upperDangerState);

        _lowerBoundControl.SetValue(threshold.LowerBound.Value);
        _lowerBoundControl.SetEnabled(threshold.LowerBound.Enabled);
        _lowerBoundControl.SetWarningState(lowerDangerState);

        _upperWarningBoundControl.SetValue(threshold.UpperWarningBound.Value);
        _upperWarningBoundControl.SetEnabled(threshold.UpperWarningBound.Enabled);
        _upperWarningBoundControl.SetWarningState(upperWarningState);

        _lowerWarningBoundControl.SetValue(threshold.LowerWarningBound.Value);
        _lowerWarningBoundControl.SetEnabled(threshold.LowerWarningBound.Enabled);
        _lowerWarningBoundControl.SetWarningState(lowerWarningState);

        Enabled.Pressed = !threshold.Ignore;
    }
}
