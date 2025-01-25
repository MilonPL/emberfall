// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// This Source Code Form is "Incompatible With Secondary
// Licenses", as defined by the Mozilla Public License, v. 2.0.

using Content.Shared._Emberfall.Planetside.Systems;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Emberfall.Planetside.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSpaceElevatorConsoleSystem))]
[AutoGenerateComponentState]
public sealed partial class SpaceElevatorConsoleComponent : Component
{
    /// <summary>
    /// The tag used to identify preferred docking airlocks at the destination.
    /// The elevator will attempt to dock at airlocks with this tag first.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> DockTag;

    /// <summary>
    /// Defines which elevator platforms this console can control.
    /// Used to ensure consoles can only control their assigned platforms.
    /// The whitelist should match exactly one elevator platform.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist PlatformWhitelist = new();

    /// <summary>
    /// The current elevator platform this console is controlling.
    /// Found by checking nearby platforms against <see cref="PlatformWhitelist"/>.
    /// </summary>
    [DataField]
    public EntityUid? Platform;

    /// <summary>
    /// Tracks whether a valid platform connection exists on the server.
    /// This is networked separately from Platform since clients cannot
    /// reference platforms outside their PVS range.
    /// </summary>
    /// <remarks>
    /// Used by the UI to show connection status and enable/disable controls.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool HasPlatform;
}
