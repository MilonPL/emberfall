using Content.Server.Fluids.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Server.Fluids.Components
{
    /// <summary>
    /// Puddle on a floor
    /// </summary>
    [RegisterComponent]
    [Access(typeof(PuddleSystem))]
    public sealed class PuddleComponent : Component
    {
        /// <summary>
        /// Puddles with volume above this threshold can slip players.
        /// </summary>
        [DataField("slipThreshold")]
        public FixedPoint2 SlipThreshold = FixedPoint2.New(-1);

        [DataField("spillSound")]
        public SoundSpecifier SpillSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

        [DataField("overflowVolume")]
        public FixedPoint2 OverflowVolume = FixedPoint2.New(100);

        [DataField("solution")] public string SolutionName = "puddle";
    }
}
