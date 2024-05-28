using Content.Shared.Alert;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Gravity
{
    public abstract partial class SharedGravitySystem : EntitySystem
    {
        [Dependency] protected readonly IGameTiming Timing = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;

        [ValidatePrototypeId<AlertPrototype>]
        public const string WeightlessAlert = "Weightless";

        private EntityQuery<InventoryComponent> _inventoryQuery;

        public bool IsWeightless(EntityUid uid, PhysicsComponent? body = null, TransformComponent? xform = null)
        {
            Resolve(uid, ref body, false);

            if ((body?.BodyType & (BodyType.Static | BodyType.Kinematic)) != 0)
                return false;

            if (TryComp<MovementIgnoreGravityComponent>(uid, out var ignoreGravityComponent))
                return ignoreGravityComponent.Weightless;

            if (!Resolve(uid, ref xform))
                return true;

            // If grid / map has gravity
            if (TryComp<GravityComponent>(xform.GridUid, out var gravity) && gravity.Enabled ||
                 TryComp<GravityComponent>(xform.MapUid, out var mapGravity) && mapGravity.Enabled)
            {
                return false;
            }

            // If there's no gravity comp at all (i.e. space) then nothing can hold you down
            if (gravity == null && mapGravity == null)
                return true;

            // Check for something holding us down
            // If the planet has gravity component and no gravity it will still give gravity
            var ev = new CheckGravityEvent();
            RaiseLocalEvent(uid, ref ev);
            if (ev.Handled)
                return false;

            if (_inventoryQuery.TryComp(uid, out var inv))
            {
                _inventory.RelayEvent((uid, inv), ref ev);
                if (ev.Handled)
                    return false;
            }

            // on a grid without gravity and no magboots, floating time
            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            _inventoryQuery = GetEntityQuery<InventoryComponent>();

            SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
            SubscribeLocalEvent<AlertSyncEvent>(OnAlertsSync);
            SubscribeLocalEvent<AlertsComponent, EntParentChangedMessage>(OnAlertsParentChange);
            SubscribeLocalEvent<GravityChangedEvent>(OnGravityChange);
            SubscribeLocalEvent<GravityComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<GravityComponent, ComponentHandleState>(OnHandleState);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            UpdateShake();
        }

        private void OnHandleState(EntityUid uid, GravityComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not GravityComponentState state) return;

            if (component.EnabledVV == state.Enabled) return;
            component.EnabledVV = state.Enabled;
            var ev = new GravityChangedEvent(uid, component.EnabledVV);
            RaiseLocalEvent(uid, ref ev, true);
        }

        private void OnGetState(EntityUid uid, GravityComponent component, ref ComponentGetState args)
        {
            args.State = new GravityComponentState(component.EnabledVV);
        }

        private void OnGravityChange(ref GravityChangedEvent ev)
        {
            var alerts = AllEntityQuery<AlertsComponent, TransformComponent>();
            while(alerts.MoveNext(out var uid, out var comp, out var xform))
            {
                if (xform.GridUid != ev.ChangedGridIndex) continue;

                if (!ev.HasGravity)
                {
                    _alerts.ShowAlert(uid, WeightlessAlert);
                }
                else
                {
                    _alerts.ClearAlert(uid, WeightlessAlert);
                }
            }
        }

        private void OnAlertsSync(AlertSyncEvent ev)
        {
            if (IsWeightless(ev.Euid))
            {
                _alerts.ShowAlert(ev.Euid, WeightlessAlert);
            }
            else
            {
                _alerts.ClearAlert(ev.Euid, WeightlessAlert);
            }
        }

        private void OnAlertsParentChange(EntityUid uid, AlertsComponent component, ref EntParentChangedMessage args)
        {
            if (IsWeightless(uid))
            {
                _alerts.ShowAlert(uid, WeightlessAlert);
            }
            else
            {
                _alerts.ClearAlert(uid, WeightlessAlert);
            }
        }

        private void OnGridInit(GridInitializeEvent ev)
        {
            EntityManager.EnsureComponent<GravityComponent>(ev.EntityUid);
        }

        [Serializable, NetSerializable]
        private sealed class GravityComponentState : ComponentState
        {
            public bool Enabled { get; }

            public GravityComponentState(bool enabled)
            {
                Enabled = enabled;
            }
        }
    }
}
