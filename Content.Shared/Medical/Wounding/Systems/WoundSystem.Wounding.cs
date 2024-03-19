﻿using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Components;
using Content.Shared.Medical.Wounding.Events;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Wounding.Systems;

public sealed partial class WoundSystem
{

    [Dependency] private readonly INetManager _netManager = default!;
    private const float DefaultSeverity = 100;
    private const float SplatterDamageMult = 1.0f;
    private const float NonCoreDamageChance = 0.5f;
    private const float HeadDamageChance = 0.2f;
    private const float ChanceForPartSelection = 0.50f;

    private void InitWounding()
    {
        SubscribeLocalEvent<WoundableComponent, DamageChangedEvent>(OnWoundableDamaged);
        SubscribeLocalEvent<BodyComponent, DamageChangedEvent>(OnBodyDamaged);
        SubscribeLocalEvent<WoundComponent, ContainerGettingInsertedAttemptEvent>(OnWoundInsertAttempt);
        SubscribeLocalEvent<WoundComponent, EntGotRemovedFromContainerMessage>(OnWoundRemoved);
    }

    private void OnBodyDamaged(EntityUid bodyEnt, BodyComponent body, ref DamageChangedEvent args)
    {
        //TODO: Make this method not MEGA ASS, because jesus christ I'm giving myself terminal space aids by doing this.
        //This is all placeholder and terrible, rewrite asap

        //Do not handle damage if it is being set instead of being changed.
        //We will handle that with another listener
        if (args.DamageDelta == null)
            return;
        if (!_bodySystem.TryGetRootBodyPart(bodyEnt, out var rootPart, body))
            return;

        //TODO: This is a quick hack to prevent asphyxiation/bloodloss from damaging bodyparts
        //Once proper body/organ simulation is implemented these can be removed
        args.DamageDelta.DamageDict.Remove("Asphyxiation");
        args.DamageDelta.DamageDict.Remove("Bloodloss");
        args.DamageDelta.DamageDict.Remove("Structural");
        if (args.DamageDelta.Empty)
            return;

        DamageableComponent? damagableComp;

        if (_random.NextFloat(0f, 1f) > NonCoreDamageChance)
        {
            if (_random.NextFloat(0f, 1f) <= HeadDamageChance)
            {
                var heads = _bodySystem.GetBodyChildrenOfType(bodyEnt, BodyPartType.Head, body).ToList();
                var (headId, _) = heads[_random.Next(heads.Count)];
                if (TryComp(headId, out damagableComp))
                {
                    _damageableSystem.TryChangeDamage(headId, args.DamageDelta, damageable: damagableComp);
                    return;
                }
            }
            if (TryComp(rootPart, out damagableComp))
            {
                _damageableSystem.TryChangeDamage(rootPart.Value, args.DamageDelta, damageable: damagableComp);
                return;
            }
        }
        var children = _bodySystem.GetBodyPartDirectChildren(rootPart.Value, rootPart.Value.Comp).ToArray();
        Entity<BodyPartComponent> foundTarget = children[_random.Next(0, children.Length)];
        while (_random.NextFloat(0, 1f) > ChanceForPartSelection)
        {
            children = _bodySystem.GetBodyPartDirectChildren(foundTarget, foundTarget.Comp).ToArray();
            if (children.Length == 0)
                break;
            foundTarget = children[_random.Next(0, children.Length)];
        }
        _damageableSystem.TryChangeDamage(foundTarget, args.DamageDelta);
    }

    private void OnWoundableDamaged(EntityUid owner, WoundableComponent woundableComp, ref DamageChangedEvent args)
    {
        //Do not handle damage if it is being set instead of being changed.
        //We will handle that with another listener
        if (args.DamageDelta == null)
            return;
        CreateWoundsFromDamage(new(owner, woundableComp), args.DamageDelta);
    }

    #region Utility

    public IEnumerable<Entity<WoundComponent>> GetAllWounds(Entity<WoundableComponent> woundable)
    {
        if (!_containerSystem.TryGetContainer(woundable, WoundableComponent.WoundableContainerId, out var container))
        {
            Log.Error($"Wound container could not be found for {ToPrettyString(woundable)}! This should never happen!");
            yield break;
        }
        foreach (var entId in container.ContainedEntities)
        {
            yield return new Entity<WoundComponent>(entId, Comp<WoundComponent>(entId));
        }
    }

    #endregion


    #region WoundDestruction

    public void RemoveWound(Entity<WoundComponent> wound, Entity<WoundableComponent>? woundableParent)
    {
        if (woundableParent != null)
        {
            if (wound.Comp.ParentWoundable != woundableParent.Value.Owner)
            {
                Log.Error($"{ToPrettyString(woundableParent.Value.Owner)} does not match the parent woundable on {ToPrettyString(wound.Owner)}");
                return;
            }
        }
        else
        {
            if (!TryComp<WoundableComponent>(wound.Comp.ParentWoundable, out var woundable))
            {
                Log.Error($"{ToPrettyString(wound.Comp.ParentWoundable)} Does not have a woundable component this should never happen!");
                return;
            }

            woundableParent = new(wound.Comp.ParentWoundable, woundable);
        }

        if (!_containerSystem.TryGetContainer(woundableParent.Value, WoundableComponent.WoundableContainerId,
                out var container)
            || !_containerSystem.Remove(new(wound, null, null), container, reparent: false))
        {
            Log.Error($"Failed to remove wound from {ToPrettyString(wound.Comp.ParentWoundable)}, this should never happen!");
        }
    }
    #endregion

    #region WoundCreation

    public void CreateWoundsFromDamage(Entity<WoundableComponent> woundable, DamageSpecifier damageSpec)
    {
        foreach (var (damageTypeId, damage) in damageSpec.DamageDict)
        {
            //If damage is negative (healing) skip because wound healing is handled with internal logic.
            if (damage < 0)
                continue;
            if (!TryGetWoundProtoFromDamage(woundable, new(damageTypeId), damage,
                    out var protoId, out var overflow))
                return;
            AddWound(woundable, protoId, DefaultSeverity);
        }
    }

    private void AddWound(Entity<WoundableComponent> woundable, EntProtoId woundProtoId, FixedPoint2 severity)
    {
        var newWound = CreateWound(woundProtoId, severity);
        var attempt = new CreateWoundAttemptEvent(woundable, newWound);
        RaiseLocalEvent(woundable, ref attempt);
        if (woundable.Comp.Body != null)
            RaiseLocalEvent(woundable.Comp.Body.Value, ref attempt);
        if (attempt.Canceled)
        {
            //if we aren't adding this wound, nuke it because it's not being attached to anything.
            QueueDel(newWound);
            return;
        }
        if (!_containerSystem.TryGetContainer(woundable, WoundableComponent.WoundableContainerId, out var container)
            || !_containerSystem.Insert(new(newWound.Owner, null, null, null), container)
           )
        {
            Log.Error($"{ToPrettyString(woundable.Owner)} does not have a woundable container, or insertion is not possible! This should never happen!");
            return;
        }
    }

    /// <summary>
    /// Create a new wound in nullspace from a wound entity prototype id.
    /// </summary>
    /// <param name="woundProtoId">ProtoId of our wound</param>
    /// <param name="severity">The severity the wound will start with</param>
    /// <returns></returns>
    private Entity<WoundComponent> CreateWound(EntProtoId woundProtoId, FixedPoint2 severity)
    {
        var newEnt = Spawn(woundProtoId);
        var newWound = new Entity<WoundComponent>(newEnt, Comp<WoundComponent>(newEnt));
        //Do not raise a severity changed event because we will handle that when the wound gets attached
        SetWoundSeverity(newWound, severity, false);
        return newWound;
    }

    /// <summary>
    /// Tries to get the appropriate wound for the specified damage type and damage amount
    /// </summary>
    /// <param name="woundable">Woundable Entity/comp</param>
    /// <param name="damageType">Damage type to check</param>
    /// <param name="damage">Damage being applied</param>
    /// <param name="woundProtoId">Found WoundProtoId</param>
    /// <param name="damageOverflow">The amount of damage exceeding the max cap</param>
    /// <returns>True if a woundProto is found, false if not</returns>
    public bool TryGetWoundProtoFromDamage(Entity<WoundableComponent> woundable,ProtoId<DamageTypePrototype> damageType, FixedPoint2 damage,
        [NotNullWhen(true)] out string? woundProtoId, out FixedPoint2 damageOverflow)
    {
        damageOverflow = 0;
        woundProtoId = null;
        if (!woundable.Comp.Config.TryGetValue(damageType, out var metadata))
            return false;
        //scale the incoming damage and calculate overflows
        var adjDamage = damage * metadata.Scaling;
        if (adjDamage > metadata.DamageMax)
        {
            damageOverflow = adjDamage - metadata.DamageMax;
            adjDamage = metadata.DamageMax;
        }
        var percentageOfMax = adjDamage / metadata.DamageMax *100;
        var woundPool = _prototypeManager.Index(metadata.WoundPool);
        foreach (var (percentage, lastWoundProtoId) in woundPool.Wounds)
        {
            if (percentage >= percentageOfMax)
                break;
            woundProtoId = lastWoundProtoId;
        }
        return woundProtoId != null;
    }

    #endregion

    #region Severity


    /// <summary>
    /// Forcibly set a wound's severity, this does NOT raise a cancellable event, and should only be used internally.
    /// This exists for performance reasons to reduce the amount of events being raised.
    /// Or in situations where cancelling should not be allowed.
    /// </summary>
    /// <param name="wound">Target wound</param>
    /// <param name="severity">New Severity</param>
    /// <param name="raiseEvent">Should we raise a severity changed event</param>
    private void SetWoundSeverity(Entity<WoundComponent> wound, FixedPoint2 severity, bool raiseEvent)
    {
        var oldSev = severity;
        wound.Comp.Severity = severity;
        if (!raiseEvent)
            return;
        var ev = new WoundSeverityChangedEvent(wound, oldSev);
        RaiseLocalEvent(wound, ref ev);
        Dirty(wound);
    }

    /// <summary>
    /// Sets a wound's severity in a cancellable way. Generally avoid using this when you can and use AddWoundSeverity instead!
    /// AddWoundSeverity DOES NOT raise a cancellable event, only set does to prevent systems from overwriting each other!
    /// </summary>
    /// <param name="wound">Target wound</param>
    /// <param name="severity">New severity value</param>
    /// <returns>True if the severity was successfully set, false if not</returns>
    public bool TrySetWoundSeverity(Entity<WoundComponent> wound, FixedPoint2 severity)
    {
        var attempt = new SetWoundSeverityAttemptEvent(wound, severity);
        RaiseLocalEvent(wound, ref attempt);
        if (attempt.Cancel)
            return false;
        SetWoundSeverity(wound, severity, true);
        return true;
    }

    /// <summary>
    /// Add/Remove from a wounds severity value. Note: Severity is clamped between 0 and 100.
    /// </summary>
    /// <param name="wound">Target wound</param>
    /// <param name="severityDelta">Severity value we are adding/removing</param>
    public void ChangeWoundSeverity(Entity<WoundComponent> wound, FixedPoint2 severityDelta)
    {
        SetWoundSeverity(wound, FixedPoint2.Clamp(wound.Comp.Severity+severityDelta, 0 , 100), true);
    }

    #endregion

    #region ContainerEvents

    private void OnWoundInsertAttempt(EntityUid woundEnt, WoundComponent wound, ref ContainerGettingInsertedAttemptEvent args)
    {
        if (args.Container.ID != WoundableComponent.WoundableContainerId)
        {
            Log.Error("Tried to add wound to a container that is NOT woundableContainer");
            args.Cancel();
            return;
        }
        if (!TryComp<WoundableComponent>(args.Container.Owner, out var woundable))
        {
            Log.Error("Tried to add a wound to an entity without a woundable!");
            args.Cancel();
            return;
        }

        wound.ParentWoundable = args.Container.Owner;
        var ev = new WoundCreatedEvent(
            new (args.Container.Owner,woundable),
            new(woundEnt, wound));
        RaiseLocalEvent(args.Container.Owner, ref ev);
        RaiseLocalEvent(woundEnt, ref ev);
        if (woundable.Body != null)
        {
            var ev2 = new WoundAppliedToBody(
                new (woundable.Body.Value, Comp<BodyComponent>(woundable.Body.Value)),
                new (args.Container.Owner,woundable),
                new(woundEnt, wound));
            RaiseLocalEvent(woundable.Body.Value ,ref ev2);
            RaiseLocalEvent(woundEnt ,ref ev2);
        }
        Dirty(woundEnt, wound);
    }

    private void OnWoundRemoved(EntityUid woundEnt, WoundComponent wound, ref EntGotRemovedFromContainerMessage args)
    {
        var woundable = Comp<WoundableComponent>(args.Container.Owner);
        var ev = new WoundDestroyedEvent(
            new(args.Container.Owner, woundable),
            new (woundEnt, wound)
        );
        RaiseLocalEvent(woundEnt, ref ev);
        RaiseLocalEvent(args.Container.Owner, ref ev);
        if (woundable.Body != null)
        {
            var ev2 = new WoundRemovedFromBody(
                new (woundable.Body.Value, Comp<BodyComponent>(woundable.Body.Value)),
                new (args.Container.Owner,woundable),
                new(woundEnt, wound));
            RaiseLocalEvent(woundable.Body.Value ,ref ev2);
            RaiseLocalEvent(woundEnt ,ref ev2);
        }
        QueueDel(woundEnt); //Wounds should never exist outside of a container
    }
    #endregion
}
