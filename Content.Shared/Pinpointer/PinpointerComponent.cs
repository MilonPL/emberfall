using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Displays a sprite on the item that points towards the target component.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedPinpointerSystem))]
public sealed partial class PinpointerComponent : Component
{
    // TODO: Type serializer oh god
    [DataField("component")]
    public string? Component;

    [DataField("mediumDistance")]
    public float MediumDistance = 16f;

    [DataField("closeDistance")]
    public float CloseDistance = 8f;

    [DataField("reachedDistance")]
    public float ReachedDistance = 1f;

    /// <summary>
    ///     Pinpointer arrow precision in radians.
    /// </summary>
    [DataField("precision")]
    public double Precision = 0.09;

    public EntityUid? Target = null;

    [ViewVariables]
    [AutoNetworkedField]
    public bool IsActive = false;

    [ViewVariables]
    [AutoNetworkedField]
    public Angle ArrowAngle;

    [ViewVariables]
    [AutoNetworkedField]
    public Distance DistanceToTarget = Distance.Unknown;

    public bool HasTarget => DistanceToTarget != Distance.Unknown;
}

[Serializable, NetSerializable]
public enum Distance : byte
{
    Unknown,
    Reached,
    Close,
    Medium,
    Far
}
