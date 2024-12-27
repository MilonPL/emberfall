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
