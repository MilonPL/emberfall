﻿using Content.Server.StationEvents.Events;
using Content.Server.AlertLevel;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(CommunicationInterceptionRule))]
public sealed partial class CommunicationInterceptionRuleComponent : Component
{
    /// <summary>
    /// Alert level to set the station to when the event starts.
    /// </summary>
    [DataField]
    public ProtoId<AlertLevelPrototype> AlertLevel = "yellow";
}
