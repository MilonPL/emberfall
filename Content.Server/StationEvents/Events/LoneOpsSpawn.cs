using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Content.Server.GameTicking;
using Robust.Shared.Prototypes;
using Content.Server.GameTicking.Rules;

namespace Content.Server.StationEvents.Events;

public sealed class LoneOpsSpawn : StationEventSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly NukeopsRuleSystem _nukeopsRuleSystem = default!;

    public override string Prototype => "LoneOps";
    public const string LoneOpsShuttlePath = "Maps/Shuttles/striker.yml";

    public override void Started()
    {
        base.Started();

        var shuttleMap = _mapManager.CreateMap();
        var options = new MapLoadOptions()
        {
            LoadMap = true,
        };

        _map.TryLoad(shuttleMap, LoneOpsShuttlePath, out var grids, options);

        _prototypeManager.TryIndex<GameRulePrototype>("Nukeops", out var ruleProto);

        if (ruleProto == null)
            return;

        _nukeopsRuleSystem.OnLoneOpsSpawn();
        _gameTicker.StartGameRule(ruleProto);
    }
}

