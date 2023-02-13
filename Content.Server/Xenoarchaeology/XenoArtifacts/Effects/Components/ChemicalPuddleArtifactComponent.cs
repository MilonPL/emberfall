﻿using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// This is used for an artifact that creates a puddle of
/// random chemicals upon being triggered.
/// </summary>
[RegisterComponent]
public sealed class ChemicalPuddleArtifactComponent : Component
{
    /// <summary>
    /// The prototype id of the puddle
    /// </summary>
    [DataField("puddlePrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string PuddlePrototype = "PuddleSmear";

    /// <summary>
    /// The solution where all the chemicals are stored
    /// </summary>
    [DataField("chemicalSolution", required: true), ViewVariables(VVAccess.ReadWrite)]
    public Solution ChemicalSolution = default!;

    /// <summary>
    /// The different chemicals that can be spawned by this effect
    /// </summary>
    [DataField("possibleChemicals", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<ReagentPrototype>))]
    public List<string> PossibleChemicals = default!;

    /// <summary>
    /// The number of different chemicals that will be mixed.
    /// </summary>
    [DataField("chemAmount")]
    public int ChemAmount = 3;
}
