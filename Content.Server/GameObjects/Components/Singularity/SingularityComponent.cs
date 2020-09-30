using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.Components.StationEvents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Sound;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class SingularityComponent : Component, ICollideBehavior
    {
        [Dependency] private IEntityManager _entityManager;
        [Dependency] private IMapManager _mapManager;
        [Dependency] private IRobustRandom _random;


        public override uint? NetID => ContentNetIDs.SINGULARITY;

        public override string Name => "Singularity";

        public int Energy
        {
            get => _energy;
            set
            {
                if (value != _energy) return;

                _energy = value;
                if (_energy <= 0)
                {
                    SendNetworkMessage(new SingularitySoundMessage(false));

                    _singularityController.LinearVelocity = Vector2.Zero;
                    dyingTransition = true;
                    _spriteComponent.LayerSetVisible(0, false);

                    Timer.Spawn(7500, () => Owner.Delete());
                    return;
                }

                Level = _energy switch
                {
                    var n when n >= 1500 => 6,
                    var n when n >= 1000 => 5,
                    var n when n >= 600 => 4,
                    var n when n >= 300 => 3,
                    var n when n >= 200 => 2,
                    var n when n <  200 => 1,
                    _ => 1
                };
            }
        }
        private int _energy = 100;

        public int Level
        {
            get => _level;
            set
            {
                if (value == _level) return;
                if (value < 0) value = 0;
                if (value > 6) value = 6;

                _level = value;
                _radiationPulseComponent.RadsPerSecond = 10 * value;

                _spriteComponent.LayerSetRSI(0, "Effects/Singularity/singularity_" + _level + ".rsi");
                _spriteComponent.LayerSetState(0, "singularity_" + _level);

                (_collidableComponent.PhysicsShapes[0] as PhysShapeCircle).Radius = _level - 0.5f;
            }
        }
        private int _level;

        public int EnergyDrain =>
            Level switch
            {
                6 => 20,
                5 => 15,
                4 => 10,
                3 => 5,
                2 => 2,
                1 => 1,
                _ => 0
            };

        private SingularityController _singularityController;
        private ICollidableComponent _collidableComponent;
        private SpriteComponent _spriteComponent;
        private RadiationPulseComponent _radiationPulseComponent;

        private bool dyingTransition;

        private bool repelled = false;

        public override void Initialize()
        {
            base.Initialize();

            _collidableComponent = Owner.GetComponent<ICollidableComponent>();
            _collidableComponent.Hard = false;

            _spriteComponent = Owner.GetComponent<SpriteComponent>();

            _singularityController = _collidableComponent.EnsureController<SingularityController>();
            _singularityController.ControlledComponent = _collidableComponent;

            _radiationPulseComponent = Owner.GetComponent<RadiationPulseComponent>();
            Level = 1;
        }

        protected override void Startup()
        {
            SendNetworkMessage(new SingularitySoundMessage(true));
        }

        public void Update()
        {
            if (dyingTransition)
            {
                return;
            }

            Energy -= EnergyDrain;

            if (!repelled)
            {
                _singularityController.Push(new Vector2((_random.Next(-10, 10)), _random.Next(-10, 10)).Normalized, 2);
            }
        }

        public void TileUpdate()
        {
            var mapGrid = _mapManager.GetGrid(Owner.Transform.GridID);
            foreach (var tile in mapGrid.GetTilesIntersecting(_collidableComponent.WorldAABB))
            {
                mapGrid.SetTile(tile.GridIndices, Tile.Empty);
                Energy++;
            }
        }

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (repelled)
            {
                return;
            }

            if (entity.HasComponent<ContainmentFieldComponent>() || (entity.TryGetComponent<ContainmentFieldGeneratorComponent>(out var component) && component.Power >= 1))
            {
                return;
                //repelled = true;
                //Timer.Spawn(50, () => repelled = false);

                /*if (entity.Transform.WorldRotation.Degrees == -90f ||
                    entity.Transform.WorldRotation.Degrees == 90f)
                {
                    Vector2 normal = new Vector2(0.05f * Math.Sign(Owner.Transform.WorldPosition.X - entity.Transform.WorldPosition.X), 0);

                    if (normal == Vector2.Zero)
                    {
                        normal = new Vector2(0.05f, 0);
                    }

                    _singularityController.LinearVelocity = new Vector2(_singularityController.LinearVelocity.X * -1, _singularityController.LinearVelocity.Y);

                    while (_entityManager.GetEntitiesIntersecting(Owner).Contains(entity))
                    {
                        Owner.Transform.WorldPosition += normal;
                    }

                }

                else
                {
                    Vector2 normal = new Vector2(0, 0.05f * Math.Sign(Owner.Transform.WorldPosition.Y - entity.Transform.WorldPosition.Y));

                    if (normal == Vector2.Zero)
                    {
                        normal = new Vector2(0, 0.05f);
                    }

                    _singularityController.LinearVelocity = new Vector2(_singularityController.LinearVelocity.X, _singularityController.LinearVelocity.Y * -1);

                    while (_entityManager.GetEntitiesIntersecting(Owner).Contains(entity))
                    {
                        Owner.Transform.WorldPosition += normal;
                    }
                }*/
            }

            if (ContainerHelpers.IsInContainer(entity)) return;

            entity.Delete();
            Energy++;
        }
    }
}
