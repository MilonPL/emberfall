using Robust.Shared.GameStates;

namespace Content.Shared.IdentityManagement.Components;
/// <summary>
/// Makes it so when starting gear loads up, the name on a PDA/Id (if present) is changed to the character's name.
/// </summary>

[RegisterComponent, NetworkedComponent]
public sealed partial class IdBindComponent : Component;

