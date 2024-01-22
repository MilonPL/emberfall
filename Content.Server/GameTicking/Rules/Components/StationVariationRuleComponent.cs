﻿using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class StationVariationRuleComponent : Component
{
    /// <summary>
    ///     The list of rules that will be started once the map is spawned.
    ///     Uses <see cref="EntitySpawnEntry"/> to support probabilities for various rules
    ///     without having to hardcode the probability directly in the rule's logic.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> Rules = new();
}
