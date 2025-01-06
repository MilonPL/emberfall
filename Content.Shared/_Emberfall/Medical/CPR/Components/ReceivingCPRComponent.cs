using Content.Shared._Emberfall.Medical.CPR.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Emberfall.Medical.CPR.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCPRSystem))]
public sealed partial class ReceivingCPRComponent : Component;
