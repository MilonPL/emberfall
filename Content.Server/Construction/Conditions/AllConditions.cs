using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Conditions;

[UsedImplicitly]
[DataDefinition]
public class AllConditions : IGraphCondition
{
    [DataField("conditions")]
    public IGraphCondition[] Conditions { get; } = Array.Empty<IGraphCondition>();

    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        foreach (var condition in Conditions)
        {
            if (!condition.Condition(uid, entityManager))
                return false;
        }

        return true;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var ret = false;

        foreach (var condition in Conditions)
        {
            ret |= condition.DoExamine(args);
        }

        return ret;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        foreach (var condition in Conditions)
        {
            foreach (var entry in condition.GenerateGuideEntry())
            {
                yield return entry;
            }
        }
    }
}