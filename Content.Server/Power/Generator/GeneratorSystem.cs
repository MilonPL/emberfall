﻿using Content.Server.Audio;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Power.Generator;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;

namespace Content.Server.Power.Generator;

/// <inheritdoc/>
/// <seealso cref="FuelGeneratorComponent"/>
/// <seealso cref="ChemicalFuelGeneratorAdapterComponent"/>
/// <seealso cref="SolidFuelGeneratorAdapterComponent"/>
public sealed class GeneratorSystem : SharedGeneratorSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;

    private EntityQuery<UpgradePowerSupplierComponent> _upgradeQuery;

    public override void Initialize()
    {
        _upgradeQuery = GetEntityQuery<UpgradePowerSupplierComponent>();

        SubscribeLocalEvent<SolidFuelGeneratorAdapterComponent, InteractUsingEvent>(OnSolidFuelAdapterInteractUsing);
        SubscribeLocalEvent<ChemicalFuelGeneratorAdapterComponent, InteractUsingEvent>(OnChemicalFuelAdapterInteractUsing);
        SubscribeLocalEvent<FuelGeneratorComponent, PortableGeneratorSetTargetPowerMessage>(OnTargetPowerSet);
    }

    private void OnChemicalFuelAdapterInteractUsing(EntityUid uid, ChemicalFuelGeneratorAdapterComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<SolutionContainerManagerComponent>(args.Used, out var solutions) ||
            !TryComp<FuelGeneratorComponent>(uid, out var generator))
            return;

        if (!(component.Whitelist?.IsValid(args.Used) ?? true))
            return;

        if (TryComp<ChemicalFuelGeneratorDirectSourceComponent>(args.Used, out var source))
        {
            if (!solutions.Solutions.ContainsKey(source.Solution))
            {
                Log.Error($"Couldn't get solution {source.Solution} on {ToPrettyString(args.Used)}");
                return;
            }

            var solution = solutions.Solutions[source.Solution];
            generator.RemainingFuel += ReagentsToFuel(component, solution);
            solution.RemoveAllSolution();
            QueueDel(args.Used);
        }
    }

    private float ReagentsToFuel(ChemicalFuelGeneratorAdapterComponent component, Solution solution)
    {
        var total = 0.0f;
        foreach (var reagent in solution.Contents)
        {
            if (!component.ChemConversionFactors.ContainsKey(reagent.ReagentId))
                continue;

            total += reagent.Quantity.Float() * component.ChemConversionFactors[reagent.ReagentId];
        }

        return total;
    }

    private void OnTargetPowerSet(EntityUid uid, FuelGeneratorComponent component, PortableGeneratorSetTargetPowerMessage args)
    {
        component.TargetPower = Math.Clamp(
            args.TargetPower,
            component.MinTargetPower / 1000,
            component.MaxTargetPower / 1000) * 1000;
    }

    public void SetFuelGeneratorOn(EntityUid uid, bool on, FuelGeneratorComponent? generator = null)
    {
        if (!Resolve(uid, ref generator))
            return;

        generator.On = on;
    }

    private void OnSolidFuelAdapterInteractUsing(EntityUid uid, SolidFuelGeneratorAdapterComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PhysicalCompositionComponent>(args.Used, out var mat) ||
            !HasComp<MaterialComponent>(args.Used)  ||
            !TryComp<FuelGeneratorComponent>(uid, out var generator))
            return;

        if (!mat.MaterialComposition.ContainsKey(component.FuelMaterial))
            return;

        _popup.PopupEntity(Loc.GetString("generator-insert-material", ("item", args.Used), ("generator", uid)), uid);
        generator.RemainingFuel += _stack.GetCount(args.Used) * component.Multiplier;
        QueueDel(args.Used);
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<FuelGeneratorComponent, PowerSupplierComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var gen, out var supplier, out var xform))
        {
            supplier.Enabled = gen.On && gen.RemainingFuel > 0.0f && xform.Anchored;

            if (supplier.Enabled)
            {
                var upgradeMultiplier = _upgradeQuery.CompOrNull(uid)?.ActualScalar ?? 1f;

                supplier.MaxSupply = gen.TargetPower * upgradeMultiplier;

                var eff = 1 / CalcFuelEfficiency(gen.TargetPower, gen.OptimalPower, gen);

                gen.RemainingFuel = MathF.Max(gen.RemainingFuel - (gen.OptimalBurnRate * frameTime * eff), 0.0f);
            }

            _appearance.SetData(uid, GeneratorVisuals.Running, supplier.Enabled);
            _ambientSound.SetAmbience(uid, supplier.Enabled);
        }
    }
}
