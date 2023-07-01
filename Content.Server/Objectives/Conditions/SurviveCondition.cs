using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions;

[DataDefinition]
public sealed class SurviveCondition : IObjectiveCondition
{
    private Mind.Mind? _mind;

    public IObjectiveCondition GetAssigned(Mind.Mind mind)
    {
        return new SurviveCondition {_mind = mind};
    }

    public string Title => Loc.GetString("objective-condition-survive-title");

    public string Description => Loc.GetString("objective-condition-survive-description");

    public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResPath("Clothing/Mask/ninja.rsi"), "icon");

    public float Difficulty => 0.5f;

    public float Progress
    {
        get
        {
            if (_mind == null)
                return 0f;

            var entMan = IoCManager.Resolve<IEntityManager>();
            var mindSystem = entMan.System<MindSystem>();
            return mindSystem.IsCharacterDeadIc(_mind) ? 0f : 1f;
        }
    }

    public bool Equals(IObjectiveCondition? other)
    {
        return other is SurviveCondition condition && Equals(_mind, condition._mind);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SurviveCondition) obj);
    }

    public override int GetHashCode()
    {
        return (_mind != null ? _mind.GetHashCode() : 0);
    }
}
