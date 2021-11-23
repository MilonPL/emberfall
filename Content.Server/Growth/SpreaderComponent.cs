using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Growth;

/// <summary>
/// Component for rapidly spreading objects, like Kudzu.
/// ONLY USE THIS FOR ANCHORED OBJECTS. An error will be logged if not anchored/static.
/// Currently does not support growing in space.
/// </summary>
[RegisterComponent]
public class SpreaderComponent : Component
{
    public override string Name => "Spreader";

    /// <summary>
    /// Chance for it to grow on any given tick, after the normal growth rate-limit (if it doesn't grow, SpreaderSystem will pick another one.).
    /// </summary>
    [ViewVariables, DataField("chance", required: true)]
    public float Chance = 1.0f;

    /// <summary>
    /// Prototype spawned on growth success.
    /// </summary>
    [ViewVariables, DataField("growthResult", required: true)]
    public string GrowthResult = default!;
}
