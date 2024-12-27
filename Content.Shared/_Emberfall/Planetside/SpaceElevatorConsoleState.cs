using Content.Shared._Emberfall.Planetside.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Timing;
using Robust.Shared.Serialization;

namespace Content.Shared._Emberfall.Planetside;

[Serializable, NetSerializable]
public enum SpaceElevatorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SpaceElevatorConsoleState(FTLState ftlState, StartEndTime ftlTime, List<ElevatorDestination> destinations) : BoundUserInterfaceState
{
    public FTLState FTLState = ftlState;
    public StartEndTime FTLTime = ftlTime;
    public List<ElevatorDestination> Destinations = destinations;
}

[Serializable, NetSerializable]
public sealed class DockingConsoleFTLMessage(int index) : BoundUserInterfaceMessage
{
    public int Index = index;
}
