// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// This Source Code Form is "Incompatible With Secondary
// Licenses", as defined by the Mozilla Public License, v. 2.0.

using Content.Server.Atmos.EntitySystems;
using Content.Server.Parallax;
using Content.Shared._Emberfall.Planetside;
using Content.Shared.Parallax.Biomes;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Emberfall.Planetside.Systems;

public sealed class PlanetsideTerrainSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    private readonly List<(Vector2i, Tile)> _reservedTiles = [];

    /// <summary>
    /// Generates a new terrain map from a template.
    /// </summary>
    /// <param name="templateId"><see cref="TerrainTemplatePrototype" /> to load</param>
    /// <param name="initMap">Whether to initialize the map immediately</param>
    /// <returns>The EntityUid of the generated map</returns>
    [PublicAPI]
    public EntityUid GenerateTerrain(ProtoId<TerrainTemplatePrototype> templateId, bool initMap = true)
    {
        var template = _prototype.Index(templateId);
        var mapUid = _map.CreateMap(out _, initMap);

        ConfigureMap(mapUid, template);
        return mapUid;
    }

    /// <summary>
    /// Generates a terrain map and loads structures onto it from a file path.
    /// </summary>
    /// <param name="templateId"><see cref="TerrainTemplatePrototype" /> to load</param>
    /// <param name="structuresPath">File path containing structure data to load</param>
    /// <returns>The EntityUid of the generated map, or null if loading failed</returns>
    [PublicAPI]
    public EntityUid? GenerateTerrainWithStructures(ProtoId<TerrainTemplatePrototype> templateId, string structuresPath)
    {
        var mapUid = GenerateTerrain(templateId, false);
        var mapId = Comp<MapComponent>(mapUid).MapId;

        if (!_mapLoader.TryLoad(mapId, structuresPath, out var grids))
        {
            Log.Error($"Failed to load structures from {structuresPath} for terrain {templateId}");
            Del(mapUid);
            return null;
        }

        ReserveStructureSpace(mapUid, grids);
        _map.InitializeMap(mapUid);
        return mapUid;
    }

    private void ConfigureMap(EntityUid mapUid, TerrainTemplatePrototype template)
    {
        _biome.EnsurePlanet(mapUid, _prototype.Index(template.Biome), mapLight: template.AmbientLight);

        var biome = Comp<BiomeComponent>(mapUid);
        foreach (var layer in template.BiomeMarkerLayers)
        {
            _biome.AddMarkerLayer(mapUid, biome, layer);
        }

        if (template.AddComponents is not null)
            EntityManager.AddComponents(mapUid, template.AddComponents);

        _atmos.SetMapAtmosphere(mapUid, false, template.Atmosphere);
        _meta.SetEntityName(mapUid, Loc.GetString(template.Name));
    }

    private void ReserveStructureSpace(EntityUid mapUid, IReadOnlyList<EntityUid> structures)
    {
        foreach (var structure in structures)
        {
            _reservedTiles.Clear();
            var bounds = Comp<MapGridComponent>(structure).LocalAABB;
            _biome.ReserveTiles(mapUid, bounds.Enlarged(0.2f), _reservedTiles);
        }
    }
}
