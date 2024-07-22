﻿using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Revenant.Components;

/// <summary>
/// This is used for tracking lights that are overloaded
/// and are about to zap a player.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RevenantOverloadedLightsComponent : Component
{
    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Accumulator = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public float ZapDelay = 3f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float ZapRange = 4f;

    [DataField]
    public EntProtoId ZapBeamEntityId = "LightningRevenant";

    public float? OriginalEnergy;
    public bool OriginalEnabled = false;
}
