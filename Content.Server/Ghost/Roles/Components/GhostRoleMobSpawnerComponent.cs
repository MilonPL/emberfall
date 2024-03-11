﻿using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Ghost.Roles.Components
{
    /// <summary>
    ///     Allows a ghost to take this role, spawning a new entity.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(GhostRoleSystem))]
    public sealed partial class GhostRoleMobSpawnerComponent : Component
    {
        [DataField]
        public bool DeleteOnSpawn = true;

        [DataField]
        public int AvailableTakeovers = 1;

        [ViewVariables]
        public int CurrentTakeovers = 0;

        [DataField]
        public string? Prototype;

        [DataField]
        public List<string> SelectablePrototypes = [];
    }
}
