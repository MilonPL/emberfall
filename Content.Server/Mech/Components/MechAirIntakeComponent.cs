﻿using Content.Shared.Atmos;

namespace Content.Server.Mech.Components;

/// <summary>
/// This is basically a siphon vent for mech but not using pump vent component because MechAir bad
/// </summary>
[RegisterComponent]
public sealed partial class MechAirIntakeComponent : Component
{
    /// <summary>
    /// Target pressure change for a single atmos tick
    /// </summary>
    [DataField("targetPressureChange"), ViewVariables(VVAccess.ReadWrite)]
    public float TargetPressureChange = 5f;

    /// <summary>
    /// How strong the intake pump is, it will be able to replenish air from lower pressure areas.
    /// </summary>
    [DataField("pumpPower"), ViewVariables(VVAccess.ReadWrite)]
    public float PumpPower = 2f;

    /// <summary>
    /// Pressure to intake gases up to, maintains MechAir pressure.
    /// </summary>
    [DataField("pressure"), ViewVariables(VVAccess.ReadWrite)]
    public float Pressure = Atmospherics.OneAtmosphere;
}
