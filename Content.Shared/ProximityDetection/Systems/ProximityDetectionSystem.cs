﻿using Content.Shared.ProximityDetection.Components;
using Robust.Shared.Network;

namespace Content.Shared.ProximityDetection.Systems;


//This handles generic proximity detector logic
public sealed class ProximityDetectionSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;
    private const float HighTickWarningRange = 15;
    private const float LowTickWarningRange = 50;
    private const float HighImpactTickRateThreshold = 0.5f;

    public override void Initialize()
    {
        SubscribeLocalEvent<ProximityDetectorComponent, EntityPausedEvent>(OnPaused);
        SubscribeLocalEvent<ProximityDetectorComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<ProximityDetectorComponent, ComponentStartup>(OnCompStartup);
    }

    private void OnPaused(EntityUid owner, ProximityDetectorComponent component, EntityPausedEvent args)
    {
        SetEnable_Internal(owner,component,false);
    }

    private void OnUnpaused(EntityUid owner, ProximityDetectorComponent detector, ref EntityUnpausedEvent args)
    {
        SetEnable_Internal(owner, detector,true);
    }
    public void SetEnable(EntityUid owner, bool enabled, ProximityDetectorComponent? detector = null)
    {
        if (!Resolve(owner, ref detector) || detector.Enabled == enabled)
            return;
        SetEnable_Internal(owner ,detector, enabled);
    }

    public bool GetEnable(EntityUid owner, ProximityDetectorComponent? detector = null)
    {
        return Resolve(owner, ref detector, false) && detector.Enabled;
    }

    private void SetEnable_Internal(EntityUid owner,ProximityDetectorComponent detector, bool enabled)
    {
        detector.Enabled = enabled;
        var noDetectEvent = new ProximityTargetUpdatedEvent(detector, detector.TargetEnt, detector.Distance);
        RaiseLocalEvent(owner, ref noDetectEvent);
        if (!enabled)
        {
            detector.AccumulatedFrameTime = 0;
            RunUpdate_Internal(owner, detector);
            Dirty(owner, detector);
            return;
        }
        RunUpdate_Internal(owner, detector);
    }

    public void ForceUpdate(EntityUid owner, ProximityDetectorComponent? detector = null)
    {
        if (!Resolve(owner, ref detector))
            return;
        RunUpdate_Internal(owner, detector);
    }


    private void RunUpdate_Internal(EntityUid owner,ProximityDetectorComponent detector)
    {
        if (!_net.IsServer) //only run detection checks on the server! TODO: Move to server
            return;
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(owner);
        List<(EntityUid TargetEnt, float Distance)> detections = new();
        foreach (var ent in _entityLookup.GetEntitiesInRange(_transform.GetMapCoordinates(owner, xform),
                     detector.Range.Float()))
        {
            if (!detector.Criteria.IsValid(ent, EntityManager))
                continue;
            var distance = (_transform.GetWorldPosition(xform, xformQuery) - _transform.GetWorldPosition(ent, xformQuery)).Length();
            if (CheckDetectConditions(ent, distance, owner, detector))
            {
                detections.Add((ent, distance));
            }
        }
        UpdateTargetFromClosest(owner, detector, detections);
    }

    private bool CheckDetectConditions(EntityUid targetEntity, float dist, EntityUid owner, ProximityDetectorComponent detector)
    {
        var detectAttempt = new ProximityDetectionAttemptEvent(false, dist, (owner, detector));
        RaiseLocalEvent(targetEntity, ref detectAttempt);
        return !detectAttempt.Cancel;
    }

    private void UpdateTargetFromClosest(EntityUid owner, ProximityDetectorComponent detector, List<(EntityUid TargetEnt, float Distance)> detections)
    {
        if (detections.Count == 0)
        {
            if (detector.TargetEnt == null)
                return;
            detector.Distance = -1;
            detector.TargetEnt = null;
            var noDetectEvent = new ProximityTargetUpdatedEvent(detector, null, -1);
            RaiseLocalEvent(owner, ref noDetectEvent);
            var newTargetEvent = new NewProximityTargetEvent(detector, null);
            RaiseLocalEvent(owner, ref newTargetEvent);
            Dirty(owner, detector);
            return;
        }
        var closestDistance = detections[0].Distance;
        EntityUid closestEnt = default!;
        foreach (var (ent,dist) in detections)
        {
            if (dist >= closestDistance)
                continue;
            closestEnt = ent;
            closestDistance = dist;
        }

        var newTarget = detector.TargetEnt != closestEnt;
        var newData = newTarget || detector.Distance != closestDistance;
        detector.TargetEnt = closestEnt;
        detector.Distance = closestDistance;
        if (newTarget)
        {
            var newTargetEvent = new NewProximityTargetEvent(detector, closestEnt);
            RaiseLocalEvent(owner, ref newTargetEvent);
        }

        if (!newData)
            return;
        var targetUpdatedEvent = new ProximityTargetUpdatedEvent(detector, closestEnt, closestDistance);
        RaiseLocalEvent(owner, ref targetUpdatedEvent);
        Dirty(owner, detector);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ProximityDetectorComponent>();
        while (query.MoveNext(out var owner, out var detector))
        {
            if (!detector.Enabled)
                continue;
            detector.AccumulatedFrameTime += frameTime;
            if (detector.AccumulatedFrameTime < detector.UpdateRate)
                continue;
            detector.AccumulatedFrameTime -= detector.UpdateRate;
            RunUpdate_Internal(owner, detector);
        }
    }

    public void SetRange(EntityUid owner, float newRange, ProximityDetectorComponent? detector = null)
    {
        if (!Resolve(owner, ref detector))
            return;
        detector.Range = newRange;
        CheckPerf(owner, detector);
        Dirty(owner, detector);
    }

    private void OnCompStartup(EntityUid owner, ProximityDetectorComponent detector, ComponentStartup args)
    {
        CheckPerf(owner, detector);
    }

    //TODO: move this to a test!
    private void CheckPerf(EntityUid owner,ProximityDetectorComponent detector)
    {
        if (detector.Range < LowTickWarningRange)
            return;
        var range = LowTickWarningRange;
        var extra = "";
        if (detector.UpdateRate <= HighImpactTickRateThreshold)
        {
            range = HighTickWarningRange;
            extra = "or decrease update frequency";
        }
        Log.Error($"{ToPrettyString(owner)} : " +
                  $"\n ProximityDetector Range: {detector.Range} is excessive! Recommend using under {range} {extra}!");
    }
}
