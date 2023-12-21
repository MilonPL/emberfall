using Content.Shared.Maps;
using Content.Shared.RCD.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.RCD.Components;

/// <summary>
/// Main component for the RCD
/// Optionally uses LimitedChargesComponent.
/// Charges can be refilled with RCD ammo
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RCDSystem))]
public sealed partial class RCDComponent : Component
{
    /// <summary>
    /// Time taken to do an action like placing a wall
    /// </summary>
    [DataField("delay"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Delay = 2f;

    [DataField("swapModeSound")]
    public SoundSpecifier SwapModeSound = new SoundPathSpecifier("/Audio/Items/genhit.ogg");

    [DataField("successSound")]
    public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

    /// <summary>
    /// What mode are we on? Can be floors, walls, airlock, deconstruct.
    /// </summary>
    [DataField("mode"), AutoNetworkedField]
    public RcdMode Mode = RcdMode.Invalid;

    /// <summary>
    /// Prototype to be constructed
    /// </summary>
    [DataField("constructionPrototype"), AutoNetworkedField]
    public string? ConstructionPrototype;

    /// <summary>
    /// List of RCD prototypes that the device comes loaded with
    /// </summary>
    [DataField("availablePrototypes"), AutoNetworkedField]
    public List<ProtoId<RCDPrototype>> AvailablePrototypes = new();
}

public enum RcdMode : byte
{
    Invalid,
    Deconstruct,
    Floors,
    Catwalks,
    Walls,
    Airlocks,
    Windows,
    DirectionalWindows,
    Machines,
    Computers,
    Lighting,
}

[Serializable, NetSerializable]
public sealed class RCDSystemMessage : BoundUserInterfaceMessage
{
    public RcdMode RcdMode;
    public string? ConstructionPrototype;

    public RCDSystemMessage(RcdMode rcdMode, string? constructionPrototype)
    {
        RcdMode = rcdMode;
        ConstructionPrototype = constructionPrototype;
    }
}

[Serializable, NetSerializable]
public enum RcdUiKey : byte
{
    Key
}
