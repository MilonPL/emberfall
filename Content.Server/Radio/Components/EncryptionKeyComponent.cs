using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Radio.Components;

[RegisterComponent]
public sealed class EncryptionKeyComponent : Component
{

    [DataField("channels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    public HashSet<string> Channels = new()
    {
        "Common"
    };
}
