using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Systems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SpeakOperator : HTNOperator
{
    private ChatSystem _chat = default!;

    [DataField(required: true)]
    public string Speech = string.Empty;

    [DataField]
    public string PlanSpeech = string.Empty;

    /// <summary>
    /// Whether to hide message from chat window and logs.
    /// </summary>
    [DataField]
    public bool Hidden;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);

        _chat = sysManager.GetEntitySystem<ChatSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        if (PlanSpeech != string.Empty)
            {
                var speaker = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
                _chat.TrySendInGameICMessage(speaker, PlanSpeech, InGameICChatType.Speak, hideChat: Hidden, hideLog: Hidden);
            }
        return (true, null);
    }


    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var speaker = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        _chat.TrySendInGameICMessage(speaker, Loc.GetString(Speech), InGameICChatType.Speak, hideChat: Hidden, hideLog: Hidden);

        return base.Update(blackboard, frameTime);
    }
}
