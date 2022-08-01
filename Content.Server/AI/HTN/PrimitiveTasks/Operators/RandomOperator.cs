using System.Threading.Tasks;
using Robust.Shared.Random;

namespace Content.Server.AI.HTN.PrimitiveTasks;

public sealed class RandomOperator : HTNOperator
{
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Target blackboard key to set the value to. Doesn't need to exist beforehand.
    /// </summary>
    [DataField("targetKey", required: true)] public string TargetKey = string.Empty;

    /// <summary>
    ///  Minimum idle time.
    /// </summary>
    [DataField("minKey", required: true)] public string MinKey = string.Empty;

    /// <summary>
    ///  Maximum idle time.
    /// </summary>
    [DataField("maxKey", required: true)] public string MaxKey = string.Empty;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard)
    {
        return (true, new Dictionary<string, object>()
        {
            {
                TargetKey,
                _random.NextFloat(blackboard.GetValueOrDefault<float>(MinKey),
                    blackboard.GetValueOrDefault<float>(MaxKey))
            }
        });
    }
}
