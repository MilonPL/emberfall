﻿using Robust.Shared.GameStates;
using Content.Shared.Actions;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Implants;
/// <summary>
///     Will allow anyone implanted with the implant to have more control over their chameleon clothing and items.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChameleonControllerImplantComponent : Component
{
}

/// <summary>
///     This is sent when someone clicks on the hud icon and will open the menu.
/// </summary>
public sealed partial class ChameleonControllerOpenMenuEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum ChameleonControllerKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ChameleonControllerBuiState : BoundUserInterfaceState;


/// <summary>
///     Triggered when the user clicks on a job in the menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChameleonControllerSelectedJobMessage : BoundUserInterfaceMessage
{
    public readonly ProtoId<JobPrototype> SelectedJob;

    public ChameleonControllerSelectedJobMessage(ProtoId<JobPrototype> selectedJob)
    {
        SelectedJob = selectedJob;
    }
}
