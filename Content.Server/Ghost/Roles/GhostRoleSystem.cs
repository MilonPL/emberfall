using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Ghost.Roles.UI;
using Content.Server.Mind.Commands;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Follower;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Roles;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Players;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Ghost.Roles
{
    [UsedImplicitly]
    public sealed class GhostRoleSystem : EntitySystem
    {
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly FollowerSystem _followerSystem = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        private uint _nextRoleIdentifier;
        private bool _needsUpdateGhostRoleCount = true;

        private readonly Dictionary<uint, Entity<GhostRoleComponent>> _ghostRoles = new();
        private readonly Dictionary<uint, Entity<GhostRoleRaffleComponent>> _ghostRoleRaffles = new();

        private readonly Dictionary<ICommonSession, GhostRolesEui> _openUis = new();
        private readonly Dictionary<ICommonSession, MakeGhostRoleEui> _openMakeGhostRoleUis = new();

        [ViewVariables]
        public IReadOnlyCollection<Entity<GhostRoleComponent>> GhostRoles => _ghostRoles.Values;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindRemovedMessage>(OnMindRemoved);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<GhostRoleComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<GhostRoleComponent, ComponentStartup>(OnRoleStartup);
            SubscribeLocalEvent<GhostRoleComponent, ComponentShutdown>(OnRoleShutdown);
            SubscribeLocalEvent<GhostRoleComponent, EntityPausedEvent>(OnPaused);
            SubscribeLocalEvent<GhostRoleComponent, EntityUnpausedEvent>(OnUnpaused);
            SubscribeLocalEvent<GhostRoleRaffleComponent, ComponentInit>(OnRaffleInit);
            SubscribeLocalEvent<GhostRoleRaffleComponent, ComponentShutdown>(OnRaffleShutdown);
            SubscribeLocalEvent<GhostRoleMobSpawnerComponent, TakeGhostRoleEvent>(OnSpawnerTakeRole);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, TakeGhostRoleEvent>(OnTakeoverTakeRole);
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        }

        private void OnMobStateChanged(Entity<GhostTakeoverAvailableComponent> component, ref MobStateChangedEvent args)
        {
            if (!TryComp(component, out GhostRoleComponent? ghostRole))
                return;

            switch (args.NewMobState)
            {
                case MobState.Alive:
                {
                    if (!ghostRole.Taken)
                        RegisterGhostRole((component, ghostRole));
                    break;
                }
                case MobState.Critical:
                case MobState.Dead:
                    UnregisterGhostRole((component, ghostRole));
                    break;
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= PlayerStatusChanged;
        }

        private uint GetNextRoleIdentifier()
        {
            return unchecked(_nextRoleIdentifier++);
        }

        public void OpenEui(ICommonSession session)
        {
            if (session.AttachedEntity is not {Valid: true} attached ||
                !EntityManager.HasComponent<GhostComponent>(attached))
                return;

            if(_openUis.ContainsKey(session))
                CloseEui(session);

            var eui = _openUis[session] = new GhostRolesEui();
            _euiManager.OpenEui(eui, session);
            eui.StateDirty();
        }

        public void OpenMakeGhostRoleEui(ICommonSession session, EntityUid uid)
        {
            if (session.AttachedEntity == null)
                return;

            if (_openMakeGhostRoleUis.ContainsKey(session))
                CloseEui(session);

            var eui = _openMakeGhostRoleUis[session] = new MakeGhostRoleEui(EntityManager, GetNetEntity(uid));
            _euiManager.OpenEui(eui, session);
            eui.StateDirty();
        }

        public void CloseEui(ICommonSession session)
        {
            if (!_openUis.ContainsKey(session))
                return;

            _openUis.Remove(session, out var eui);

            eui?.Close();
        }

        public void CloseMakeGhostRoleEui(ICommonSession session)
        {
            if (_openMakeGhostRoleUis.Remove(session, out var eui))
            {
                eui.Close();
            }
        }

        public void UpdateAllEui()
        {
            foreach (var eui in _openUis.Values)
            {
                eui.StateDirty();
            }
            // Note that this, like the EUIs, is deferred.
            // This is for roughly the same reasons, too:
            // Someone might spawn a ton of ghost roles at once.
            _needsUpdateGhostRoleCount = true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateGhostRoleCount();
            UpdateRaffles(frameTime);
        }

        private void UpdateGhostRoleCount()
        {
            if (!_needsUpdateGhostRoleCount)
                return;

            _needsUpdateGhostRoleCount = false;
            // TODO: this used to be cheap but now it's more expensive...
            var response = new GhostUpdateGhostRoleCountEvent(GetGhostRolesInfo(null).Length);
            foreach (var player in _playerManager.Sessions)
            {
                RaiseNetworkEvent(response, player.Channel);
            }
        }

        private void UpdateRaffles(float frameTime)
        {
            var query = EntityQueryEnumerator<GhostRoleRaffleComponent, MetaDataComponent>();
            while (query.MoveNext(out var entityUid, out var raffle, out var meta))
            {
                if (meta.EntityPaused)
                    continue;

                // if all participants leave/were removed from the raffle, the raffle is canceled.
                if (raffle.CurrentMembers.Count == 0)
                {
                    RemoveRaffleAndUpdateEui(entityUid, raffle);
                    continue;
                }

                raffle.Countdown -= frameTime;
                if (raffle.Countdown > 0)
                    continue;

                // the raffle is over! find someone to take over the ghost role
                var candidates = raffle.CurrentMembers.ToList();
                _random.Shuffle(candidates); // shuffle the list so we can pick a lucky winner!

                // try to find someone who can take the role
                foreach (var candidate in candidates)
                {
                    // TODO: the following two checks are kind of redundant since they should already be removed
                    //           from the raffle
                    // can't win if you are disconnected (although you shouldn't be a candidate anyway)
                    if (candidate.Status != SessionStatus.InGame)
                        continue;

                    // can't win if you are no longer a ghost (e.g. if you returned to your body)
                    if (candidate.AttachedEntity == null || !HasComp<GhostComponent>(candidate.AttachedEntity))
                        continue;

                    if (Takeover(candidate, raffle.Identifier))
                    {
                        // takeover successful, we have a winner! remove the winner from other raffles they might be in
                        LeaveAllRaffles(candidate);
                        break;
                    }
                }

                // raffle over, either because someone won, or everyone unregistered from the raffle
                RemoveRaffleAndUpdateEui(entityUid, raffle);
            }
        }

        private void RemoveRaffleAndUpdateEui(EntityUid entityUid, GhostRoleRaffleComponent raffle)
        {
            _ghostRoleRaffles.Remove(raffle.Identifier);
            RemComp(entityUid, raffle);
            UpdateAllEui();
        }

        private void PlayerStatusChanged(object? blah, SessionStatusEventArgs args)
        {
            if (args.NewStatus == SessionStatus.InGame)
            {
                var response = new GhostUpdateGhostRoleCountEvent(_ghostRoles.Count);
                RaiseNetworkEvent(response, args.Session.Channel);
            }
            else
            {
                // people who disconnect are removed from ghost role raffles
                LeaveAllRaffles(args.Session);
            }
        }

        public void RegisterGhostRole(Entity<GhostRoleComponent> role)
        {
            if (_ghostRoles.ContainsValue(role))
                return;

            _ghostRoles[role.Comp.Identifier = GetNextRoleIdentifier()] = role;
            UpdateAllEui();
        }

        public void UnregisterGhostRole(Entity<GhostRoleComponent> role)
        {
            var comp = role.Comp;
            if (!_ghostRoles.ContainsKey(comp.Identifier) || _ghostRoles[comp.Identifier] != role)
                return;

            _ghostRoles.Remove(comp.Identifier);
            _ghostRoleRaffles.Remove(comp.Identifier); // remove raffle too if one is running
            UpdateAllEui();
        }

        // probably fine to be init because it's never added during entity initialization, but much later
        private void OnRaffleInit(Entity<GhostRoleRaffleComponent> ent, ref ComponentInit args)
        {
            if (!TryComp(ent, out GhostRoleComponent? ghostRole))
            {
                // can't have a raffle for a ghost role that doesn't exist
                RemComp<GhostRoleRaffleComponent>(ent);
                return;
            }

            var raffle = ent.Comp;
            raffle.Identifier = ghostRole.Identifier;
            raffle.Countdown = ghostRole.RaffleInitialDuration;
            raffle.CumulativeTime = ghostRole.RaffleInitialDuration;
        }

        private void OnRaffleShutdown(Entity<GhostRoleRaffleComponent> ent, ref ComponentShutdown args)
        {
            _ghostRoleRaffles.Remove(ent.Comp.Identifier);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="player"></param>
        /// <param name="identifier"></param>
        public void JoinRaffle(ICommonSession player, uint identifier)
        {
            if (!_ghostRoles.TryGetValue(identifier, out var roleEnt))
                return;

            var role = roleEnt.Comp;
            // get raffle or create a new one if it doesn't exist
            var raffle = _ghostRoleRaffles.TryGetValue(identifier, out var raffleEnt)
                ? raffleEnt.Comp
                : EnsureComp<GhostRoleRaffleComponent>(roleEnt.Owner);

            _ghostRoleRaffles.TryAdd(identifier, (roleEnt.Owner, raffle));

            if (!raffle.CurrentMembers.Add(player))
            {
                Log.Warning($"{player.Name} tried to join raffle for ghost role {identifier} but they are already in the raffle");
                return;
            }

            // if this is the first time the player joins this raffle, and the player wasn't the starter of the raffle:
            // extend the countdown, but only if doing so will not make the raffle take longer than the maximum
            // duration
            if (raffle.AllMembers.Add(player) && raffle.AllMembers.Count > 1
                && raffle.CumulativeTime + role.RaffleJoinExtendsDurationBy <= role.RaffleMaxDuration)
            {
                    raffle.Countdown += role.RaffleJoinExtendsDurationBy;
                    raffle.CumulativeTime += role.RaffleJoinExtendsDurationBy;
            }

            UpdateAllEui();
        }

        public void LeaveRaffle(ICommonSession player, uint identifier)
        {
            if (!_ghostRoleRaffles.TryGetValue(identifier, out var raffleEnt))
                return;

            if (raffleEnt.Comp.CurrentMembers.Remove(player))
            {
                UpdateAllEui();
            }
            else
            {
                Log.Warning($"{player.Name} tried to leave raffle for ghost role {identifier} but they are not in the raffle");
            }

            // (raffle ending because all players left is handled in update())
        }

        public void LeaveAllRaffles(ICommonSession player)
        {
            var shouldUpdateEui = false;

            foreach (var raffleEnt in _ghostRoleRaffles.Values)
            {
                shouldUpdateEui |= raffleEnt.Comp.CurrentMembers.Remove(player);
            }

            if (shouldUpdateEui)
                UpdateAllEui();
        }

        /// <summary>
        /// Request a ghost role. If it's a raffled role starts or joins a raffle, otherwise the player immediately
        /// takes over the ghost role if possible.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="identifier">ID of the ghost role.</param>
        public void Request(ICommonSession player, uint identifier)
        {
            if (!_ghostRoles.TryGetValue(identifier, out var roleEnt))
                return;

            if (roleEnt.Comp.Raffle)
            {
                JoinRaffle(player, identifier);
            }
            else
            {
                Takeover(player, identifier);
            }
        }

        public bool Takeover(ICommonSession player, uint identifier)
        {
            if (!_ghostRoles.TryGetValue(identifier, out var role))
                return false;

            var ev = new TakeGhostRoleEvent(player);
            RaiseLocalEvent(role, ref ev);

            if (!ev.TookRole)
                return false;

            if (player.AttachedEntity != null)
                _adminLogger.Add(LogType.GhostRoleTaken, LogImpact.Low, $"{player:player} took the {role.Comp.RoleName:roleName} ghost role {ToPrettyString(player.AttachedEntity.Value):entity}");

            CloseEui(player);
            return true;
        }

        public void Follow(ICommonSession player, uint identifier)
        {
            if (!_ghostRoles.TryGetValue(identifier, out var role))
                return;

            if (player.AttachedEntity == null)
                return;

            _followerSystem.StartFollowingEntity(player.AttachedEntity.Value, role);
        }

        public void GhostRoleInternalCreateMindAndTransfer(ICommonSession player, EntityUid roleUid, EntityUid mob, GhostRoleComponent? role = null)
        {
            if (!Resolve(roleUid, ref role))
                return;

            DebugTools.AssertNotNull(player.ContentData());

            var newMind = _mindSystem.CreateMind(player.UserId,
                EntityManager.GetComponent<MetaDataComponent>(mob).EntityName);
            _roleSystem.MindAddRole(newMind, new GhostRoleMarkerRoleComponent { Name = role.RoleName });

            _mindSystem.SetUserId(newMind, player.UserId);
            _mindSystem.TransferTo(newMind, mob);
        }

        public GhostRoleInfo[] GetGhostRolesInfo(ICommonSession? player)
        {
            var roles = new List<GhostRoleInfo>();
            var metaQuery = GetEntityQuery<MetaDataComponent>();

            foreach (var (id, (uid, role)) in _ghostRoles)
            {
                if (metaQuery.GetComponent(uid).EntityPaused)
                    continue;


                var kind = GhostRoleKind.FirstComeFirstServe;
                GhostRoleRaffleComponent? raffle = null;

                if (role.Raffle)
                {
                    kind = GhostRoleKind.RaffleReady;

                    if (_ghostRoleRaffles.TryGetValue(id, out var raffleEnt))
                    {
                        kind = GhostRoleKind.RaffleInProgress;
                        raffle = raffleEnt.Comp;

                        if (player is not null && raffle.CurrentMembers.Contains(player))
                            kind = GhostRoleKind.RaffleJoined;
                    }
                }

                var rafflePlayerCount = (uint?) raffle?.CurrentMembers.Count ?? 0;
                var raffleEndTime = raffle is not null
                    ? _timing.CurTime.Add(TimeSpan.FromSeconds(raffle.Countdown))
                    : TimeSpan.MinValue;


                roles.Add(new GhostRoleInfo
                {
                    Identifier = id,
                    Name = role.RoleName,
                    Description = role.RoleDescription,
                    Rules = role.RoleRules,
                    Requirements = role.Requirements,
                    Kind = kind,
                    RafflePlayerCount = rafflePlayerCount,
                    RaffleEndTime = raffleEndTime
                });
            }

            return roles.ToArray();
        }

        private void OnPlayerAttached(PlayerAttachedEvent message)
        {
            // Close the session of any player that has a ghost roles window open and isn't a ghost anymore.
            if (!_openUis.ContainsKey(message.Player))
                return;

            if (HasComp<GhostComponent>(message.Entity))
                return;

            // The player is not a ghost (anymore), so they should not be in any raffles. Remove them.
            // This ensures player doesn't win a raffle after returning to their (revived) body and getting forced into
            // a ghost role.
            LeaveAllRaffles(message.Player);
            CloseEui(message.Player);
        }

        private void OnMindAdded(EntityUid uid, GhostTakeoverAvailableComponent component, MindAddedMessage args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole))
                return;

            ghostRole.Taken = true;
            UnregisterGhostRole((uid, ghostRole));
        }

        private void OnMindRemoved(EntityUid uid, GhostTakeoverAvailableComponent component, MindRemovedMessage args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole))
                return;

            // Avoid re-registering it for duplicate entries and potential exceptions.
            if (!ghostRole.ReregisterOnGhost || component.LifeStage > ComponentLifeStage.Running)
                return;

            ghostRole.Taken = false;
            RegisterGhostRole((uid, ghostRole));
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            foreach (var session in _openUis.Keys)
            {
                CloseEui(session);
            }

            _openUis.Clear();
            _ghostRoles.Clear();
            _ghostRoleRaffles.Clear();
            _nextRoleIdentifier = 0;
        }

        private void OnPaused(EntityUid uid, GhostRoleComponent component, ref EntityPausedEvent args)
        {
            if (HasComp<ActorComponent>(uid))
                return;

            UpdateAllEui();
        }

        private void OnUnpaused(EntityUid uid, GhostRoleComponent component, ref EntityUnpausedEvent args)
        {
            if (HasComp<ActorComponent>(uid))
                return;

            UpdateAllEui();
        }

        private void OnMapInit(Entity<GhostRoleComponent> ent, ref MapInitEvent args)
        {
            if (ent.Comp.Probability < 1f && !_random.Prob(ent.Comp.Probability))
                RemCompDeferred<GhostRoleComponent>(ent);
        }

        private void OnRoleStartup(Entity<GhostRoleComponent> ent, ref ComponentStartup args)
        {
            RegisterGhostRole(ent);
        }

        private void OnRoleShutdown(Entity<GhostRoleComponent> role, ref ComponentShutdown args)
        {
            UnregisterGhostRole(role);
        }

        private void OnSpawnerTakeRole(EntityUid uid, GhostRoleMobSpawnerComponent component, ref TakeGhostRoleEvent args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole) ||
                !CanTakeGhost(uid, ghostRole))
            {
                args.TookRole = false;
                return;
            }

            if (string.IsNullOrEmpty(component.Prototype))
                throw new NullReferenceException("Prototype string cannot be null or empty!");

            var mob = Spawn(component.Prototype, Transform(uid).Coordinates);
            _transform.AttachToGridOrMap(mob);

            var spawnedEvent = new GhostRoleSpawnerUsedEvent(uid, mob);
            RaiseLocalEvent(mob, spawnedEvent);

            if (ghostRole.MakeSentient)
                MakeSentientCommand.MakeSentient(mob, EntityManager, ghostRole.AllowMovement, ghostRole.AllowSpeech);

            EnsureComp<MindContainerComponent>(mob);

            GhostRoleInternalCreateMindAndTransfer(args.Player, uid, mob, ghostRole);

            if (++component.CurrentTakeovers < component.AvailableTakeovers)
            {
                args.TookRole = true;
                return;
            }

            ghostRole.Taken = true;

            if (component.DeleteOnSpawn)
                QueueDel(uid);

            args.TookRole = true;
        }

        private bool CanTakeGhost(EntityUid uid, GhostRoleComponent? component = null)
        {
            return Resolve(uid, ref component, false) &&
                   !component.Taken &&
                   !MetaData(uid).EntityPaused;
        }

        private void OnTakeoverTakeRole(EntityUid uid, GhostTakeoverAvailableComponent component, ref TakeGhostRoleEvent args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole) ||
                !CanTakeGhost(uid, ghostRole))
            {
                args.TookRole = false;
                return;
            }

            ghostRole.Taken = true;

            var mind = EnsureComp<MindContainerComponent>(uid);

            if (mind.HasMind)
            {
                args.TookRole = false;
                return;
            }

            if (ghostRole.MakeSentient)
                MakeSentientCommand.MakeSentient(uid, EntityManager, ghostRole.AllowMovement, ghostRole.AllowSpeech);

            GhostRoleInternalCreateMindAndTransfer(args.Player, uid, uid, ghostRole);
            UnregisterGhostRole((uid, ghostRole));

            args.TookRole = true;
        }
    }

    [AnyCommand]
    public sealed class GhostRoles : IConsoleCommand
    {
        public string Command => "ghostroles";
        public string Description => "Opens the ghost role request window.";
        public string Help => $"{Command}";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if(shell.Player != null)
                EntitySystem.Get<GhostRoleSystem>().OpenEui(shell.Player);
            else
                shell.WriteLine("You can only open the ghost roles UI on a client.");
        }
    }
}
