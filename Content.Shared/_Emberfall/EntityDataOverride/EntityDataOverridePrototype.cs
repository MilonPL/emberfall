using Robust.Shared.Prototypes;

namespace Content.Shared._Emberfall.EntityDataOverride;

/// <summary>
///     Prototype for overriding entity name and description without modifying the original entity prototype.
/// </summary>
[Prototype]
public sealed class EntityDataOverridePrototype : IPrototype
{
    /// <inheritdoc/>
    /// <remarks>Must match the entity ID you're trying to override</remarks>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     The custom name to override the entity's default name.
    /// </summary>
    [DataField]
    public string? Name { get; }

    /// <summary>
    ///     The custom description to override the entity's default description.
    /// </summary>
    [DataField]
    public string? Description { get; }
}
