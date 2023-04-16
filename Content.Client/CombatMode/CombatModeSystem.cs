using Content.Shared.CombatMode;
using Content.Shared.Targeting;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Client.Graphics;

namespace Content.Client.CombatMode
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : SharedCombatModeSystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public event Action? LocalPlayerCombatModeUpdated;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CombatModeComponent, ComponentHandleState>(OnHandleState);
            _overlayManager.AddOverlay(new ShowCombatModeIndicatorsOverlay());
        }

        private void OnHandleState(EntityUid uid, CombatModeComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not CombatModeComponentState state)
                return;

            component.IsInCombatMode = state.IsInCombatMode;
            component.ActiveZone = state.TargetingZone;
            UpdateHud(uid);
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<CombatModeSystem>();
            _overlayManager.RemoveOverlay<ShowCombatModeIndicatorsOverlay>();

            base.Shutdown();
        }

        private void OnTargetingZoneChanged(TargetingZone obj)
        {
            EntityManager.RaisePredictiveEvent(new CombatModeSystemMessages.SetTargetZoneMessage(obj));
        }

        public bool IsInCombatMode()
        {
            var entity = _playerManager.LocalPlayer?.ControlledEntity;

            if (entity == null)
                return false;

            return IsInCombatMode(entity.Value);
        }

        public override void SetInCombatMode(EntityUid entity, bool inCombatMode, CombatModeComponent? component = null)
        {
            base.SetInCombatMode(entity, inCombatMode, component);
            UpdateHud(entity);
        }

        public override void SetActiveZone(EntityUid entity, TargetingZone zone, CombatModeComponent? component = null)
        {
            base.SetActiveZone(entity, zone, component);
            UpdateHud(entity);
        }

        private void UpdateHud(EntityUid entity)
        {
            if (entity != _playerManager.LocalPlayer?.ControlledEntity)
            {
                return;
            }

            LocalPlayerCombatModeUpdated?.Invoke();
        }
    }
}
