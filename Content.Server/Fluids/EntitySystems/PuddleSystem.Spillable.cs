using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory.Events;
using Content.Shared.Throwing;
using Content.Shared.Verbs;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class PuddleSystem
{
    private void InitializeSpillable()
    {
        SubscribeLocalEvent<SpillableComponent, LandEvent>(SpillOnLand);
        SubscribeLocalEvent<SpillableComponent, GetVerbsEvent<Verb>>(AddSpillVerb);
        SubscribeLocalEvent<SpillableComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SpillableComponent, SolutionSpikeOverflowEvent>(OnSpikeOverflow);
        SubscribeLocalEvent<SpillableComponent, DoAfterEvent>(OnDoAfter);
    }

    private void OnSpikeOverflow(EntityUid uid, SpillableComponent component, SolutionSpikeOverflowEvent args)
    {
        if (!args.Handled)
        {
            TrySpillAt(args.Overflow, Transform(uid).Coordinates);
        }

        args.Handled = true;
    }

    private void OnGotEquipped(EntityUid uid, SpillableComponent component, GotEquippedEvent args)
    {
        if (!component.SpillWorn)
            return;

        if (!TryComp(uid, out ClothingComponent? clothing))
            return;

        // check if entity was actually used as clothing
        // not just taken in pockets or something
        var isCorrectSlot = clothing.Slots.HasFlag(args.SlotFlags);
        if (!isCorrectSlot)
            return;

        if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution))
            return;

        if (solution.Volume == 0)
            return;

        // spill all solution on the player
        var drainedSolution = _solutionContainerSystem.Drain(uid, solution, solution.Volume);
        TrySpillAt(args.Equipee, drainedSolution);
    }

    private void SpillOnLand(EntityUid uid, SpillableComponent component, ref LandEvent args)
    {
        if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution))
            return;

        if (TryComp<DrinkComponent>(uid, out var drink) && !drink.Opened)
            return;

        if (args.User != null)
        {
            _adminLogger.Add(LogType.Landed,
                $"{ToPrettyString(uid):entity} spilled a solution {SolutionContainerSystem.ToPrettyString(solution):solution} on landing");
        }

        var drainedSolution = _solutionContainerSystem.Drain(uid, solution, solution.Volume);
        TrySpillAt(drainedSolution, EntityManager.GetComponent<TransformComponent>(uid).Coordinates);
    }

    private void AddSpillVerb(EntityUid uid, SpillableComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_solutionContainerSystem.TryGetDrainableSolution(args.Target, out var solution))
            return;

        if (TryComp<DrinkComponent>(args.Target, out var drink) && (!drink.Opened))
            return;

        if (solution.Volume == FixedPoint2.Zero)
            return;

        Verb verb = new()
        {
            Text = Loc.GetString("spill-target-verb-get-data-text")
        };

        // TODO VERB ICONS spill icon? pouring out a glass/beaker?
        if (component.SpillDelay == null)
        {
            verb.Act = () =>
            {
                var puddleSolution = _solutionContainerSystem.SplitSolution(args.Target,
                    solution, solution.Volume);
                TrySpillAt(puddleSolution, Transform(args.Target).Coordinates);
            };
        }
        else
        {
            verb.Act = () =>
            {
                _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, component.SpillDelay.Value, target:uid)
                {
                    BreakOnTargetMove = true,
                    BreakOnUserMove = true,
                    BreakOnDamage = true,
                    BreakOnStun = true,
                    NeedHand = true
                });
            };
        }
        verb.Impact = LogImpact.Medium; // dangerous reagent reaction are logged separately.
        verb.DoContactInteraction = true;
        args.Verbs.Add(verb);
    }

    private void OnDoAfter(EntityUid uid, SpillableComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        //solution gone by other means before doafter completes
        if (!_solutionContainerSystem.TryGetDrainableSolution(uid, out var solution) || solution.Volume == 0)
            return;

        var puddleSolution = _solutionContainerSystem.SplitSolution(uid, solution, solution.Volume);
        TrySpillAt(puddleSolution, Transform(uid).Coordinates);
        args.Handled = true;
    }
}
