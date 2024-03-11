using Robust.Shared.Prototypes;

namespace Content.Shared.Ghost.Roles
{
    /// <summary>
    ///     For selectable ghostrole prototypes in ghostrole spawners.
    /// </summary>
    [Prototype("ghostrole")]
    public sealed partial class GhostRolePrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     The name of the ghostrole.
        /// </summary>
        [DataField]
        public string? Name { get; set; }

        /// <summary>
        ///     The description of the ghostrole.
        /// </summary>
        [DataField]
        public string? Description { get; set; }

        /// <summary>
        ///     The entity prototype of the ghostrole
        /// </summary>
        [DataField]
        public string? EntityPrototype;

        /// <summary>
        ///     Rules of the ghostrole
        /// </summary>
        [DataField]
        public string? Rules;
    }
}