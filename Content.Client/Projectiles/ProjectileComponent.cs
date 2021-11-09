using Content.Shared.Projectiles;
using Robust.Shared.GameObjects;

namespace Content.Client.Projectiles;

[RegisterComponent]
[ComponentReference(typeof(SharedProjectileComponent))]
public class ProjectileComponent : SharedProjectileComponent
{
    public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
    {
        if (curState is ProjectileComponentState compState)
        {
            Shooter = compState.Shooter;
            IgnoreShooter = compState.IgnoreShooter;
        }
    }
}