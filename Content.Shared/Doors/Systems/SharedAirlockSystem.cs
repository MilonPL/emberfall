using Content.Shared.Doors.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.Wires;
using Robust.Shared.Timing;

namespace Content.Shared.Doors.Systems;

public abstract class SharedAirlockSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedDoorSystem DoorSystem = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly SharedWiresSystem _wiresSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        SubscribeLocalEvent<AirlockComponent, DoorStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<AirlockComponent, DoorBoltsChangedEvent>(OnBoltsChanged);
        SubscribeLocalEvent<AirlockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<AirlockComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
        SubscribeLocalEvent<AirlockComponent, GetPryTimeModifierEvent>(OnGetPryMod);
        SubscribeLocalEvent<AirlockComponent, BeforePryEvent>(OnBeforePry);
    }

    private void OnBeforeDoorClosed(Entity<AirlockComponent> airlock, ref BeforeDoorClosedEvent args)
    {
        if (args.Cancelled)
            return;

        if (!airlock.Comp.Safety)
            args.PerformCollisionCheck = false;

        // only block based on bolts / power status when initially closing the door, not when its already
        // mid-transition. Particularly relevant for when the door was pried-closed with a crowbar, which bypasses
        // the initial power-check.

        if (!TryComp(airlock, out DoorComponent? door)
            || door.State is DoorState.OpeningInProgress or DoorState.ClosingInProgress
            || CanChangeState(airlock,
                door.State is DoorState.AttemptingCloseByPrying or DoorState.AttemptingOpenByPrying))
            return;

        args.Cancel();
    }

    private void OnStateChanged(Entity<AirlockComponent> airlock, ref DoorStateChangedEvent args)
    {
        // This is here so we don't accidentally bulldoze state values and mispredict.
        if (_timing.ApplyingState)
            return;

        // Only show the maintenance panel if the airlock is closed
        if (TryComp<WiresPanelComponent>(airlock, out var wiresPanel))
        {
            _wiresSystem.ChangePanelVisibility(airlock,
                wiresPanel,
                airlock.Comp.OpenPanelVisible || args.State != DoorState.Open);
        }

        // If the door is closed, we should look if the bolt was locked while closing
        UpdateAutoClose(airlock);

        // Make sure the airlock auto closes again next time it is opened
        if (args.State != DoorState.Closed)
            return;

        airlock.Comp.AutoClose = true;
        Dirty(airlock);
    }

    private void OnBoltsChanged(Entity<AirlockComponent> airlock, ref DoorBoltsChangedEvent args)
    {
        // If unbolted, reset the auto close timer
        if (args.BoltsDown)
            return;

        UpdateAutoClose(airlock);
    }

    private void OnBeforeDoorOpened(Entity<AirlockComponent> airlock, ref BeforeDoorOpenedEvent args)
    {
        if (CanChangeState(airlock))
            return;

        args.Cancel();
    }

    private void OnBeforeDoorDenied(Entity<AirlockComponent> airlock, ref BeforeDoorDeniedEvent args)
    {
        if (CanChangeState(airlock))
            return;

        args.Cancel();
    }

    private void OnGetPryMod(EntityUid uid, AirlockComponent component, ref GetPryTimeModifierEvent args)
    {
        if (component.Powered)
            args.PryTimeModifier *= component.PoweredPryModifier;

        if (DoorSystem.IsBolted(uid))
            args.PryTimeModifier *= component.BoltedPryModifier;
    }

    /// <summary>
    /// Updates the auto close timer.
    /// </summary>
    public void UpdateAutoClose(Entity<AirlockComponent> airlock, DoorComponent? door = null)
    {
        if (!Resolve(airlock, ref door))
            return;

        if (door.State != DoorState.Open)
            return;

        if (!airlock.Comp.AutoClose)
            return;

        if (!CanChangeState(airlock))
            return;

        var autoCloseEvent = new BeforeDoorAutoCloseEvent();
        RaiseLocalEvent(airlock, autoCloseEvent);

        if (autoCloseEvent.Cancelled)
            return;

        DoorSystem.SetNextStateChange((airlock, door),
            airlock.Comp.AutoCloseDelay * airlock.Comp.AutoCloseDelayModifier);
    }

    private void OnBeforePry(EntityUid uid, AirlockComponent component, ref BeforePryEvent args)
    {
        if (args.Cancelled)
            return;

        if (!component.Powered || args.PryPowered)
            return;

        args.Message = "airlock-component-cannot-pry-is-powered-message";

        args.Cancelled = true;
    }

    public void UpdateEmergencyLightStatus(EntityUid uid, AirlockComponent component)
    {
        Appearance.SetData(uid, DoorVisuals.EmergencyLights, component.EmergencyAccess);
    }

    public void SetEmergencyAccess(Entity<AirlockComponent> ent,
        bool value,
        EntityUid? user = null,
        bool predicted = false)
    {
        if (!ent.Comp.Powered)
            return;

        if (ent.Comp.EmergencyAccess == value)
            return;

        ent.Comp.EmergencyAccess = value;
        Dirty(ent, ent.Comp); // This only runs on the server apparently so we need this.
        UpdateEmergencyLightStatus(ent, ent.Comp);

        var sound = ent.Comp.EmergencyAccess ? ent.Comp.EmergencyOnSound : ent.Comp.EmergencyOffSound;
        if (predicted)
            Audio.PlayPredicted(sound, ent, user: user);
        else
            Audio.PlayPvs(sound, ent);
    }

    public void SetAutoCloseDelayModifier(AirlockComponent component, float value)
    {
        if (component.AutoCloseDelayModifier.Equals(value))
            return;

        component.AutoCloseDelayModifier = value;
    }

    public void SetSafety(AirlockComponent component, bool value)
    {
        component.Safety = value;
    }

    public bool CanChangeState(Entity<AirlockComponent> airlock, bool isPried = false)
    {
        return (isPried || airlock.Comp.Powered) && !DoorSystem.IsBolted(airlock);
    }
}
