using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using System.Threading;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.Damage;
using Content.Shared.Dragon;
using Robust.Shared.GameStates;
using Robust.Shared.Random;

namespace Content.Server.Dragon
{
    public sealed partial class DragonSystem : GameRuleSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DragonComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DragonComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<DragonComponent, DragonDevourComplete>(OnDragonDevourComplete);
            SubscribeLocalEvent<DragonComponent, DragonDevourActionEvent>(OnDevourAction);
            SubscribeLocalEvent<DragonComponent, DragonSpawnActionEvent>(OnDragonSpawnAction);
            SubscribeLocalEvent<DragonComponent, DragonSpawnRiftActionEvent>(OnDragonRift);

            SubscribeLocalEvent<DragonComponent, DragonStructureDevourComplete>(OnDragonStructureDevourComplete);
            SubscribeLocalEvent<DragonComponent, DragonDevourCancelledEvent>(OnDragonDevourCancelled);
            SubscribeLocalEvent<DragonComponent, MobStateChangedEvent>(OnMobStateChanged);

            SubscribeLocalEvent<DragonRiftComponent, ComponentShutdown>(OnRiftShutdown);
            SubscribeLocalEvent<DragonRiftComponent, ComponentGetState>(OnRiftGetState);
            SubscribeLocalEvent<DragonRiftComponent, AnchorStateChangedEvent>(OnAnchorChange);

            SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRiftRoundEnd);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var comp in EntityQuery<DragonRiftComponent>())
            {
                if (comp.State != DragonRiftState.Finished && comp.Accumulator >= comp.MaxAccumulator)
                {
                    // TODO: When we get autocall you can buff if the rift finishes / 3 rifts are up
                    // for now they just keep 3 rifts up.

                    comp.Accumulator = comp.MaxAccumulator;
                    RemComp<DamageableComponent>(comp.Owner);
                    comp.State = DragonRiftState.Finished;
                    Dirty(comp);
                }
                else
                {
                    comp.Accumulator += frameTime;
                }

                comp.SpawnAccumulator += frameTime;

                if (comp.State < DragonRiftState.AlmostFinished && comp.SpawnAccumulator > comp.MaxAccumulator / 2f)
                {
                    comp.State = DragonRiftState.AlmostFinished;
                    Dirty(comp);
                    var location = Transform(comp.Owner).LocalPosition;

                    _chat.DispatchGlobalAnnouncement(Loc.GetString("carp-rift-warning", ("location", location)), playSound: false, colorOverride: Color.Red);
                    _audioSystem.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast());
                }

                if (comp.SpawnAccumulator > comp.SpawnCooldown)
                {
                    comp.SpawnAccumulator -= comp.SpawnCooldown;
                    Spawn(comp.SpawnPrototype, Transform(comp.Owner).MapPosition);
                    // TODO: When NPC refactor make it guard the rift.
                }
            }
        }

        #region Rift

        private void OnAnchorChange(EntityUid uid, DragonRiftComponent component, ref AnchorStateChangedEvent args)
        {
            if (!args.Anchored)
            {
                QueueDel(uid);
            }
        }

        private void OnRiftShutdown(EntityUid uid, DragonRiftComponent component, ComponentShutdown args)
        {
            if (TryComp<DragonComponent>(component.Dragon, out var dragon))
            {
                dragon.Rifts.Remove(uid);
            }
        }

        private void OnRiftGetState(EntityUid uid, DragonRiftComponent component, ref ComponentGetState args)
        {
            args.State = new DragonRiftComponentState()
            {
                State = component.State
            };
        }

        private void OnDragonRift(EntityUid uid, DragonComponent component, DragonSpawnRiftActionEvent args)
        {
            if (component.Rifts.Count >= 3)
            {
                _popupSystem.PopupEntity(Loc.GetString("carp-rift-max"), uid, Filter.Entities(uid));
                return;
            }

            if (component.Rifts.Count > 0 && TryComp<DragonRiftComponent>(component.Rifts[^1], out var rift) && rift.State != DragonRiftState.Finished)
            {
                _popupSystem.PopupEntity(Loc.GetString("carp-rift-duplicate"), uid, Filter.Entities(uid));
                return;
            }

            if (component.SpawnsLeft == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("dragon-spawn-action-popup-message-fail-no-eggs"), uid, Filter.Entities(uid));
                return;
            }

            // TODO: Make weightless movement floatier
            // TODO: Spawn dragon in space
            var xform = Transform(uid);

            // Have to be on a grid fam
            if (xform.GridUid == null)
            {
                return;
            }

            component.SpawnsLeft--;
            var carpUid = Spawn(component.RiftPrototype, xform.MapPosition);
            component.Rifts.Add(carpUid);
            Comp<DragonRiftComponent>(carpUid).Dragon = uid;
            _audioSystem.Play("/Audio/Weapons/Guns/Gunshots/rocket_launcher.ogg", Filter.Pvs(carpUid, entityManager: EntityManager), carpUid);
        }

        #endregion

        private void OnShutdown(EntityUid uid, DragonComponent component, ComponentShutdown args)
        {
            foreach (var rift in component.Rifts)
            {
                Del(rift);
            }
        }

        private void OnMobStateChanged(EntityUid uid, DragonComponent component, MobStateChangedEvent args)
        {
            //Empties the stomach upon death
            //TODO: Do this when the dragon gets butchered instead
            if (args.CurrentMobState == DamageState.Dead)
            {
                if (component.SoundDeath != null)
                    _audioSystem.PlayPvs(component.SoundDeath, uid, component.SoundDeath.Params);

                component.DragonStomach.EmptyContainer();
            }
        }

        private void OnDragonDevourCancelled(EntityUid uid, DragonComponent component, DragonDevourCancelledEvent args)
        {
            component.CancelToken = null;
        }

        private void OnDragonDevourComplete(EntityUid uid, DragonComponent component, DragonDevourComplete args)
        {
            component.CancelToken = null;
            var ichorInjection = new Solution(component.DevourChem, component.DevourHealRate);

            //Humanoid devours allow dragon to get eggs, corpses included
            if (EntityManager.HasComponent<HumanoidAppearanceComponent>(args.Target))
            {
                // Add a spawn for a consumed humanoid
                component.SpawnsLeft = Math.Min(component.SpawnsLeft + 1, component.MaxSpawns);
            }
            //Non-humanoid mobs can only heal dragon for half the normal amount, with no additional spawn tickets
            else
            {
                ichorInjection.ScaleSolution(0.5f);
            }

            _bloodstreamSystem.TryAddToChemicals(uid, ichorInjection);
            component.DragonStomach.Insert(args.Target);

            if (component.SoundDevour != null)
                _audioSystem.PlayPvs(component.SoundDevour, uid, component.SoundDevour.Params);
        }

        private void OnDragonStructureDevourComplete(EntityUid uid, DragonComponent component, DragonStructureDevourComplete args)
        {
            component.CancelToken = null;
            //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
            EntityManager.QueueDeleteEntity(args.Target);

            if (component.SoundDevour != null)
                _audioSystem.PlayPvs(component.SoundDevour, uid, component.SoundDevour.Params);
        }

        private void OnStartup(EntityUid uid, DragonComponent component, ComponentStartup args)
        {
            component.SpawnsLeft = Math.Min(component.SpawnsLeft, component.MaxSpawns);

            //Dragon doesn't actually chew, since he sends targets right into his stomach.
            //I did it mom, I added ERP content into upstream. Legally!
            component.DragonStomach = _containerSystem.EnsureContainer<Container>(uid, "dragon_stomach");

            if (component.DevourAction != null)
                _actionsSystem.AddAction(uid, component.DevourAction, null);

            if (component.SpawnAction != null)
                _actionsSystem.AddAction(uid, component.SpawnAction, null);

            if (component.SpawnRiftAction != null)
                _actionsSystem.AddAction(uid, component.SpawnRiftAction, null);

            if (component.SoundRoar != null)
                _audioSystem.Play(component.SoundRoar, Filter.Pvs(uid, 4f, EntityManager), uid, component.SoundRoar.Params);
        }

        /// <summary>
        /// The devour action
        /// </summary>
        private void OnDevourAction(EntityUid uid, DragonComponent component, DragonDevourActionEvent args)
        {
            if (component.CancelToken != null ||
                args.Handled ||
                component.DevourWhitelist?.IsValid(args.Target, EntityManager) != true)
            {
                return;
            }


            args.Handled = true;
            var target = args.Target;

            // Structure and mob devours handled differently.
            if (EntityManager.TryGetComponent(target, out MobStateComponent? targetState))
            {
                switch (targetState.CurrentState)
                {
                    case DamageState.Critical:
                    case DamageState.Dead:
                        component.CancelToken = new CancellationTokenSource();

                        _doAfterSystem.DoAfter(new DoAfterEventArgs(uid, component.DevourTime, component.CancelToken.Token, target)
                        {
                            UserFinishedEvent = new DragonDevourComplete(uid, target),
                            UserCancelledEvent = new DragonDevourCancelledEvent(),
                            BreakOnTargetMove = true,
                            BreakOnUserMove = true,
                            BreakOnStun = true,
                        });
                        break;
                    default:
                        _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-alive"), uid, Filter.Entities(uid));
                        break;
                }

                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-structure"), uid, Filter.Entities(uid));

            if (component.SoundStructureDevour != null)
                _audioSystem.PlayPvs(component.SoundStructureDevour, uid, component.SoundStructureDevour.Params);

            component.CancelToken = new CancellationTokenSource();

            _doAfterSystem.DoAfter(new DoAfterEventArgs(uid, component.StructureDevourTime, component.CancelToken.Token, target)
            {
                UserFinishedEvent = new DragonStructureDevourComplete(uid, target),
                UserCancelledEvent = new DragonDevourCancelledEvent(),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
            });
        }

        private void OnDragonSpawnAction(EntityUid dragonuid, DragonComponent component, DragonSpawnActionEvent args)
        {
            if (component.SpawnPrototype == null)
                return;

            // If dragon has spawns then add one.
            if (component.SpawnsLeft > 0)
            {
                Spawn(component.SpawnPrototype, Transform(dragonuid).MapPosition);
                component.SpawnsLeft--;
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("dragon-spawn-action-popup-message-fail-no-eggs"), dragonuid, Filter.Entities(dragonuid));
        }

        private sealed class DragonDevourComplete : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid Target { get; }

            public DragonDevourComplete(EntityUid user, EntityUid target)
            {
                User = user;
                Target = target;
            }
        }

        private sealed class DragonStructureDevourComplete : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid Target { get; }

            public DragonStructureDevourComplete(EntityUid user, EntityUid target)
            {
                 User = user;
                 Target = target;
            }
        }

        private sealed class DragonDevourCancelledEvent : EntityEventArgs {}
    }
}
