using Content.Server.Chat.Systems;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Chat;
using Content.Shared.Salvage;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    /*
     * Handles actively running a salvage expedition.
     */

    private void InitializeRunner()
    {
        SubscribeLocalEvent<FTLRequestEvent>(OnFTLRequest);
        SubscribeLocalEvent<FTLStartedEvent>(OnFTLStarted);
        SubscribeLocalEvent<FTLCompletedEvent>(OnFTLCompleted);
    }

    /// <summary>
    /// Announces status updates to salvage crewmembers on the state of the expedition.
    /// </summary>
    private void Announce(EntityUid mapUid, string text)
    {
        var mapId = Comp<MapComponent>(mapUid).MapId;

        // I love TComms and chat!!!
        _chat.ChatMessageToManyFiltered(
            Filter.BroadcastMap(mapId),
            ChatChannel.Radio,
            text,
            text,
            _mapManager.GetMapEntityId(mapId),
            false,
            true,
            null);
    }

    private void OnFTLRequest(ref FTLRequestEvent ev)
    {
        if (!HasComp<SalvageExpeditionComponent>(ev.MapUid) ||
            !TryComp<FTLDestinationComponent>(ev.MapUid, out var dest))
        {
            return;
        }

        // Only one shuttle can occupy an expedition.
        dest.Enabled = false;
        _shuttleConsoles.RefreshShuttleConsoles();
    }

    private void OnFTLCompleted(ref FTLCompletedEvent args)
    {
        if (!TryComp<SalvageExpeditionComponent>(args.MapUid, out var component))
            return;

        // Someone FTLd there so start announcement
        if (component.Stage != ExpeditionStage.Added)
            return;

        Announce(args.MapUid, Loc.GetString("salvage-expedition-announcement-countdown-minutes", ("duration", (component.EndTime - _timing.CurTime).Minutes)));

        if (component.DungeonLocation != Vector2.Zero)
            Announce(args.MapUid, Loc.GetString("salvage-expedition-announcement-dungeon", ("direction", component.DungeonLocation.GetDir())));

        component.Stage = ExpeditionStage.Running;
    }

    private void OnFTLStarted(ref FTLStartedEvent ev)
    {
        // Started a mining mission so work out exempt entities
        if (TryComp<SalvageMiningExpeditionComponent>(
                _mapManager.GetMapEntityId(ev.TargetCoordinates.ToMap(EntityManager, _transform).MapId),
                out var mining))
        {
            var ents = new List<EntityUid>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            MiningTax(ents, ev.Entity, mining, xformQuery);
            mining.ExemptEntities = ents;
        }

        if (!TryComp<SalvageExpeditionComponent>(ev.FromMapUid, out var expedition) ||
            !TryComp<SalvageExpeditionDataComponent>(expedition.Station, out var station))
        {
            return;
        }

        // Check if any shuttles remain.
        var query = EntityQueryEnumerator<ShuttleComponent, TransformComponent>();

        while (query.MoveNext(out _, out var xform))
        {
            if (xform.MapUid == ev.FromMapUid)
                return;
        }

        // Last shuttle has left so finish the mission.
        FinishExpedition(station, expedition, ev.Entity);
    }

    // Runs the expedition
    private void UpdateRunner()
    {
        // Generic missions
        var query = EntityQueryEnumerator<SalvageExpeditionComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Completed)
                continue;

            var remaining = comp.EndTime - _timing.CurTime;

            if (comp.Stage < ExpeditionStage.FinalCountdown && remaining < TimeSpan.FromSeconds(30))
            {
                comp.Stage = ExpeditionStage.FinalCountdown;
                Announce(uid, Loc.GetString("salvage-expedition-announcement-countdown-seconds", ("duration", (comp.EndTime - _timing.CurTime).Minutes)));
            }
            // TODO: Play song.
            else if (comp.Stage < ExpeditionStage.Countdown && remaining < TimeSpan.FromMinutes(2))
            {
                comp.Stage = ExpeditionStage.Countdown;
                Announce(uid, Loc.GetString("salvage-expedition-announcement-countdown-minutes", ("duration", (comp.EndTime - _timing.CurTime).Minutes)));
            }
        }

        // Mining missions: NOOP

        // Structure missions
        var structureQuery = EntityQueryEnumerator<SalvageStructureExpeditionComponent, SalvageExpeditionComponent>();

        while (structureQuery.MoveNext(out var uid, out var structure, out var comp))
        {
            if (comp.Completed)
                continue;

            var structureAnnounce = false;

            for (var i = 0; i < structure.Structures.Count; i++)
            {
                var objective = structure.Structures[i];

                if (Deleted(objective))
                {
                    structure.Structures.RemoveSwap(i);
                    structureAnnounce = true;
                }
            }

            if (structureAnnounce)
            {
                Announce(uid, Loc.GetString("salvage-expedition-structures-remaining", ("count", structure.Structures.Count)));
            }
        }
    }
}
