using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Audio;

/// <summary>
/// Attaches a rules prototype to sound files to play ambience.
/// </summary>
[Prototype("ambientMusic")]
public sealed partial class AmbientMusicPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    /// <summary>
    /// Traditionally you'd prioritise most rules to least as priority but in our case we'll just be explicit.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public int Priority = 0;

    /// <summary>
    /// Can we interrupt this ambience for a better prototype if possible?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool Interruptable = false;

    /// <summary>
    /// Do we fade-in. Useful for songs.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool FadeIn;

    [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
    public ProtoId<RulesPrototype> Rules = string.Empty;
}
