using Content.Shared._Emberfall.Planetside.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Emberfall.Planetside.Components;

/// <summary>
/// Component that marks an entity as a space elevator platform and stores its valid destinations
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSpaceElevatorSystem))]
public sealed partial class SpaceElevatorComponent : Component
{
    /// <summary>
    /// The station this elevator platform is assigned to.
    /// </summary>
    [DataField]
    public EntityUid? Station;

    /// <summary>
    /// List of valid destinations this elevator can travel to.
    /// </summary>
    [DataField]
    public List<ElevatorDestination> Destinations = new();
}

/// <summary>
/// Represents a valid destination point for a space elevator.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct ElevatorDestination
{
    /// <summary>
    /// LocId name of the destination shown in the UI.
    /// </summary>
    [DataField]
    public LocId Name;

    /// <summary>
    /// The target map ID for this destination.
    /// </summary>
    [DataField]
    public MapId Map;
}
