﻿using Content.Server.GameObjects.Components.Body;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Body
{
    [UsedImplicitly]
    public class BodySystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var body in EntityManager.ComponentManager.EntityQuery<BodyManagerComponent>())
            {
                // TODO
            }
        }
    }
}
