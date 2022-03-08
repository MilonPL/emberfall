using Content.Shared.Alert;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Stop pulling something
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed class StopPulling : IAlertClick
    {
        public void AlertClicked(EntityUid player)
        {
            var ps = EntitySystem.Get<SharedPullingSystem>();
            var playerTarget = ps.GetPulled(player);
            if (playerTarget != default && IoCManager.Resolve<IEntityManager>().TryGetComponent(playerTarget, out SharedPullableComponent playerPullable))
            {
                ps.TryStopPull(playerPullable);
            }
        }
    }
}
