using Content.Shared.Whitelist;

namespace Content.Shared.Lock;

public sealed class LockingWhitelistSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LockingWhitelistComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
    }

    private void OnLockToggleAttempt(Entity<LockingWhitelistComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (!_whitelist.CheckBoth(args.Target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            args.Cancelled = true;
    }
}
