﻿using Content.Shared.Physics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Lightning.Components;
public abstract class SharedLightningComponent : Component
{
    /// <summary>
    /// Can this lightning arc?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("canArc")]
    public bool CanArc;

    /// <summary>
    /// How much should lightning arc in total?
    /// Controls the amount of bolts that will spawn.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxTotalArc")]
    public int MaxTotalArcs = 50;

    /// <summary>
    /// The prototype ID used for arcing bolts. Usually will be the same name as the main proto but it could be flexible.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lightningPrototype")]
    public string LightningPrototype = "Lightning";

    /// <summary>
    /// The target that the lightning will Arc to.
    /// </summary>
    [ViewVariables]
    [DataField("arcTarget")]
    public EntityUid ArcTarget;

    /// <summary>
    /// How far should this lightning go?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxLength")]
    public float MaxLength = 5f;

    /// <summary>
    /// List of targets that this collided with already
    /// </summary>
    [ViewVariables]
    [DataField("arcTargets")]
    public HashSet<EntityUid> ArcTargets = new();

    /// <summary>
    /// What should this arc to?
    /// </summary>
    [ViewVariables]
    [DataField("collisionMask")]
    public int CollisionMask = (int) (CollisionGroup.MobMask | CollisionGroup.MachineMask);
}
