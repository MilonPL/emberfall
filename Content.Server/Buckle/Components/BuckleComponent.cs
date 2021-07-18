#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Alert;
using Content.Server.Hands.Components;
using Content.Server.MobState.States;
using Content.Server.Pulling;
using Content.Server.Stunnable.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Notification.Managers;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.Buckle.Components
{
    /// <summary>
    ///     Component that handles sitting entities into <see cref="StrapComponent"/>s.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SharedBuckleComponent))]
    public class BuckleComponent : SharedBuckleComponent
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [ComponentDependency] public readonly AppearanceComponent? Appearance = null;
        [ComponentDependency] private readonly ServerAlertsComponent? _serverAlerts = null;
        [ComponentDependency] private readonly StunnableComponent? _stunnable = null;
        [ComponentDependency] private readonly MobStateComponent? _mobState = null;

        [DataField("size")]
        private int _size = 100;

        /// <summary>
        ///     The amount of time that must pass for this entity to
        ///     be able to unbuckle after recently buckling.
        /// </summary>
        [DataField("delay")]
        [ViewVariables]
        private TimeSpan _unbuckleDelay  = TimeSpan.FromSeconds(0.25f);

        /// <summary>
        ///     The time that this entity buckled at.
        /// </summary>
        [ViewVariables]
        private TimeSpan _buckleTime;

        /// <summary>
        ///     The position offset that is being applied to this entity if buckled.
        /// </summary>
        public Vector2 BuckleOffset { get; private set; }

        private StrapComponent? _buckledTo;

        /// <summary>
        ///     The strap that this component is buckled to.
        /// </summary>
        [ViewVariables]
        public StrapComponent? BuckledTo
        {
            get => _buckledTo;
            private set
            {
                _buckledTo = value;
                _buckleTime = _gameTiming.CurTime;
                Dirty();
            }
        }

        [ViewVariables]
        public override bool Buckled => BuckledTo != null;

        /// <summary>
        ///     The amount of space that this entity occupies in a
        ///     <see cref="StrapComponent"/>.
        /// </summary>
        [ViewVariables]
        public int Size => _size;

        /// <summary>
        ///     Shows or hides the buckled status effect depending on if the
        ///     entity is buckled or not.
        /// </summary>
        private void UpdateBuckleStatus()
        {
            if (_serverAlerts == null)
            {
                return;
            }

            if (Buckled)
            {
                _serverAlerts.ShowAlert(BuckledTo?.BuckledAlertType ?? AlertType.Buckled);
            }
            else
            {
                _serverAlerts.ClearAlertCategory(AlertCategory.Buckled);
            }
        }

        /// <summary>
        ///     Reattaches this entity to the strap, modifying its position and rotation.
        /// </summary>
        /// <param name="strap">The strap to reattach to.</param>
        public void ReAttach(StrapComponent strap)
        {
            var ownTransform = Owner.Transform;
            var strapTransform = strap.Owner.Transform;

            ownTransform.AttachParent(strapTransform);

            switch (strap.Position)
            {
                case StrapPosition.None:
                    ownTransform.WorldRotation = strapTransform.WorldRotation;
                    break;
                case StrapPosition.Stand:
                    EntitySystem.Get<StandingStateSystem>().Stand(Owner);
                    ownTransform.WorldRotation = strapTransform.WorldRotation;
                    break;
                case StrapPosition.Down:
                    EntitySystem.Get<StandingStateSystem>().Down(Owner, false, false);
                    ownTransform.LocalRotation = Angle.Zero;
                    break;
            }

            // Assign BuckleOffset first, before causing a MoveEvent to fire
            if (strapTransform.WorldRotation.GetCardinalDir() == Direction.North)
            {
                BuckleOffset = (0, 0.15f);
                ownTransform.WorldPosition = strapTransform.WorldPosition + BuckleOffset;
            }
            else
            {
                BuckleOffset = Vector2.Zero;
                ownTransform.WorldPosition = strapTransform.WorldPosition;
            }
        }

        private bool CanBuckle(IEntity? user, IEntity to, [NotNullWhen(true)] out StrapComponent? strap, bool check_range = true, float? range = null)
        {
            strap = null;

            if (user == null || user == to)
            {
                return false;
            }

            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
            {
                user.PopupMessage(Loc.GetString("buckle-component-cannot-do-that-message"));
                return false;
            }

            if (!to.TryGetComponent(out strap))
            {
                return false;
            }

            if (check_range)
            {
                var component = strap;
                bool Ignored(IEntity entity) => entity == Owner || entity == user || entity == component.Owner;

                if (range == null) range = Range;

                if (!Owner.InRangeUnobstructed(strap, (float) range, predicate: Ignored, popup: true))
                {
                    return false;
                }
            }

            // If in a container
            if (Owner.TryGetContainer(out var ownerContainer))
            {
                // And not in the same container as the strap
                if (!strap.Owner.TryGetContainer(out var strapContainer) ||
                    ownerContainer != strapContainer)
                {
                    return false;
                }
            }

            if (!user.HasComponent<HandsComponent>())
            {
                user.PopupMessage(Loc.GetString("buckle-component-no-hands-message "));
                return false;
            }

            if (Buckled)
            {
                var message = Loc.GetString(Owner == user
                    ? "buckle-component-already-buckled-message"
                    : "buckle-component-other-already-buckled-message",("owner", Owner));
                Owner.PopupMessage(user, message);

                return false;
            }

            var parent = to.Transform.Parent;
            while (parent != null)
            {
                if (parent == user.Transform)
                {
                    var message = Loc.GetString(Owner == user
                        ? "buckle-component-cannot-buckle-message"
                        : "buckle-component-other-cannot-buckle-message",("owner", Owner));
                    Owner.PopupMessage(user, message);

                    return false;
                }

                parent = parent.Parent;
            }

            if (!strap.HasSpace(this))
            {
                var message = Loc.GetString(Owner == user
                    ? "buckle-component-cannot-fit-message"
                    : "buckle-component-other-cannot-fit-message",("owner", Owner));
                Owner.PopupMessage(user, message);

                return false;
            }

            return true;
        }

        public override bool TryBuckle(IEntity? user, IEntity to, bool check_range = true, float? range = null)
        {
            if (user == null || !CanBuckle(user, to, out var strap, check_range, range))
            {
                return false;
            }

            var buckleAttemptMsg = new BuckleAttemptEvent(user, Owner);
            Owner.EntityManager.EventBus.RaiseLocalEvent(strap.Owner.Uid, buckleAttemptMsg);
            if (buckleAttemptMsg.Cancelled) return false;

            SoundSystem.Play(Filter.Pvs(Owner), strap.BuckleSound, Owner);

            if (!strap.TryAdd(this))
            {
                var message = Loc.GetString(Owner == user
                    ? "buckle-component-cannot-buckle-message"
                    : "buckle-component-other-cannot-buckle-message",("owner", Owner));
                Owner.PopupMessage(user, message);
                return false;
            }

            Appearance?.SetData(BuckleVisuals.Buckled, true);

            ReAttach(strap);

            BuckledTo = strap;
            LastEntityBuckledTo = BuckledTo.Owner.Uid;
            DontCollide = true;

            UpdateBuckleStatus();

            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new BuckleMessage(Owner, to));

            if (Owner.TryGetComponent(out PullableComponent? ownerPullable))
            {
                if (ownerPullable.Puller != null)
                {
                    ownerPullable.TryStopPull();
                }
            }

            if (to.TryGetComponent(out PullableComponent? toPullable))
            {
                if (toPullable.Puller == Owner)
                {
                    // can't pull it and buckle to it at the same time
                    toPullable.TryStopPull();
                }
            }

            return true;
        }

        /// <summary>
        ///     Tries to unbuckle the Owner of this component from its current strap.
        /// </summary>
        /// <param name="user">The entity doing the unbuckling.</param>
        /// <param name="force">
        ///     Whether to force the unbuckling or not. Does not guarantee true to
        ///     be returned, but guarantees the owner to be unbuckled afterwards.
        /// </param>
        /// <returns>
        ///     true if the owner was unbuckled, otherwise false even if the owner
        ///     was previously already unbuckled.
        /// </returns>
        public bool TryUnbuckle(IEntity user, bool force = false)
        {
            if (BuckledTo == null)
            {
                return false;
            }

            var oldBuckledTo = BuckledTo;

            if (!force)
            {
                // Send message to entity to which mob is buckled to
                var unbuckleAttemptMsg = new UnbuckleAttemptEvent(user, Owner);
                Owner.EntityManager.EventBus.RaiseLocalEvent(BuckledTo.Owner.Uid, unbuckleAttemptMsg);
                if (unbuckleAttemptMsg.Cancelled) return false;

                if (_gameTiming.CurTime < _buckleTime + _unbuckleDelay)
                {
                    return false;
                }

                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    user.PopupMessage(Loc.GetString("buckle-component-cannot-do-that-message"));
                    return false;
                }

                if (!user.InRangeUnobstructed(oldBuckledTo, Range, popup: true))
                {
                    return false;
                }
            }

            BuckledTo = null;

            if (Owner.Transform.Parent == oldBuckledTo.Owner.Transform)
            {
                Owner.Transform.AttachParentToContainerOrGrid();
                Owner.Transform.WorldRotation = oldBuckledTo.Owner.Transform.WorldRotation;
            }

            Appearance?.SetData(BuckleVisuals.Buckled, false);

            if (_stunnable != null && _stunnable.KnockedDown
                || (_mobState?.IsIncapacitated() ?? false))
            {
                EntitySystem.Get<StandingStateSystem>().Down(Owner);
            }
            else
            {
                EntitySystem.Get<StandingStateSystem>().Stand(Owner);
            }

            _mobState?.CurrentState?.EnterState(Owner);

            UpdateBuckleStatus();

            oldBuckledTo.Remove(this);
            SoundSystem.Play(Filter.Pvs(Owner), oldBuckledTo.UnbuckleSound, Owner);

            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new UnbuckleMessage(Owner, oldBuckledTo.Owner));

            return true;
        }

        /// <summary>
        ///     Makes an entity toggle the buckling status of the owner to a
        ///     specific entity.
        /// </summary>
        /// <param name="user">The entity doing the buckling/unbuckling.</param>
        /// <param name="to">
        ///     The entity to toggle the buckle status of the owner to.
        /// </param>
        /// <param name="force">
        ///     Whether to force the unbuckling or not, if it happens. Does not
        ///     guarantee true to be returned, but guarantees the owner to be
        ///     unbuckled afterwards.
        /// </param>
        /// <returns>true if the buckling status was changed, false otherwise.</returns>
        public bool ToggleBuckle(IEntity user, IEntity to, bool force = false)
        {
            if (BuckledTo?.Owner == to)
            {
                return TryUnbuckle(user, force);
            }

            return TryBuckle(user, to);
        }

        protected override void Startup()
        {
            base.Startup();
            UpdateBuckleStatus();
        }

        protected override void OnRemove()
        {
            BuckledTo?.Remove(this);
            TryUnbuckle(Owner, true);

            _buckleTime = default;
            UpdateBuckleStatus();

            base.OnRemove();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            int? drawDepth = null;

            if (BuckledTo != null &&
                Owner.Transform.WorldRotation.GetCardinalDir() == Direction.North &&
                BuckledTo.SpriteComponent != null)
            {
                drawDepth = BuckledTo.SpriteComponent.DrawDepth - 1;
            }

            return new BuckleComponentState(Buckled, drawDepth, LastEntityBuckledTo, DontCollide);
        }

        public void Update(PhysicsComponent physics)
        {
            if (!DontCollide)
                return;

            physics.WakeBody();

            if (!IsOnStrapEntityThisFrame && DontCollide)
            {
                DontCollide = false;
                TryUnbuckle(Owner);
                Dirty();
            }

            IsOnStrapEntityThisFrame = false;
        }

        /// <summary>
        ///     Allows the unbuckling of the owning entity through a verb if
        ///     anyone right clicks them.
        /// </summary>
        [Verb]
        private sealed class BuckleVerb : Verb<BuckleComponent>
        {
            protected override void GetData(IEntity user, BuckleComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) || !component.Buckled)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("buckle-verb-unbuckle");
            }

            protected override void Activate(IEntity user, BuckleComponent component)
            {
                component.TryUnbuckle(user);
            }
        }
    }
}
