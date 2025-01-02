using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._Emberfall.Engraving;

public abstract class SharedEngravableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EngravableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<EngravableComponent> ent, ref ExaminedEvent args)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString(ent.Comp.EngravedMessage == string.Empty
            ? ent.Comp.NoEngravingText
            : ent.Comp.HasEngravingText));

        if (ent.Comp.EngravedMessage != string.Empty)
            msg.AddMarkupPermissive(Loc.GetString(ent.Comp.EngravedMessage));

        args.PushMessage(msg, 1);
    }
}
