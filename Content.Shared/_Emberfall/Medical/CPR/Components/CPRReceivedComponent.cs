using Content.Shared._Emberfall.Medical.CPR.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Emberfall.Medical.CPR.Components;

// ReSharper disable InconsistentNaming
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCPRSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CPRReceivedComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan Last;
}
