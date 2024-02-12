using Content.Server.Chat;
using Content.Server.Chat.V2;

namespace Content.Server.Chat.Systems;

public sealed class AnnounceOnSpawnSystem : EntitySystem
{
    [Dependency] private readonly ServerAnnouncementSystem _announce = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnnounceOnSpawnComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, AnnounceOnSpawnComponent comp, MapInitEvent args)
    {
        var message = Loc.GetString(comp.Message);
        var sender = comp.Sender != null ? Loc.GetString(comp.Sender) : "Central Command";
        _announce.DispatchGlobalAnnouncement(message, sender, playSound: true, comp.Sound, comp.Color);
    }
}
