﻿using Content.Client.DoAfter;
using Content.Client.UserInterface.Controls;
using Content.Shared.Power.Generator;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Power.Generator;

[GenerateTypedNameReferences]
public sealed partial class GeneratorWindow : FancyWindow
{
    private readonly EntityUid _entity;

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    private readonly SharedPowerSwitchableSystem _switchable;
    private readonly FuelGeneratorComponent? _component;
    private PortableGeneratorComponentBuiState? _lastState;

    public GeneratorWindow(PortableGeneratorBoundUserInterface bui, EntityUid entity)
    {
        _entity = entity;
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _entityManager.TryGetComponent(entity, out _component);
        _switchable = _entityManager.System<SharedPowerSwitchableSystem>();

        EntityView.SetEntity(entity);
        TargetPower.IsValid += IsValid;
        TargetPower.ValueChanged += (args) =>
        {
            bui.SetTargetPower(args.Value);
        };

        StartButton.OnPressed += _ => bui.Start();
        StopButton.OnPressed += _ => bui.Stop();
        OutputSwitchButton.OnPressed += _ => bui.SwitchOutput();
        FuelEject.OnPressed += _ => bui.EjectFuel();
    }

    private bool IsValid(int arg)
    {
        if (arg < 0)
            return false;

        if (arg > (_lastState?.MaximumPower / 1000.0f ?? 0))
            return false;

        return true;
    }

    public void Update(PortableGeneratorComponentBuiState state)
    {
        if (_component == null)
            return;

        _lastState = state;
        if (!TargetPower.LineEditControl.HasKeyboardFocus())
            TargetPower.OverrideValue((int)(state.TargetPower / 1000.0f));
        var efficiency = SharedGeneratorSystem.CalcFuelEfficiency(state.TargetPower, state.OptimalPower, _component);
        Efficiency.Text = efficiency.ToString("P1");

        var burnRate = _component.OptimalBurnRate / efficiency;
        var left = state.RemainingFuel / burnRate;

        Eta.Text = Loc.GetString(
            "portable-generator-ui-eta",
            ("minutes", Math.Ceiling(left / 60.0)));
        FuelFraction.Value = state.RemainingFuel - (int) state.RemainingFuel;
        FuelLeft.Text = ((int) MathF.Floor(state.RemainingFuel)).ToString();

        var progress = 0f;

        var unanchored = !_entityManager.GetComponent<TransformComponent>(_entity).Anchored;
        var starting = !unanchored && TryGetStartProgress(out progress);
        var on = !unanchored && !starting && state.On;
        var off = !unanchored && !starting && !state.On;

        LabelUnanchored.Visible = unanchored;
        StartProgress.Visible = starting;
        StopButton.Visible = on;
        StartButton.Visible = off;

        if (starting)
        {
            StatusLabel.Text = _loc.GetString("portable-generator-ui-status-starting");
            StatusLabel.SetOnlyStyleClass("Caution");

            StartProgress.Value = progress;
        }
        else if (on)
        {
            StatusLabel.Text = _loc.GetString("portable-generator-ui-status-running");
            StatusLabel.SetOnlyStyleClass("Good");
        }
        else
        {
            StatusLabel.Text = _loc.GetString("portable-generator-ui-status-stopped");
            StatusLabel.SetOnlyStyleClass("Danger");
        }

        var canSwitch = _entityManager.TryGetComponent(_entity, out PowerSwitchableComponent? switchable);
        OutputSwitchLabel.Visible = canSwitch;
        OutputSwitchButton.Visible = canSwitch;

        if (switchable != null)
        {
            var voltage = _switchable.VoltageString(_switchable.GetVoltage(_entity, switchable));
            OutputSwitchLabel.Text = Loc.GetString("portable-generator-ui-current-output", ("voltage", voltage));
            var nextVoltage = _switchable.VoltageString(_switchable.GetNextVoltage(_entity, switchable));
            OutputSwitchButton.Text = Loc.GetString("power-switchable-switch-voltage", ("voltage", nextVoltage));
            OutputSwitchButton.Disabled = state.On;
        }

        CloggedLabel.Visible = state.Clogged;
    }

    private bool TryGetStartProgress(out float progress)
    {
        // Try to check progress of auto-revving first
        if (_entityManager.TryGetComponent<ActiveGeneratorRevvingComponent>(_entity, out var activeGeneratorRevvingComponent) && _entityManager.TryGetComponent<PortableGeneratorComponent>(_entity, out var portableGeneratorComponent))
        {
            var calculatedProgress = activeGeneratorRevvingComponent.CurrentTime / portableGeneratorComponent.StartTime;
            progress = (float) calculatedProgress;
            return true;
        }

        var doAfterSystem = _entityManager.EntitySysManager.GetEntitySystem<DoAfterSystem>();
        return doAfterSystem.TryFindActiveDoAfter<GeneratorStartedEvent>(_entity, out _, out _, out progress);
    }
}
