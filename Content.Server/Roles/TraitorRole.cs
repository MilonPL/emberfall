using Content.Server.Chat.Managers;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Robust.Shared.Audio;

namespace Content.Server.Roles;

public sealed class TraitorRole : AntagonistRole
{
    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    protected override SoundSpecifier? AntagonistAlert { get; } =
        new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");

    public TraitorRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }

    public void GreetTraitor(string[] codewords, Note[] code)
    {
        if (Mind.TryGetSession(out var session))
        {
            var chatMgr = IoCManager.Resolve<IChatManager>();
            chatMgr.DispatchServerMessage(session, Loc.GetString("traitor-role-greeting"));
            chatMgr.DispatchServerMessage(session,
                Loc.GetString("traitor-role-codewords", ("codewords", string.Join(", ", codewords))));
            chatMgr.DispatchServerMessage(session,
                Loc.GetString("traitor-role-uplink-code", ("code", string.Join("", code))));
        }
    }
}
