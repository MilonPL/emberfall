using System.Linq;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Server.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.CombatMode;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Lock;
using Content.Shared.Placeable;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using static Content.Shared.Storage.StorageComponent;

namespace Content.Server.Storage.EntitySystems
{
    public sealed partial class StorageSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ISharedAdminManager _admin = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
        [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
        [Dependency] private readonly InteractionSystem _interactionSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _sharedInteractionSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly StackSystem _stack = default!;
        [Dependency] private readonly UseDelaySystem _useDelay = default!;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StorageComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<StorageComponent, GetVerbsEvent<ActivationVerb>>(AddOpenUiVerb);
            SubscribeLocalEvent<StorageComponent, GetVerbsEvent<UtilityVerb>>(AddTransferVerbs);
            SubscribeLocalEvent<StorageComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(ItemSlotsSystem) });
            SubscribeLocalEvent<StorageComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<StorageComponent, OpenStorageImplantEvent>(OnImplantActivate);
            SubscribeLocalEvent<StorageComponent, AfterInteractEvent>(AfterInteract);
            SubscribeLocalEvent<StorageComponent, DestructionEventArgs>(OnDestroy);
            SubscribeLocalEvent<StorageComponent, StorageInteractWithItemEvent>(OnInteractWithItem);
            SubscribeLocalEvent<StorageComponent, StorageInsertItemMessage>(OnInsertItemMessage);
            SubscribeLocalEvent<StorageComponent, BoundUIOpenedEvent>(OnBoundUIOpen);
            SubscribeLocalEvent<StorageComponent, BoundUIClosedEvent>(OnBoundUIClosed);
            SubscribeLocalEvent<StorageComponent, EntRemovedFromContainerMessage>(OnStorageItemRemoved);

            SubscribeLocalEvent<StorageComponent, AreaPickupDoAfterEvent>(OnDoAfter);

            SubscribeLocalEvent<StorageFillComponent, MapInitEvent>(OnStorageFillMapInit);
        }

        private void OnComponentInit(EntityUid uid, StorageComponent storageComp, ComponentInit args)
        {
            base.Initialize();

            // ReSharper disable once StringLiteralTypo
            storageComp.Container = _containerSystem.EnsureContainer<Container>(uid, "storagebase");
            UpdateStorageVisualization(uid, storageComp);
            RecalculateStorageUsed(storageComp);
            UpdateStorageUI(uid, storageComp);
        }

        private void AddOpenUiVerb(EntityUid uid, StorageComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            var silent = false;
            if (!args.CanAccess || !args.CanInteract || TryComp<LockComponent>(uid, out var lockComponent) && lockComponent.Locked)
            {
                // we allow admins to open the storage anyways
                if (!_admin.HasAdminFlag(args.User, AdminFlags.Admin))
                    return;

                silent = true;
            }

            silent |= HasComp<GhostComponent>(args.User);

            // Get the session for the user
            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            // Does this player currently have the storage UI open?
            var uiOpen = _uiSystem.SessionHasOpenUi(uid, StorageUiKey.Key, actor.PlayerSession);

            ActivationVerb verb = new()
            {
                Act = () => OpenStorageUI(uid, args.User, component, silent)
            };
            if (uiOpen)
            {
                verb.Text = Loc.GetString("verb-common-close-ui");
                verb.Icon = new SpriteSpecifier.Texture(
                    new("/Textures/Interface/VerbIcons/close.svg.192dpi.png"));
            }
            else
            {
                verb.Text = Loc.GetString("verb-common-open-ui");
                verb.Icon = new SpriteSpecifier.Texture(
                    new("/Textures/Interface/VerbIcons/open.svg.192dpi.png"));
            }
            args.Verbs.Add(verb);
        }

        private void AddTransferVerbs(EntityUid uid, StorageComponent component, GetVerbsEvent<UtilityVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            var entities = component.Container?.ContainedEntities;
            if (entities == null || entities.Count == 0 || TryComp(uid, out LockComponent? lockComponent) && lockComponent.Locked)
                return;

            // if the target is storage, add a verb to transfer storage.
            if (TryComp(args.Target, out StorageComponent? targetStorage)
                && (!TryComp(uid, out LockComponent? targetLock) || !targetLock.Locked))
            {
                UtilityVerb verb = new()
                {
                    Text = Loc.GetString("storage-component-transfer-verb"),
                    IconEntity = args.Using,
                    Act = () => TransferEntities(uid, args.Target, component, lockComponent, targetStorage, targetLock)
                };

                args.Verbs.Add(verb);
            }
        }

        /// <summary>
        /// Inserts storable entities into this storage container if possible, otherwise return to the hand of the user
        /// </summary>
        /// <returns>true if inserted, false otherwise</returns>
        private void OnInteractUsing(EntityUid uid, StorageComponent storageComp, InteractUsingEvent args)
        {
            if (args.Handled || !storageComp.ClickInsert || TryComp(uid, out LockComponent? lockComponent) && lockComponent.Locked)
                return;

            Log.Debug($"Storage (UID {uid}) attacked by user (UID {args.User}) with entity (UID {args.Used}).");

            if (HasComp<PlaceableSurfaceComponent>(uid))
                return;

            PlayerInsertHeldEntity(uid, args.User, storageComp);
            // Always handle it, even if insertion fails.
            // We don't want to trigger any AfterInteract logic here.
            // Example bug: placing wires if item doesn't fit in backpack.
            args.Handled = true;
        }

        /// <summary>
        /// Sends a message to open the storage UI
        /// </summary>
        /// <returns></returns>
        private void OnActivate(EntityUid uid, StorageComponent storageComp, ActivateInWorldEvent args)
        {
            if (args.Handled || _combatMode.IsInCombatMode(args.User) || TryComp(uid, out LockComponent? lockComponent) && lockComponent.Locked)
                return;

            OpenStorageUI(uid, args.User, storageComp);
        }

        /// <summary>
        /// Specifically for storage implants.
        /// </summary>
        private void OnImplantActivate(EntityUid uid, StorageComponent storageComp, OpenStorageImplantEvent args)
        {
            if (args.Handled || !TryComp<TransformComponent>(uid, out var xform))
                return;

            OpenStorageUI(uid, xform.ParentUid, storageComp);
        }

        /// <summary>
        /// Allows a user to pick up entities by clicking them, or pick up all entities in a certain radius
        /// around a click.
        /// </summary>
        /// <returns></returns>
        private async void AfterInteract(EntityUid uid, StorageComponent storageComp, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            // Pick up all entities in a radius around the clicked location.
            // The last half of the if is because carpets exist and this is terrible
            if (storageComp.AreaInsert && (args.Target == null || !HasComp<ItemComponent>(args.Target.Value)))
            {
                var validStorables = new List<EntityUid>();
                var itemQuery = GetEntityQuery<ItemComponent>();

                foreach (var entity in _entityLookupSystem.GetEntitiesInRange(args.ClickLocation, storageComp.AreaInsertRadius, LookupFlags.Dynamic | LookupFlags.Sundries))
                {
                    if (entity == args.User
                        || !itemQuery.HasComponent(entity)
                        || !CanInsert(uid, entity, out _, storageComp)
                        || !_interactionSystem.InRangeUnobstructed(args.User, entity))
                    {
                        continue;
                    }

                    validStorables.Add(entity);
                }

                //If there's only one then let's be generous
                if (validStorables.Count > 1)
                {
                    var doAfterArgs = new DoAfterArgs(args.User, 0.2f * validStorables.Count, new AreaPickupDoAfterEvent(validStorables), uid, target: uid)
                    {
                        BreakOnDamage = true,
                        BreakOnUserMove = true,
                        NeedHand = true
                    };

                    _doAfterSystem.TryStartDoAfter(doAfterArgs);
                }

                return;
            }

            // Pick up the clicked entity
            if (storageComp.QuickInsert)
            {
                if (args.Target is not { Valid: true } target)
                    return;

                if (_containerSystem.IsEntityInContainer(target)
                    || target == args.User
                    || !HasComp<ItemComponent>(target))
                    return;

                if (TryComp<TransformComponent>(uid, out var transformOwner) && TryComp<TransformComponent>(target, out var transformEnt))
                {
                    var parent = transformOwner.ParentUid;

                    var position = EntityCoordinates.FromMap(
                        parent.IsValid() ? parent : uid,
                        transformEnt.MapPosition,
                        _transform
                    );

                    if (PlayerInsertEntityInWorld(uid, args.User, target, storageComp))
                    {
                        RaiseNetworkEvent(new AnimateInsertingEntitiesEvent(uid,
                            new List<EntityUid> { target },
                            new List<EntityCoordinates> { position },
                            new List<Angle> { transformOwner.LocalRotation }));
                    }
                }
            }
        }

        private void OnDoAfter(EntityUid uid, StorageComponent component, AreaPickupDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            var successfullyInserted = new List<EntityUid>();
            var successfullyInsertedPositions = new List<EntityCoordinates>();
            var successfullyInsertedAngles = new List<Angle>();
            var itemQuery = GetEntityQuery<ItemComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            xformQuery.TryGetComponent(uid, out var xform);

            foreach (var entity in args.Entities)
            {
                // Check again, situation may have changed for some entities, but we'll still pick up any that are valid
                if (_containerSystem.IsEntityInContainer(entity)
                    || entity == args.Args.User
                    || !itemQuery.HasComponent(entity))
                    continue;

                if (xform == null ||
                    !xformQuery.TryGetComponent(entity, out var targetXform) ||
                    targetXform.MapID != xform.MapID)
                {
                    continue;
                }

                var position = EntityCoordinates.FromMap(
                    xform.ParentUid.IsValid() ? xform.ParentUid : uid,
                    new MapCoordinates(_transform.GetWorldPosition(targetXform, xformQuery), targetXform.MapID),
                    _transform
                );

                var angle = targetXform.LocalRotation;

                if (PlayerInsertEntityInWorld(uid, args.Args.User, entity, component))
                {
                    successfullyInserted.Add(entity);
                    successfullyInsertedPositions.Add(position);
                    successfullyInsertedAngles.Add(angle);
                }
            }

            // If we picked up atleast one thing, play a sound and do a cool animation!
            if (successfullyInserted.Count > 0)
            {
                _audio.PlayPvs(component.StorageInsertSound, uid);
                RaiseNetworkEvent(new AnimateInsertingEntitiesEvent(uid, successfullyInserted, successfullyInsertedPositions, successfullyInsertedAngles));
            }

            args.Handled = true;
        }

        private void OnDestroy(EntityUid uid, StorageComponent storageComp, DestructionEventArgs args)
        {
            var contained = storageComp.Container.ContainedEntities.ToArray();

            foreach (var entity in contained)
            {
                RemoveAndDrop(uid, entity, storageComp);
            }
        }

        /// <summary>
        ///     This function gets called when the user clicked on an item in the storage UI. This will either place the
        ///     item in the user's hand if it is currently empty, or interact with the item using the user's currently
        ///     held item.
        /// </summary>
        private void OnInteractWithItem(EntityUid uid, StorageComponent storageComp, StorageInteractWithItemEvent args)
        {
            // TODO move this to shared for prediction.
            if (args.Session.AttachedEntity is not EntityUid player)
                return;

            if (!Exists(args.InteractedItemUID))
            {
                Log.Error($"Player {args.Session} interacted with non-existent item {args.InteractedItemUID} stored in {ToPrettyString(uid)}");
                return;
            }

            if (!_actionBlockerSystem.CanInteract(player, args.InteractedItemUID) || storageComp.Container == null || !storageComp.Container.Contains(args.InteractedItemUID))
                return;

            // Does the player have hands?
            if (!TryComp(player, out HandsComponent? hands) || hands.Count == 0)
                return;

            // If the user's active hand is empty, try pick up the item.
            if (hands.ActiveHandEntity == null)
            {
                if (_sharedHandsSystem.TryPickupAnyHand(player, args.InteractedItemUID, handsComp: hands)
                    && storageComp.StorageRemoveSound != null)
                    _audio.Play(storageComp.StorageRemoveSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, AudioParams.Default);
                return;
            }

            // Else, interact using the held item
            _interactionSystem.InteractUsing(player, hands.ActiveHandEntity.Value, args.InteractedItemUID, Transform(args.InteractedItemUID).Coordinates, checkCanInteract: false);
        }

        private void OnInsertItemMessage(EntityUid uid, StorageComponent storageComp, StorageInsertItemMessage args)
        {
            // TODO move this to shared for prediction.
            if (args.Session.AttachedEntity == null)
                return;

            PlayerInsertHeldEntity(uid, args.Session.AttachedEntity.Value, storageComp);
        }

        private void OnBoundUIOpen(EntityUid uid, StorageComponent storageComp, BoundUIOpenedEvent args)
        {
            if (!storageComp.IsUiOpen)
            {
                storageComp.IsUiOpen = true;
                UpdateStorageVisualization(uid, storageComp);
            }
        }

        private void OnBoundUIClosed(EntityUid uid, StorageComponent storageComp, BoundUIClosedEvent args)
        {
            if (TryComp<ActorComponent>(args.Session.AttachedEntity, out var actor) && actor?.PlayerSession != null)
                CloseNestedInterfaces(uid, actor.PlayerSession, storageComp);

            // If UI is closed for everyone
            if (!_uiSystem.IsUiOpen(uid, args.UiKey))
            {
                storageComp.IsUiOpen = false;
                UpdateStorageVisualization(uid, storageComp);

                if (storageComp.StorageCloseSound is not null)
                    _audio.Play(storageComp.StorageCloseSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, storageComp.StorageCloseSound.Params);
            }
        }

        private void OnStorageItemRemoved(EntityUid uid, StorageComponent storageComp, EntRemovedFromContainerMessage args)
        {
            RecalculateStorageUsed(storageComp);
            UpdateStorageUI(uid, storageComp);
        }

        private void UpdateStorageVisualization(EntityUid uid, StorageComponent storageComp)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            _appearance.SetData(uid, StorageVisuals.Open, storageComp.IsUiOpen, appearance);
            _appearance.SetData(uid, SharedBagOpenVisuals.BagState, storageComp.IsUiOpen ? SharedBagState.Open : SharedBagState.Closed);

            if (HasComp<ItemCounterComponent>(uid))
                _appearance.SetData(uid, StackVisuals.Hide, !storageComp.IsUiOpen);
        }

        public void RecalculateStorageUsed(StorageComponent storageComp)
        {
            storageComp.StorageUsed = 0;
            storageComp.SizeCache.Clear();

            if (storageComp.Container == null)
                return;

            var itemQuery = GetEntityQuery<ItemComponent>();

            foreach (var entity in storageComp.Container.ContainedEntities)
            {
                if (!itemQuery.TryGetComponent(entity, out var itemComp))
                    continue;

                var size = itemComp.Size;
                storageComp.StorageUsed += size;
                storageComp.SizeCache.Add(entity, size);
            }
        }

        public int GetAvailableSpace(EntityUid uid, StorageComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return 0;

            return component.StorageCapacityMax - component.StorageUsed;
        }

        /// <summary>
        ///     Move entities from one storage to another.
        /// </summary>
        public void TransferEntities(EntityUid source, EntityUid target,
            StorageComponent? sourceComp = null, LockComponent? sourceLock = null,
            StorageComponent? targetComp = null, LockComponent? targetLock = null)
        {
            if (!Resolve(source, ref sourceComp) || !Resolve(target, ref targetComp))
                return;

            var entities = sourceComp.Container?.ContainedEntities;
            if (entities == null || entities.Count == 0)
                return;

            if (Resolve(source, ref sourceLock, false) && sourceLock.Locked
                || Resolve(target, ref targetLock, false) && targetLock.Locked)
                return;

            foreach (var entity in entities.ToList())
            {
                Insert(target, entity, targetComp);
            }
            RecalculateStorageUsed(sourceComp);
            UpdateStorageUI(source, sourceComp);
        }

        /// <summary>
        ///     Verifies if an entity can be stored and if it fits
        /// </summary>
        /// <param name="uid">The entity to check</param>
        /// <param name="reason">If returning false, the reason displayed to the player</param>
        /// <returns>true if it can be inserted, false otherwise</returns>
        public bool CanInsert(EntityUid uid, EntityUid insertEnt, out string? reason, StorageComponent? storageComp = null)
        {
            if (!Resolve(uid, ref storageComp))
            {
                reason = null;
                return false;
            }

            if (TryComp(insertEnt, out TransformComponent? transformComp) && transformComp.Anchored)
            {
                reason = "comp-storage-anchored-failure";
                return false;
            }

            if (storageComp.Whitelist?.IsValid(insertEnt, EntityManager) == false)
            {
                reason = "comp-storage-invalid-container";
                return false;
            }

            if (storageComp.Blacklist?.IsValid(insertEnt, EntityManager) == true)
            {
                reason = "comp-storage-invalid-container";
                return false;
            }

            if (TryComp(insertEnt, out StorageComponent? storage) &&
                storage.StorageCapacityMax >= storageComp.StorageCapacityMax)
            {
                reason = "comp-storage-insufficient-capacity";
                return false;
            }

            if (TryComp(insertEnt, out ItemComponent? itemComp) &&
                itemComp.Size > storageComp.StorageCapacityMax - storageComp.StorageUsed)
            {
                reason = "comp-storage-insufficient-capacity";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>
        ///     Inserts into the storage container
        /// </summary>
        /// <returns>true if the entity was inserted, false otherwise</returns>
        public bool Insert(EntityUid uid, EntityUid insertEnt, StorageComponent? storageComp = null, bool playSound = true)
        {
            if (!Resolve(uid, ref storageComp) || !CanInsert(uid, insertEnt, out _, storageComp) || storageComp.Container == null)
                return false;

            /*
             * 1. If the inserted thing is stackable then try to stack it to existing stacks
             * 2. If anything remains insert whatever is possible.
             * 3. If insertion is not possible then leave the stack as is.
             * At either rate still play the insertion sound
             *
             * For now we just treat items as always being the same size regardless of stack count.
             */

            // If it's stackable then prefer to stack it
            var stackQuery = GetEntityQuery<StackComponent>();

            if (stackQuery.TryGetComponent(insertEnt, out var insertStack))
            {
                var toInsertCount = insertStack.Count;

                foreach (var ent in storageComp.Container.ContainedEntities)
                {
                    if (!stackQuery.TryGetComponent(ent, out var containedStack) || !insertStack.StackTypeId.Equals(containedStack.StackTypeId))
                        continue;

                    if (!_stack.TryAdd(insertEnt, ent, insertStack, containedStack))
                        continue;

                    var remaining = insertStack.Count;
                    toInsertCount -= toInsertCount - remaining;

                    if (remaining > 0)
                        continue;

                    break;
                }

                // Still stackable remaining
                if (insertStack.Count > 0)
                {
                    // Try to insert it as a new stack.
                    if (TryComp(insertEnt, out ItemComponent? itemComp) &&
                        itemComp.Size > storageComp.StorageCapacityMax - storageComp.StorageUsed ||
                        !storageComp.Container.Insert(insertEnt))
                    {
                        // If we also didn't do any stack fills above then just end
                        // otherwise play sound and update UI anyway.
                        if (toInsertCount == insertStack.Count)
                            return false;
                    }
                }
            }
            // Non-stackable but no insertion for reasons.
            else if (!storageComp.Container.Insert(insertEnt))
            {
                return false;
            }

            if (playSound && storageComp.StorageInsertSound is not null)
                _audio.PlayPvs(storageComp.StorageInsertSound, uid);

            RecalculateStorageUsed(storageComp);
            UpdateStorageUI(uid, storageComp);
            return true;
        }

        // REMOVE: remove and drop on the ground
        public bool RemoveAndDrop(EntityUid uid, EntityUid removeEnt, StorageComponent? storageComp = null)
        {
            if (!Resolve(uid, ref storageComp))
                return false;

            var itemRemoved = storageComp.Container?.Remove(removeEnt) == true;
            if (itemRemoved)
                RecalculateStorageUsed(storageComp);

            return itemRemoved;
        }

        /// <summary>
        ///     Inserts an entity into storage from the player's active hand
        /// </summary>
        /// <param name="player">The player to insert an entity from</param>
        /// <returns>true if inserted, false otherwise</returns>
        public bool PlayerInsertHeldEntity(EntityUid uid, EntityUid player, StorageComponent? storageComp = null)
        {
            if (!Resolve(uid, ref storageComp) || !TryComp(player, out HandsComponent? hands) || hands.ActiveHandEntity == null)
                return false;

            var toInsert = hands.ActiveHandEntity;

            if (!CanInsert(uid, toInsert.Value, out var reason, storageComp))
            {
                Popup(uid, player, reason ?? "comp-storage-cant-insert", storageComp);
                return false;
            }

            if (!_sharedHandsSystem.TryDrop(player, toInsert.Value, handsComp: hands))
            {
                PopupEnt(uid, player, "comp-storage-cant-drop", toInsert.Value, storageComp);
                return false;
            }

            return PlayerInsertEntityInWorld(uid, player, toInsert.Value, storageComp);
        }

        /// <summary>
        ///     Inserts an Entity (<paramref name="toInsert"/>) in the world into storage, informing <paramref name="player"/> if it fails.
        ///     <paramref name="toInsert"/> is *NOT* held, see <see cref="PlayerInsertHeldEntity(Robust.Shared.GameObjects.EntityUid)"/>.
        /// </summary>
        /// <param name="player">The player to insert an entity with</param>
        /// <returns>true if inserted, false otherwise</returns>
        public bool PlayerInsertEntityInWorld(EntityUid uid, EntityUid player, EntityUid toInsert, StorageComponent? storageComp = null)
        {
            if (!Resolve(uid, ref storageComp) || !_sharedInteractionSystem.InRangeUnobstructed(player, uid))
                return false;

            if (!Insert(uid, toInsert, storageComp))
            {
                Popup(uid, player, "comp-storage-cant-insert", storageComp);
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Opens the storage UI for an entity
        /// </summary>
        /// <param name="entity">The entity to open the UI for</param>
        public void OpenStorageUI(EntityUid uid, EntityUid entity, StorageComponent? storageComp = null, bool silent = false)
        {
            if (!Resolve(uid, ref storageComp) || !TryComp(entity, out ActorComponent? player))
                return;

            // prevent spamming bag open / honkerton honk sound
            silent |= TryComp<UseDelayComponent>(uid, out var useDelay) && _useDelay.ActiveDelay(uid, useDelay);
            if (!silent)
            {
                _audio.PlayPvs(storageComp.StorageOpenSound, uid);
                if (useDelay != null)
                    _useDelay.BeginDelay(uid, useDelay);
            }

            Log.Debug($"Storage (UID {uid}) \"used\" by player session (UID {player.PlayerSession.AttachedEntity}).");

            var bui = _uiSystem.GetUiOrNull(uid, StorageUiKey.Key);
            if (bui != null)
                _uiSystem.OpenUi(bui, player.PlayerSession);
        }

        /// <summary>
        ///     If the user has nested-UIs open (e.g., PDA UI open when pda is in a backpack), close them.
        /// </summary>
        /// <param name="session"></param>
        public void CloseNestedInterfaces(EntityUid uid, IPlayerSession session, StorageComponent? storageComp = null)
        {
            if (!Resolve(uid, ref storageComp))
                return;

            // for each containing thing
            // if it has a storage comp
            // ensure unsubscribe from session
            // if it has a ui component
            // close ui
            foreach (var entity in storageComp.Container.ContainedEntities)
            {
                if (!TryComp(entity, out SharedUserInterfaceComponent? ui))
                    continue;

                foreach (var bui in ui.Interfaces.Values)
                {
                    _uiSystem.TryClose(entity, bui.UiKey, session, ui);
                }
            }
        }

        public void UpdateStorageUI(EntityUid uid, StorageComponent storageComp)
        {
            var state = new StorageBoundUserInterfaceState((List<EntityUid>) storageComp.Container.ContainedEntities, storageComp.StorageUsed, storageComp.StorageCapacityMax);
            var bui = _uiSystem.GetUiOrNull(uid, StorageUiKey.Key);

            if (bui != null)
                UserInterfaceSystem.SetUiState(bui, state);
        }

        private void Popup(EntityUid _, EntityUid player, string message, StorageComponent storageComp)
        {
            _popupSystem.PopupEntity(Loc.GetString(message), player, player);
        }

        private void PopupEnt(EntityUid _, EntityUid player, string message, EntityUid entityUid, StorageComponent storageComp)
        {
            _popupSystem.PopupEntity(Loc.GetString(message, ("entity", entityUid)), player, player);
        }
    }
}
