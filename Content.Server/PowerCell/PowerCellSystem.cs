using Content.Server.Administration.Logs;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Rounding;
using Robust.Shared.Containers;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Kitchen.Components;
using Content.Server.UserInterface;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;

namespace Content.Server.PowerCell;

public sealed class PowerCellSystem : SharedPowerCellSystem
{
    [Dependency] private readonly ActivatableUISystem _activatable = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _sharedAppearanceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerCellComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<PowerCellComponent, SolutionChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<PowerCellComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<PowerCellComponent, ExaminedEvent>(OnCellExamined);

        // funny
        SubscribeLocalEvent<PowerCellSlotComponent, BeingMicrowavedEvent>(OnSlotMicrowaved);
        SubscribeLocalEvent<BatteryComponent, BeingMicrowavedEvent>(OnMicrowaved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<PowerCellDrawComponent, PowerCellSlotComponent>();

        while (query.MoveNext(out var uid, out var comp, out var slot))
        {
            if (!comp.Enabled)
                continue;

            if (!TryGetBatteryFromSlot(uid, out var battery, slot))
                continue;

            if (battery.TryUseCharge(comp.DrawRate * frameTime))
                continue;

            comp.Enabled = false;
            var ev = new PowerCellSlotEmptyEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void OnRejuvenate(EntityUid uid, PowerCellComponent component, RejuvenateEvent args)
    {
        component.IsRigged = false;
    }

    private void OnSlotMicrowaved(EntityUid uid, PowerCellSlotComponent component, BeingMicrowavedEvent args)
    {
        if (_itemSlotsSystem.TryGetSlot(uid, component.CellSlotId, out ItemSlot? slot))
        {
            if (slot.Item == null)
                return;

            RaiseLocalEvent(slot.Item.Value, args, false);
        }
    }

    private void OnMicrowaved(EntityUid uid, BatteryComponent component, BeingMicrowavedEvent args)
    {
        if (component.CurrentCharge == 0)
            return;

        args.Handled = true;

        // What the fuck are you doing???
        Explode(uid, component, args.User);
    }

    private void OnChargeChanged(EntityUid uid, PowerCellComponent component, ref ChargeChangedEvent args)
    {
        if (component.IsRigged)
        {
            Explode(uid, cause: null);
            return;
        }

        if (!TryComp(uid, out BatteryComponent? battery))
            return;

        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        var frac = battery.CurrentCharge / battery.MaxCharge;
        var level = (byte) ContentHelpers.RoundToNearestLevels(frac, 1, PowerCellComponent.PowerCellVisualsLevels);
        _sharedAppearanceSystem.SetData(uid, PowerCellVisuals.ChargeLevel, level, appearance);

        // If this power cell is inside a cell-slot, inform that entity that the power has changed (for updating visuals n such).
        if (_containerSystem.TryGetContainingContainer(uid, out var container)
            && TryComp(container.Owner, out PowerCellSlotComponent? slot)
            && _itemSlotsSystem.TryGetSlot(container.Owner, slot.CellSlotId, out ItemSlot? itemSlot))
        {
            if (itemSlot.Item == uid)
                RaiseLocalEvent(container.Owner, new PowerCellChangedEvent(false), false);
        }
    }

    private void Explode(EntityUid uid, BatteryComponent? battery = null, EntityUid? cause = null)
    {
        if (!Resolve(uid, ref battery))
            return;

        var radius = MathF.Min(5, MathF.Sqrt(battery.CurrentCharge) / 9);

        _explosionSystem.TriggerExplosive(uid, radius: radius, user:cause);
        QueueDel(uid);
    }

    #region Activatable

    /// <summary>
    /// Returns whether the entity has a slotted battery and <see cref="PowerCellDrawComponent.UseRate"/> charge.
    /// </summary>
    /// <param name="user">Popup to this user with the relevant detail if specified.</param>
    public bool HasActivatableCharge(EntityUid uid, PowerCellDrawComponent? battery = null, PowerCellSlotComponent? cell = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref battery, ref cell, false))
            return false;

        return HasCharge(uid, battery.UseRate, cell, user);
    }

    /// <summary>
    /// Tries to use the <see cref="PowerCellDrawComponent.UseRate"/> for this entity.
    /// </summary>
    /// <param name="user">Popup to this user with the relevant detail if specified.</param>
    public bool TryUseActivatableCharge(EntityUid uid, PowerCellDrawComponent? battery = null, PowerCellSlotComponent? cell = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref battery, ref cell, false))
            return false;

        if (TryUseCharge(uid, battery.UseRate, cell, user))
        {
            _activatable.CheckUsage(uid);
            return true;
        }

        return false;
    }

    #endregion

    /// <summary>
    /// Returns whether the entity has a slotted battery and charge for the requested action.
    /// </summary>
    /// <param name="user">Popup to this user with the relevant detail if specified.</param>
    public bool HasCharge(EntityUid uid, float charge, PowerCellSlotComponent? component = null, EntityUid? user = null)
    {
        if (!TryGetBatteryFromSlot(uid, out var battery, component))
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("power-cell-no-battery"), uid, user.Value);

            return false;
        }

        if (battery.CurrentCharge < charge)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("power-cell-insufficient"), uid, user.Value);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to use charge from a slotted battery.
    /// </summary>
    public bool TryUseCharge(EntityUid uid, float charge, PowerCellSlotComponent? component = null, EntityUid? user = null)
    {
        if (!TryGetBatteryFromSlot(uid, out var battery, component))
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("power-cell-no-battery"), uid, user.Value);

            return false;
        }

        if (!battery.TryUseCharge(charge))
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("power-cell-insufficient"), uid, user.Value);

            return false;
        }

        return true;
    }

    public bool TryGetBatteryFromSlot(EntityUid uid, [NotNullWhen(true)] out BatteryComponent? battery, PowerCellSlotComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
        {
            battery = null;
            return false;
        }

        if (_itemSlotsSystem.TryGetSlot(uid, component.CellSlotId, out ItemSlot? slot))
        {
            return TryComp(slot.Item, out battery);
        }

        battery = null;
        return false;
    }

    private void OnSolutionChange(EntityUid uid, PowerCellComponent component, SolutionChangedEvent args)
    {
        component.IsRigged = _solutionsSystem.TryGetSolution(uid, PowerCellComponent.SolutionName, out var solution)
                               && solution.TryGetReagent("Plasma", out var plasma)
                               && plasma >= 5;

        if (component.IsRigged)
        {
            _adminLogger.Add(LogType.Explosion, LogImpact.Medium, $"Power cell {ToPrettyString(uid)} has been rigged up to explode when used.");
        }
    }

    private void OnCellExamined(EntityUid uid, PowerCellComponent component, ExaminedEvent args)
    {
        if (!TryComp(uid, out BatteryComponent? battery))
            return;

        var charge = battery.CurrentCharge / battery.MaxCharge * 100;
        args.PushMarkup(Loc.GetString("power-cell-component-examine-details", ("currentCharge", $"{charge:F0}")));
    }
}
