using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;

namespace Content.Shared.Physics;

/// <summary>
///     Defined collision groups for the physics system.
///     Mask is what it collides with when moving. Layer is what CollisionGroup it is part of.
/// </summary>
[Flags, PublicAPI]
[FlagsFor(typeof(CollisionLayer)), FlagsFor(typeof(CollisionMask))]
public enum CollisionGroup
{
    None               = 0,
    XrayImpassable     = 1 << 0, // 1 Ignores walls, can only hit mobs
    Opaque             = 2 << 1, // 2 Blocks light, can be hit by lasers
    Impassable         = 2 << 2, // 4 Walls, objects impassable by any means
    MidImpassable      = 2 << 3, // 8 Mobs, players, crabs, etc
    HighImpassable     = 2 << 4, // 16 Things on top of tables and things that block tall/large mobs.
    LowImpassable      = 2 << 5, // 32 For things that can fit under a table or squeeze under an airlock
    GhostImpassable    = 2 << 6, // 64 Things impassible by ghosts/observers, ie blessed tiles or forcefields
    BulletImpassable   = 2 << 7, // 128 Can be hit by bullets
    InteractImpassable = 2 << 8, // 256 Blocks interaction/InRangeUnobstructed

    MapGrid = MapGridHelpers.CollisionGroup, // Map grids, like shuttles. This is the actual grid itself, not the walls or other entities connected to the grid.

    // 32 possible groups
    AllMask = -1,

    // Humanoids, etc.
    MobMask = Impassable | HighImpassable | MidImpassable | LowImpassable,
    MobLayer = Opaque | BulletImpassable | XrayImpassable,
    // Mice, drones
    SmallMobMask = Impassable | LowImpassable,
    SmallMobLayer = Opaque | BulletImpassable | XrayImpassable,
    // Birds/other small flyers
    FlyingMobMask = Impassable | HighImpassable,
    FlyingMobLayer = Opaque | BulletImpassable | XrayImpassable,

    // Mechs
    LargeMobMask = Impassable | HighImpassable | MidImpassable | LowImpassable,
    LargeMobLayer = Opaque | HighImpassable | MidImpassable | LowImpassable | BulletImpassable,

    // Machines, computers
    MachineMask = Impassable | MidImpassable | LowImpassable,
    MachineLayer = Opaque | MidImpassable | LowImpassable | BulletImpassable,

    // Tables that SmallMobs can go under
    TableMask = Impassable | MidImpassable,
    TableLayer = MidImpassable,

    // Tabletop machines, windoors, firelocks
    TabletopMachineMask = Impassable | HighImpassable,
    // Tabletop machines
    TabletopMachineLayer = Opaque | HighImpassable | BulletImpassable,

    // Airlocks, windoors, firelocks
    GlassAirlockLayer = HighImpassable | MidImpassable | BulletImpassable | InteractImpassable,
    AirlockLayer = Opaque | GlassAirlockLayer,

    // Airlock assembly
    HumanoidBlockLayer = HighImpassable | MidImpassable,

    // Soap, spills
    SlipLayer = MidImpassable | LowImpassable,
    ItemMask = Impassable | HighImpassable,
    ThrownItem = Impassable | HighImpassable | BulletImpassable,
    WallLayer = Opaque | Impassable | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | InteractImpassable,
    GlassLayer = Impassable | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | InteractImpassable,
    HalfWallLayer = MidImpassable | LowImpassable,

    // Statue, monument, airlock, window
    FullTileMask = Impassable | HighImpassable | MidImpassable | LowImpassable | InteractImpassable,
    // FlyingMob can go past
    FullTileLayer = Opaque | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | InteractImpassable,

    SubfloorMask = Impassable | LowImpassable
}
