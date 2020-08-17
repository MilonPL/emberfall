﻿
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Server.Interfaces.Player;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Interfaces;
using Content.Server.GameObjects.Components.Body;
using System;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class ClimbableComponent : SharedClimbableComponent, IDragDropOn
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
#pragma warning restore 649

        /// <summary>
        ///     The range from which this entity can be climbed.
        /// </summary>
        [ViewVariables]
        private float _range;

        /// <summary>
        ///     The time it takes to climb the entity.
        /// </summary>
        [ViewVariables]
        private float _climbDelay;

        private ICollidableComponent _collidableComponent;

        public override void Initialize()
        {
            base.Initialize();

            _collidableComponent = Owner.GetComponent<ICollidableComponent>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _range, "range", SharedInteractionSystem.InteractionRange / 1.4f);
            serializer.DataField(ref _climbDelay, "delay", 0.8f);
        }

        bool IDragDropOn.CanDragDropOn(DragDropEventArgs eventArgs)
        {
            if (!ActionBlockerSystem.CanInteract(eventArgs.User))
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You can't do that!"));

                return false;
            }

            if (eventArgs.User == eventArgs.Dropped) // user is dragging themselves onto a climbable
            {
                if (eventArgs.Target == null) 
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You can't climb that!"));

                    return false;
                }

                if (!eventArgs.User.HasComponent<ClimbModeComponent>())
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You are incapable of climbing!"));

                    return false;
                }

                var bodyManager = eventArgs.User.GetComponent<BodyManagerComponent>();

                if (bodyManager.GetBodyPartsOfType(Shared.GameObjects.Components.Body.BodyPartType.Leg).Count == 0 ||
                    bodyManager.GetBodyPartsOfType(Shared.GameObjects.Components.Body.BodyPartType.Foot).Count == 0) 
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You are unable to climb!"));

                    return false;
                }

                var userPosition = eventArgs.User.Transform.MapPosition;
                var climbablePosition = eventArgs.Target.Transform.MapPosition;
                var interaction = EntitySystem.Get<SharedInteractionSystem>();
                bool Ignored(IEntity entity) => (entity == eventArgs.Target || entity == eventArgs.User);

                if (!interaction.InRangeUnobstructed(userPosition, climbablePosition, _range, predicate: Ignored))
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You can't reach there!"));

                    return false;
                }
            }
            else // user is dragging some other entity onto a climbable
            {
                if (eventArgs.Target == null || !eventArgs.Dropped.HasComponent<ClimbModeComponent>()) 
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You can't do that!"));
                    
                    return false;
                }

                var userPosition = eventArgs.User.Transform.MapPosition;
                var otherUserPosition = eventArgs.Dropped.Transform.MapPosition;
                var climbablePosition = eventArgs.Target.Transform.MapPosition;
                var interaction = EntitySystem.Get<SharedInteractionSystem>();
                bool Ignored(IEntity entity) => (entity == eventArgs.Target || entity == eventArgs.User || entity == eventArgs.Dropped);

                if (!interaction.InRangeUnobstructed(userPosition, climbablePosition, _range, predicate: Ignored) ||
                    !interaction.InRangeUnobstructed(userPosition, otherUserPosition, _range, predicate: Ignored))
                {
                    _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You can't reach there!"));

                    return false;
                }
            }

            return true;
        }

        bool IDragDropOn.DragDropOn(DragDropEventArgs eventArgs)
        {
            if (eventArgs.User == eventArgs.Dropped)
            {
                return TryClimb(eventArgs.User);
            }
            else
            {
                return TryMoveEntity(eventArgs.User, eventArgs.Dropped);
            }
        }

        private bool TryMoveEntity(IEntity user, IEntity entityToMove)
        {
            if (user.TryGetComponent(out ICollidableComponent body) && body.PhysicsShapes.Count >= 1)
            {
                var direction = (Owner.Transform.WorldPosition - entityToMove.Transform.WorldPosition).Normalized;
                var endPoint = Owner.Transform.WorldPosition;

                var climbMode = entityToMove.GetComponent<ClimbModeComponent>();
                climbMode.SetClimbing(true);

                if (MathF.Abs(direction.X) < 0.6f) // user climbed mostly vertically so lets make it a clean straight line
                {
                    endPoint = new Robust.Shared.Maths.Vector2(entityToMove.Transform.WorldPosition.X, endPoint.Y);
                }
                else if (MathF.Abs(direction.Y) < 0.6f) // user climbed mostly horizontally so lets make it a clean straight line
                {
                    endPoint = new Robust.Shared.Maths.Vector2(endPoint.X, entityToMove.Transform.WorldPosition.Y);
                }

                climbMode.TryMoveTo(entityToMove.Transform.WorldPosition, endPoint);
                // we may potentially need additional logic since we're forcing a player onto a climbable
                // there's also the cases where the user might collide with the person they are forcing onto the climbable that i haven't accounted for

                PopupMessageOtherClientsInRange(user, Loc.GetString("{0:them} forces {1:them} onto {2:theName}!", user, entityToMove, Owner), 15);
                _notifyManager.PopupMessage(user, user, Loc.GetString("You force {0:them} onto {1:theName}!", entityToMove, Owner));

                return true;
            }

            return false;
        }

        private bool TryClimb(IEntity user)
        {
            if (user.TryGetComponent(out ICollidableComponent body) && body.PhysicsShapes.Count >= 1)
            {
                var direction = (Owner.Transform.WorldPosition - user.Transform.WorldPosition).Normalized;
                var endPoint = Owner.Transform.WorldPosition;

                var climbMode = user.GetComponent<ClimbModeComponent>();
                climbMode.SetClimbing(true);

                if (MathF.Abs(direction.X) < 0.6f) // user climbed mostly vertically so lets make it a clean straight line
                {
                    endPoint = new Robust.Shared.Maths.Vector2(user.Transform.WorldPosition.X, endPoint.Y);
                }
                else if (MathF.Abs(direction.Y) < 0.6f) // user climbed mostly horizontally so lets make it a clean straight line
                {
                    endPoint = new Robust.Shared.Maths.Vector2(endPoint.X, user.Transform.WorldPosition.Y);
                }

                climbMode.TryMoveTo(user.Transform.WorldPosition, endPoint);

                PopupMessageOtherClientsInRange(user, Loc.GetString("{0:them} jumps onto the {1}!", user, Owner), 15);
                _notifyManager.PopupMessage(user, user, Loc.GetString("You jump onto {0:theName}!", Owner));

                return true;
            }

            return false;
        }

        private void PopupMessageOtherClientsInRange(IEntity source, string message, int maxReceiveDistance)
        {
            var viewers = _playerManager.GetPlayersInRange(source.Transform.GridPosition, maxReceiveDistance);

            foreach (var viewer in viewers)
            {
                var viewerEntity = viewer.AttachedEntity;

                if (viewerEntity == null || source == viewerEntity)
                {
                    continue;
                }

                source.PopupMessage(viewer.AttachedEntity, message);
            }
        }
    }
}
