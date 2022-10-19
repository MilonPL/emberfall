using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Humanoid.Prototypes;

[Prototype("species")]
public readonly record struct SpeciesPrototype : IPrototype
{
    /// <summary>
    /// Prototype ID of the species.
    /// </summary>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// User visible name of the species.
    /// </summary>
    [DataField("name", required: true)]
    public string Name { get; } = default!;

    /// <summary>
    ///     Descriptor. Unused...? This is intended
    ///     for an eventual integration into IdentitySystem
    ///     (i.e., young human person, young lizard person, etc.)
    /// </summary>
    [DataField("descriptor")]
    public string Descriptor { get; } = "humanoid";

    /// <summary>
    /// Whether the species is available "at round start" (In the character editor)
    /// </summary>
    [DataField("roundStart", required: true)]
    public bool RoundStart { get; } = false;

    // The below two are to avoid fetching information about the species from the entity
    // prototype.

    // This one here is a utility field, and is meant to *avoid* having to duplicate
    // the massive SpriteComponent found in every species.
    // Species implementors can just override SpriteComponent if they want a custom
    // sprite layout, and leave this null. Keep in mind that this will disable
    // sprite accessories.

    [DataField("sprites")]
    public string SpriteSet { get; } = default!;

    /// <summary>
    ///     Default skin tone for this species. This applies for non-human skin tones.
    /// </summary>
    [DataField("defaultSkinTone")]
    public Color DefaultSkinTone { get; } = Color.White;

    /// <summary>
    ///     Default human skin tone for this species. This applies for human skin tones.
    ///     See <see cref="SkinColor.HumanSkinTone"/> for the valid range of skin tones.
    /// </summary>
    [DataField("defaultHumanSkinTone")]
    public int DefaultHumanSkinTone { get; } = 20;

    /// <summary>
    ///     The limit of body markings that you can place on this species.
    /// </summary>
    [DataField("markingLimits")]
    public string MarkingPoints { get; } = default!;

    /// <summary>
    ///     Humanoid species variant used by this entity.
    /// </summary>
    [DataField("prototype", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype { get; } = default!;

    /// <summary>
    /// Prototype used by the species for the dress-up doll in various menus.
    /// </summary>
    [DataField("dollPrototype", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DollPrototype { get; } = default!;

    /// <summary>
    /// Method of skin coloration used by the species.
    /// </summary>
    [DataField("skinColoration", required: true)]
    public HumanoidSkinColor SkinColoration { get; }

    [DataField("maleFirstNames")]
    public string MaleFirstNames { get; } = "names_first_male";

    [DataField("femaleFirstNames")]
    public string FemaleFirstNames { get; } = "names_first_female";

    [DataField("lastNames")]
    public string LastNames { get; } = "names_last";

    [DataField("naming")]
    public SpeciesNaming Naming { get; } = SpeciesNaming.FirstLast;

    [DataField("sexes")]
    public List<Sex> Sexes { get; } = new List<Sex>(){ Sex.Male, Sex.Female };
}

public enum SpeciesNaming : byte
{
    FirstLast,
    FirstDashFirst,
}
