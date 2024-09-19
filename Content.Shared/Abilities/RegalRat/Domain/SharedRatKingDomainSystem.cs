using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.RatKing;

public abstract class SharedRatKingDomainSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RatKingDomainComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RatKingDomainComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, RatKingDomainComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        _action.AddAction(uid, ref component.ActionDomainEntity, component.ActionDomain, component: comp);
    }

    private void OnShutdown(EntityUid uid, RatKingDomainComponent component, ComponentShutdown args)
    {
        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        _action.RemoveAction(uid, component.ActionDomainEntity, comp);
    }
}
