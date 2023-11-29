using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    /// <summary>
    ///     Internal Atmos class that stores data about the atmosphere in a grid.
    ///     You shouldn't use this directly, use <see cref="AtmosphereSystem"/> instead.
    /// </summary>
    [Access(typeof(AtmosphereSystem), typeof(GasTileOverlaySystem), typeof(AtmosDebugOverlaySystem))]
    public sealed class TileAtmosphere : IGasMixtureHolder
    {
        [ViewVariables]
        public int ArchivedCycle;

        [ViewVariables]
        public int CurrentCycle;

        [ViewVariables]
        public float Temperature { get; set; } = Atmospherics.T20C;

        [ViewVariables]
        public float TemperatureArchived { get; set; } = Atmospherics.T20C;

        [ViewVariables]
        public TileAtmosphere? PressureSpecificTarget { get; set; }

        [ViewVariables]
        public float PressureDifference { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float HeatCapacity { get; set; } = Atmospherics.MinimumHeatCapacity;

        [ViewVariables]
        public float ThermalConductivity { get; set; } = 0.05f;

        [ViewVariables]
        public bool Excited { get; set; }

        /// <summary>
        ///     Whether this tile should be considered space.
        /// </summary>
        [ViewVariables]
        public bool Space { get; set; }

        /// <summary>
        ///     Adjacent tiles in the same order as <see cref="AtmosDirection"/>. (NSEW)
        /// </summary>
        [ViewVariables]
        public readonly TileAtmosphere?[] AdjacentTiles = new TileAtmosphere[Atmospherics.Directions];

        /// <summary>
        /// Neighbouring tiles to which air can flow. This is a combination of this tile's unblocked direction, and the
        /// unblocked directions on adjacent tiles.
        /// </summary>
        [ViewVariables]
        public AtmosDirection AdjacentBits = AtmosDirection.Invalid;

        [ViewVariables, Access(typeof(AtmosphereSystem), Other = AccessPermissions.ReadExecute)]
        public MonstermosInfo MonstermosInfo;

        [ViewVariables]
        public Hotspot Hotspot;

        [ViewVariables]
        public AtmosDirection PressureDirection;

        // For debug purposes.
        [ViewVariables]
        public AtmosDirection LastPressureDirection;

        [ViewVariables]
        [Access(typeof(AtmosphereSystem))]
        public EntityUid GridIndex { get; set; }

        [ViewVariables]
        public TileRef? Tile => GridIndices.GetTileRef(GridIndex);

        [ViewVariables]
        public Vector2i GridIndices;

        [ViewVariables]
        public ExcitedGroup? ExcitedGroup { get; set; }

        /// <summary>
        /// The air in this tile. If null, this tile is completely air-blocked.
        /// This can be immutable if the tile is spaced.
        /// </summary>
        [ViewVariables]
        [Access(typeof(AtmosphereSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public GasMixture? Air { get; set; }

        [DataField("lastShare")]
        public float LastShare;

        [ViewVariables]
        public readonly float[] MolesArchived = new float[Atmospherics.AdjustedNumberOfGases];

        GasMixture IGasMixtureHolder.Air
        {
            get => Air ?? new GasMixture(Atmospherics.CellVolume){ Temperature = Temperature };
            set => Air = value;
        }

        [ViewVariables]
        public float MaxFireTemperatureSustained { get; set; }

        /// <summary>
        /// If true, this tile does not actually exist on the grid, it only exists to represent the map's atmosphere for\
        /// adjacent grid tiles.
        /// </summary>
        public bool MapAtmos { get; set; }

        /// <summary>
        /// If true, the cached airtight data is invalid and needs to be recomputed. See <see cref="AirtightData"/>.
        /// </summary>
        public bool AirtightDirty = true;

        /// <summary>
        /// Cached information about airtight entities on this tile. This is only up to date if
        /// <see cref="AirtightDirty"/> is false and the tile is not queued for processing (i.e., not in
        /// <see cref="GridAtmosphereComponent.InvalidatedCoords"/>).
        /// </summary>
        public AtmosphereSystem.AirtightData AirtightData;

        public TileAtmosphere(EntityUid gridIndex, Vector2i gridIndices, GasMixture? mixture = null, bool immutable = false, bool space = false)
        {
            GridIndex = gridIndex;
            GridIndices = gridIndices;
            Air = mixture;
            Space = space;

            if(immutable)
                Air?.MarkImmutable();
        }

        public TileAtmosphere(TileAtmosphere other)
        {
            GridIndex = other.GridIndex;
            GridIndices = other.GridIndices;
            Space = other.Space;
            Air = other.Air?.Clone();
            Array.Copy(other.MolesArchived, MolesArchived, MolesArchived.Length);
        }

        public TileAtmosphere()
        {
        }
    }
}
