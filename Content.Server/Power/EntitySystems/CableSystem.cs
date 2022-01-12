using Content.Server.Electrocution;
using Content.Server.Power.Components;
using Content.Server.Tools;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Power.EntitySystems;

public class CableSystem : EntitySystem
{
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CableComponent, CuttingFinishedEvent>(OnCableCut);
        SubscribeLocalEvent<CableComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnInteractUsing(EntityUid uid, CableComponent cable, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (cable.CableDroppedOnCutPrototype == null)
            return;

        var ev = new CuttingFinishedEvent(uid, args.User);
        _toolSystem.UseTool(args.Used, args.User, uid, 0, cable.CuttingDelay, new[] { cable.CuttingQuality }, doAfterCompleteEvent: ev);
        args.Handled = true;
    }

    private void OnCableCut(EntityUid uid, CableComponent cable, CuttingFinishedEvent args)
    {
        if (_electrocutionSystem.TryDoElectrifiedAct(uid, args.User))
            return;

        Spawn(cable.CableDroppedOnCutPrototype, Transform(uid).Coordinates);
        Del(uid);
    }

    private void OnAnchorChanged(EntityUid uid, CableComponent cable, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return; // huh? it wasn't anchored?

        // This entity should not be un-anchorable. But this can happen if the grid-tile is deleted (RCD, explosion,
        // etc). In that case: behave as if the cable had been cut.
        Spawn(cable.CableDroppedOnCutPrototype, Transform(uid).Coordinates);
        Del(uid);
    }
}


// TODO: if #5887 gets merged, just use a directed event instead of broadcast-with-target
public class CuttingFinishedEvent : EntityEventArgs
{
    public EntityUid Target;
    public EntityUid User;

    public CuttingFinishedEvent(EntityUid target, EntityUid user)
    {
        Target = target;
        User = user;
    }
}
