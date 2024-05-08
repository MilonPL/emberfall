using Robust.Shared.GameStates;

namespace Content.Shared.Renamer.Components;

/// <summary>
/// Adds a modifier to the wearer's name when this item is equipped,
/// and removes it when it is unequipped.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ModifyWearerNameComponent : Component
{
    /// <summary>
    /// The text to be used as the modifier.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Text;

    /// <summary>
    /// What form of modifier should be used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NameModifierType ModifierType = NameModifierType.Prefix;

    /// <summary>
    /// Priority of the modifier. See <see cref="EntitySystems.RefreshNameModifiersEvent"/> for more information.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Priority;
}

public enum NameModifierType
{
    Prefix,
    Postfix,
    Override
}
