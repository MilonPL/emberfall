// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// This Source Code Form is "Incompatible With Secondary
// Licenses", as defined by the Mozilla Public License, v. 2.0.

using Content.Server._Emberfall.Planetside.Systems;
using Content.Shared._Emberfall.Planetside;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Emberfall.Planetside.Components;

/// <summary>
/// This is used to load a map and then spawn a grid on it.
/// </summary>
[RegisterComponent, Access(typeof(PlanetsideTerrainSpawnerSystem))]
public sealed partial class PlanetsideTerrainSpawnerComponent : Component
{
    /// <summary>
    /// The terrain template to use.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TerrainTemplatePrototype> Terrain;

    /// <summary>
    /// The grid to load when spawning the map.
    /// </summary>
    [DataField]
    public ResPath? GridPath;

    /// <summary>
    /// The loaded map entity.
    /// </summary>
    [DataField]
    public EntityUid? Map;
}
