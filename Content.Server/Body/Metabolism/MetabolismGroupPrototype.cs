﻿using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Body.Metabolism
{
    [Prototype("metabolismGroup")]
    public class MetabolismGroupPrototype : IPrototype
    {
        [DataField("id", required: true)]
        public string ID => default!;
    }
}
