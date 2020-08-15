﻿using Content.Server.GameObjects.Components.Fluids;
using Content.Server.Atmos;
using Content.Shared.Physics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Robust.Server.GameObjects;
using Content.Server.Atmos.Reactions;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.Atmos
{
    [RegisterComponent]
    class GasVaporComponent : Component, ICollideBehavior, IGasMixtureHolder
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager = default!;
#pragma warning enable 649
        public override string Name => "GasVapor";

        //TODO: IDK if this is a good way to initilize contents
        [ViewVariables] public GasMixture Air { get; set; }

        [ViewVariables] private GridAtmosphereComponent _gridAtmosphereComponent;

        private bool _running;
        private Vector2 _direction;
        private float _velocity;
        private float disspateTimer = 0;


        public void Initialize(GridAtmosphereComponent gridAtmosphereComponent)
        {
            base.Initialize();
            _gridAtmosphereComponent = gridAtmosphereComponent;
        }

        public void StartMove(Vector2 dir, float velocity)
        {
            _running = true;
            _direction = dir;
            _velocity = velocity;

            if (Owner.TryGetComponent(out ICollidableComponent collidable))
            {
                var controller = collidable.EnsureController<GasVaporController>();
                controller.Move(_direction, _velocity);
            }
        }

        public void Update(float frameTime)
        {
            if (!_running)
                return;

            if (Owner.TryGetComponent(out ICollidableComponent collidable))
            {
                var worldBounds = collidable.WorldAABB;
                var mapGrid = _mapManager.GetGrid(Owner.Transform.GridID);

                var tiles = mapGrid.GetTilesIntersecting(worldBounds);

                foreach (var tile in tiles)
                {
                    var pos = tile.GridIndices.ToGridCoordinates(_mapManager, tile.GridIndex);
                    var atmos = AtmosHelpers.GetTileAtmosphere(pos);

                    if (atmos.Air == null)
                    {
                        return;
                    }

                    if (atmos.Air.React(this) != ReactionResult.NoReaction)
                    {
                        Owner.Delete();
                    }
                }
            }

            disspateTimer += frameTime;
            if (disspateTimer > 1)
            {
                Air.SetMoles(Gas.WaterVapor, Air.TotalMoles/2 );
            }

            if (Air.TotalMoles < 1)
            {
                Owner.Delete();
            }
        }

        void ICollideBehavior.CollideWith(IEntity collidedWith)
        {
            // Check for collision with a impassable object (e.g. wall) and stop
            if (collidedWith.TryGetComponent(out ICollidableComponent collidable) &&
                (collidable.CollisionLayer & (int) CollisionGroup.Impassable) != 0 &&
                collidable.Hard &&
                Owner.TryGetComponent(out ICollidableComponent coll))
            {
                var controller = coll.EnsureController<GasVaporController>();
                controller.Stop();
                Owner.Delete();
            }
        }
    }
}
