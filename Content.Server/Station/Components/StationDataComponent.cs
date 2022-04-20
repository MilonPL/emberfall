﻿using Content.Server.Maps;
using Content.Server.Station.Systems;
using Robust.Shared.Map;

namespace Content.Server.Station.Components;

/// <summary>
/// Stores core information about a station, namely it's config and associated grids.
/// All station entities will have this component.
/// </summary>
[RegisterComponent, Friend(typeof(StationSystem))]
public sealed class StationDataComponent : Component
{
    /// <summary>
    /// The game map prototype, if any, associated with this station.
    /// </summary>
    [DataField("stationConfig")]
    public StationConfig? StationConfig = null;

    /// <summary>
    /// List of all grids this station is part of.
    /// You shouldn't mutate this.
    /// </summary>
    [DataField("grids")] // Can this even be serialized or does it need to be a list of entities
    public readonly HashSet<GridId> Grids = new();
}
