// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// This Source Code Form is "Incompatible With Secondary
// Licenses", as defined by the Mozilla Public License, v. 2.0.

using System.Linq;
using Content.Server.Shuttles.Events;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Emberfall.Planetside.Components;
using Content.Shared._Emberfall.Planetside.Systems;
using Content.Shared.Shuttles.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;

namespace Content.Server._Emberfall.Planetside.Systems;

public sealed class SpaceElevatorSystem : SharedSpaceElevatorSystem
{
    [Dependency] private readonly SpaceElevatorConsoleSystem _console = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceElevatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SpaceElevatorComponent, FTLStartedEvent>(OnFTLStarted);
        SubscribeLocalEvent<SpaceElevatorComponent, FTLCompletedEvent>(OnFTLCompleted);

        SubscribeLocalEvent<StationGridAddedEvent>(OnStationGridAdded);
    }

    private void OnMapInit(Entity<SpaceElevatorComponent> ent, ref MapInitEvent args)
    {
        // Find all valid FTL destinations this elevator can reach
        var query = EntityQueryEnumerator<FTLDestinationComponent, MapComponent>();
        while (query.MoveNext(out var mapUid, out var dest, out var map))
        {
            if (!dest.Enabled || _whitelist.IsWhitelistFailOrNull(dest.Whitelist, ent))
                continue;

            ent.Comp.Destinations.Add(new ElevatorDestination
            {
                Name = Name(mapUid),
                Map = map.MapId,
            });
        }
    }

    private void OnFTLStarted(Entity<SpaceElevatorComponent> ent, ref FTLStartedEvent args)
    {
        _console.UpdateConsolesUsing(ent);
    }

    private void OnFTLCompleted(Entity<SpaceElevatorComponent> ent, ref FTLCompletedEvent args)
    {
        _console.UpdateConsolesUsing(ent);
    }

    private void OnStationGridAdded(StationGridAddedEvent args)
    {
        var uid = args.GridId;
        if (!TryComp<SpaceElevatorComponent>(uid, out var comp))
            return;

        // only add the destination once
        if (comp.Station != null)
            return;

        if (_station.GetOwningStation(uid) is not { } station || !TryComp<StationDataComponent>(station, out var data))
            return;

        // add the source station as a destination
        comp.Station = station;
        comp.Destinations.Add(new ElevatorDestination
        {
            Name = Name(station),
            Map = Transform(data.Grids.First()).MapID,
        });
    }
}
