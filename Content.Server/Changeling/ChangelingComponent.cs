using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Utility;

namespace Content.Server.Changeling;

/// <summary>
/// Adds sting ability to the user.
/// Stinging can store up to 7 disguises.
/// </summary>
[RegisterComponent]
[Access(typeof(ChangelingSystem))]
public sealed partial class ChangelingComponent : Component
{
    /// <summary>
    /// ID of the currently active sting.
    /// Buy a sting ability and use its action to select it.
    /// </summary>
    public string ActiveSting = "ExtractionSting";

    /// <summary>
    /// Lets you refund all abilities for their evolution points.
    /// Evolution points are a unique currency stored on the player's StoreComponent.
    /// Can only reset after absorbing someone, does not stack with multiple absorbings.
    /// </summary>
    [DataField("canResetAbilities"), ViewVariables(VVAccess.ReadWrite)]
    public bool CanResetAbilities;

    /// <summary>
    /// Actions for innate abilities that cannot be removed.
    /// </summary>
    [DataField("innateAbilities")]
    public IReadOnlyList<ActionType> InnateAbilities = new[]
    {
        //"ChangelingStore",
        //"ChangelingStasis",
        //"ChangelingRegenerate",
        new InstantAction()
        {
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Interface/Actions/changeling.rsi"), "transform"),
            ItemIconStyle = ItemActionIconStyle.NoItem,
            DisplayName = "changeling-transform",
            Description = "changeling-transform-desc",
            Event = new ChangelingTransformEvent()
        },
        new InstantAction()
        {
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Interface/Actions/changeling.rsi"), "extract_sting"),
            ItemIconStyle = ItemActionIconStyle.NoItem,
            DisplayName = "changeling-extract-dna-sting",
            Description = "changeling-extract-dna-sting-desc",
            Event = new SelectStingEvent("ExtractionSting")
        }
        //"ChangelingAbsorb"
    };
}
