using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Chemistry.ReactionEffects;
using Content.Server.Coordinates.Helpers;
using Content.Server.Kudzu;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Smoking;
using Content.Shared.Spawners.Components;
using Content.Shared.Spawners.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Fluids.EntitySystems;

/// <summary>
/// Handles non-atmos solution entities similar to puddles.
/// </summary>
public sealed class SmokeSystem : EntitySystem
{
    // If I could do it all again this could probably use a lot more of puddles.
    [Dependency] private readonly IAdminLogManager _logger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InternalsSystem _internals = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SmokeComponent, EntityUnpausedEvent>(OnSmokeUnpaused);
        SubscribeLocalEvent<SmokeComponent, MapInitEvent>(OnSmokeMapInit);
        SubscribeLocalEvent<SmokeComponent, ReactionAttemptEvent>(OnReactionAttempt);
        SubscribeLocalEvent<SmokeComponent, SpreadNeighborsEvent>(OnSmokeSpread);
        SubscribeLocalEvent<SmokeDissipateSpawnComponent, TimedDespawnEvent>(OnSmokeDissipate);
    }

    private void OnSmokeDissipate(EntityUid uid, SmokeDissipateSpawnComponent component, ref TimedDespawnEvent args)
    {
        if (!TryComp<TransformComponent>(uid, out var xform))
        {
            return;
        }

        Spawn(component.Prototype, xform.Coordinates);
    }

    private void OnSmokeSpread(EntityUid uid, SmokeComponent component, ref SpreadNeighborsEvent args)
    {
        if (args.Grid == null || !_solutionSystem.TryGetSolution(uid, SmokeComponent.SolutionName, out var solution))
        {
            RemCompDeferred<EdgeSpreaderComponent>(uid);
            return;
        }

        var overflow = _solutionSystem.SplitSolution(uid, solution, solution.Volume - component.OverflowThreshold);

        if (overflow.Volume <= FixedPoint2.Zero || args.NeighborFreeTiles.Count == 0)
        {
            RemCompDeferred<EdgeSpreaderComponent>(uid);
            return;
        }

        var prototype = MetaData(uid).EntityPrototype;

        if (prototype == null)
        {
            RemCompDeferred<EdgeSpreaderComponent>(uid);
            return;
        }

        args.Handled = true;
        TryComp<TimedDespawnComponent>(uid, out var timer);

        foreach (var tile in args.NeighborFreeTiles)
        {
            var split = overflow.SplitSolution(solution.Volume / args.NeighborFreeTiles.Count);
            // TODO: Spread based on prototype
            // Dissipation visuals for foam
            // TODO: Dissipation spawn for foam.
            var coords = args.Grid.GridTileToLocal(tile);
            var ent = Spawn(prototype.ID, coords.SnapToGrid());
            var neighborSmoke = EnsureComp<SmokeComponent>(ent);

            Start(ent, neighborSmoke, split, timer?.Lifetime ?? 10f);
        }
    }

    private void OnReactionAttempt(EntityUid uid, SmokeComponent component, ReactionAttemptEvent args)
    {
        if (args.Solution.Name != SmokeComponent.SolutionName)
            return;

        // Prevent smoke/foam fork bombs (smoke creating more smoke).
        foreach (var effect in args.Reaction.Effects)
        {
            if (effect is AreaReactionEffect)
            {
                args.Cancel();
                return;
            }
        }
    }

    private void OnSmokeMapInit(EntityUid uid, SmokeComponent component, MapInitEvent args)
    {
        component.NextReact = _timing.CurTime;
    }

    private void OnSmokeUnpaused(EntityUid uid, SmokeComponent component, ref EntityUnpausedEvent args)
    {
        component.NextReact += args.PausedTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SmokeComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var smoke))
        {
            if (smoke.NextReact > curTime)
                continue;

            smoke.NextReact += TimeSpan.FromSeconds(1.5);

            SmokeReact(uid, 1f, smoke);
        }
    }

    public void SmokeReact(EntityUid uid, float averageExposures, SmokeComponent? component = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref component, ref xform))
            return;

        if (!_solutionSystem.TryGetSolution(uid, SmokeComponent.SolutionName, out var solution) ||
            solution.Contents.Count == 0)
        {
            return;
        }

        if (!_mapManager.TryGetGrid(xform.GridUid, out var mapGrid))
            return;

        var tile = mapGrid.GetTileRef(xform.Coordinates.ToVector2i(EntityManager, _mapManager));

        var solutionFraction = 1 / Math.Floor(averageExposures);
        var ents = _lookup.GetEntitiesIntersecting(tile, LookupFlags.Uncontained).ToArray();

        foreach (var reagentQuantity in solution.Contents.ToArray())
        {
            if (reagentQuantity.Quantity == FixedPoint2.Zero)
                continue;

            var reagent = _prototype.Index<ReagentPrototype>(reagentQuantity.ReagentId);

            // React with the tile the effect is on
            // We don't multiply by solutionFraction here since the tile is only ever reacted once
            if (!component.ReactedTile)
            {
                reagent.ReactionTile(tile, reagentQuantity.Quantity);
                component.ReactedTile = true;
            }
        }

        foreach (var entity in ents)
        {
            ReactWithEntity(entity, solutionFraction);
        }

        UpdateVisuals(uid, component);
    }

    private void UpdateVisuals(EntityUid uid, SmokeComponent component)
    {
        if (TryComp(uid, out AppearanceComponent? appearance) &&
            _solutionSystem.TryGetSolution(uid, SmokeComponent.SolutionName, out var solution))
        {
            _appearance.SetData(uid, SmokeVisuals.Color, solution.GetColor(_prototype), appearance);
        }
    }

    private void ReactWithEntity(EntityUid entity, double solutionFraction)
    {
        if (!_solutionSystem.TryGetSolution(entity, SmokeComponent.SolutionName, out var solution))
            return;

        if (!TryComp<BloodstreamComponent>(entity, out var bloodstream))
            return;

        if (TryComp<InternalsComponent>(entity, out var internals) &&
            _internals.AreInternalsWorking(internals))
        {
            return;
        }

        var cloneSolution = solution.Clone();
        var transferAmount = FixedPoint2.Min(cloneSolution.Volume * solutionFraction, bloodstream.ChemicalSolution.AvailableVolume);
        var transferSolution = cloneSolution.SplitSolution(transferAmount);

        foreach (var reagentQuantity in transferSolution.Contents.ToArray())
        {
            if (reagentQuantity.Quantity == FixedPoint2.Zero)
                continue;

            _reactive.ReactionEntity(entity, ReactionMethod.Ingestion, reagentQuantity.ReagentId, reagentQuantity.Quantity, transferSolution);
        }

        if (_blood.TryAddToChemicals(entity, transferSolution, bloodstream))
        {
            // Log solution addition by smoke
            _logger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity):target} was affected by smoke {SolutionContainerSystem.ToPrettyString(transferSolution)}");
        }
    }

    public void Start(EntityUid uid, SmokeComponent component, Solution solution, float duration)
    {
        TryAddSolution(uid, component, solution);
        EnsureComp<EdgeSpreaderComponent>(uid);
        var timer = EnsureComp<TimedDespawnComponent>(uid);
        timer.Lifetime = duration;
    }

    public void TryAddSolution(EntityUid uid, SmokeComponent component, Solution solution)
    {
        if (solution.Volume == FixedPoint2.Zero)
            return;

        if (!_solutionSystem.TryGetSolution(uid, SmokeComponent.SolutionName, out var solutionArea))
            return;

        var addSolution =
            solution.SplitSolution(FixedPoint2.Min(solution.Volume, solutionArea.AvailableVolume));

        _solutionSystem.TryAddSolution(uid, solutionArea, addSolution);

        UpdateVisuals(uid, component);
    }
}
