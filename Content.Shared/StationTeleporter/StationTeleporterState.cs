using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.StationTeleporter;

[Serializable, NetSerializable]
public enum StationTeleporterUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class StationTeleporterState : BoundUserInterfaceState
{
    public NetEntity? SelectedTeleporter;
    public List<StationTeleporterStatus> Teleporters;
    public StationTeleporterState(List<StationTeleporterStatus> teleporters, NetEntity? selected = null)
    {
        Teleporters = teleporters;
        SelectedTeleporter = selected;
    }
}

[Serializable, NetSerializable]
public sealed class StationTeleporterStatus
{
    public StationTeleporterStatus(NetEntity teleporterUid, NetCoordinates coordinates, NetEntity teleporterLinkedUid, NetCoordinates? link, string name, bool powered)
    {
        TeleporterUid = teleporterUid;
        Coordinates = coordinates;
        TeleporterLinkedUid = teleporterLinkedUid;
        LinkCoordinates = link;
        Name = name;
        Powered = powered;
    }

    public NetEntity TeleporterUid;
    public NetEntity TeleporterLinkedUid;
    public NetCoordinates? Coordinates;
    public NetCoordinates? LinkCoordinates;
    public string Name;
    public bool Powered;
}

[Serializable, NetSerializable]
public sealed class StationTeleporterClickMessage : BoundUserInterfaceMessage
{
    public NetEntity? Teleporter;

    /// <summary>
    /// Called when the client clicks on any active Teleporter on the StationTeleporterConsoleComponent
    /// </summary>
    public StationTeleporterClickMessage(NetEntity? teleporter)
    {
        Teleporter = teleporter;
    }
}

[Serializable, NetSerializable]
public enum TeleporterPortalVisual
{
    Color,
}
