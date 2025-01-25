// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// This Source Code Form is "Incompatible With Secondary
// Licenses", as defined by the Mozilla Public License, v. 2.0.

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
