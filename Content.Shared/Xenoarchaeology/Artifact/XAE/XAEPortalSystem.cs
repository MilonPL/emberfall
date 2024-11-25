using Content.Shared.Mind.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public sealed class XAEPortalSystem : BaseXAESystem<XAEPortalComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEPortalComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var map = Transform(ent).MapID;
        var validMinds = new ValueList<EntityUid>();
        var mindQuery = EntityQueryEnumerator<MindContainerComponent, TransformComponent, MetaDataComponent>();
        while (mindQuery.MoveNext(out var uid, out var mc, out var xform, out var meta))
        {
            // check if the MindContainer has a Mind and if the entity is not in a container (this also auto excludes AI) and if they are on the same map
            if (mc.HasMind && !_container.IsEntityOrParentInContainer(uid, meta: meta, xform: xform) && xform.MapID == map)
            {
                validMinds.Add(uid);
            }
        }
        // this would only be 0 if there were a station full of AIs and no one else, in that case just stop this function
        if (validMinds.Count == 0)
            return;

        var offset = _random.NextVector2(2, 3);
        var originWithOffset = args.Coordinates.Offset(offset);
        var firstPortal = Spawn(ent.Comp.PortalProto, originWithOffset);

        var target = _random.Pick(validMinds);

        var secondPortal = Spawn(ent.Comp.PortalProto, _transform.GetMapCoordinates(target));

        // Manual position swapping, because the portal that opens doesn't trigger a collision, and doesn't teleport targets the first time.
        _transform.SwapPositions(target, ent.Owner);

        _link.TryLink(firstPortal, secondPortal, true);
    }
}
