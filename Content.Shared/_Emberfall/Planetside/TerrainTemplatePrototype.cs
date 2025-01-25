// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// This Source Code Form is "Incompatible With Secondary
// Licenses", as defined by the Mozilla Public License, v. 2.0.

using Content.Shared.Atmos;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Markers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Emberfall.Planetside;

/// <summary>
/// Used for roundstart planetside surface.
/// </summary>
[Prototype]
public sealed partial class TerrainTemplatePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// The localized map name.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// The biome to create the map with.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> Biome;

    /// <summary>
    /// Components added when the map is spawned.
    /// </summary>
    [DataField]
    public ComponentRegistry? AddComponents;

    /// <summary>
    /// Ambient map lighting color.
    /// </summary>
    // Daylight: #D8B059
    // Midday: #E6CB8B
    // Evening: #4F3C20
    // Moonlight: #2B3143
    // Lava: #A34931
    [DataField]
    public Color AmbientLight = Color.FromHex("#4F3C20");

    /// <summary>
    /// Atmosphere gas mixture.
    /// </summary>
    [DataField(required: true)]
    public GasMixture Atmosphere = new();

    /// <summary>
    /// Biome layers added when the map is spawned.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<BiomeMarkerLayerPrototype>> BiomeMarkerLayers = new();
}
