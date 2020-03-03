using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power
{
    [Serializable, NetSerializable]
    public enum LatheVisualState
    {
        Base,
        Building,
        Inserting,
        Reloading
    }
}
