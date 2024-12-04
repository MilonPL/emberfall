using Content.Shared.Actions;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Spider;

/// <summary>
/// Gives entities (probably spiders) an action on init.  Spawning handled by <see cref="ServerSpiderSystem"/>..
/// </summary>
public abstract class SharedSpiderSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, SpiderComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ActionEntity, component.SpawnWebAction, uid);
    }
}
