using Content.Server.Anomaly.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using System.Linq;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.Anomaly.Effects;
/// <summary>
/// This component allows the anomaly to inject liquid from the SolutionContainer
/// into the surrounding entities with the InjectionSolution component
/// </summary>
///

/// <see cref="InjectionAnomalyComponent"/>
public sealed class InjectionAnomalySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedSolutionSystem _solutionSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private EntityQuery<InjectableSolutionComponent> _injectableQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<InjectionAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<InjectionAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical, before: new[] { typeof(SharedSolutionContainerSystem) });

        _injectableQuery = GetEntityQuery<InjectableSolutionComponent>();
    }

    private void OnPulse(Entity<InjectionAnomalyComponent> entity, ref AnomalyPulseEvent args)
    {
        PulseScalableEffect(entity, entity.Comp.InjectRadius * args.PowerModifier, entity.Comp.MaxSolutionInjection * args.Severity * args.PowerModifier);
    }

    private void OnSupercritical(Entity<InjectionAnomalyComponent> entity, ref AnomalySupercriticalEvent args)
    {
        PulseScalableEffect(entity, entity.Comp.SuperCriticalInjectRadius * args.PowerModifier, entity.Comp.SuperCriticalSolutionInjection * args.PowerModifier);
    }

    private void PulseScalableEffect(Entity<InjectionAnomalyComponent> entity, float injectRadius, float maxInject)
    {
        if (!_solutionSystem.TryGetSolution(entity.Owner, entity.Comp.Solution, out var sourceSolution))
            return;

        //We get all the entity in the radius into which the reagent will be injected.
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(entity);
        var allEnts = _lookup.GetEntitiesInRange<InjectableSolutionComponent>(_transform.GetMapCoordinates(entity, xform: xform), injectRadius)
            .Select(x => x.Owner).ToList();
        //for each matching entity found
        foreach (var ent in allEnts)
        {
            if (_injectableQuery.TryGetComponent(ent, out var injComp)
                && _solutionSystem.SolutionQuery.TryComp(ent, out var solComp))
            {
                _solutionSystem.TransferSolution(sourceSolution, (ent, solComp), maxInject,
                    out _, false);
                //Spawn Effect
                var uidXform = Transform(ent);
                Spawn(entity.Comp.VisualEffectPrototype, uidXform.Coordinates);
            }
        }
    }
}
