using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Wall;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Construction
{
    /// <summary>
    /// The client-side implementation of the construction system, which is used for constructing entities in game.
    /// </summary>
    [UsedImplicitly]
    public sealed class ConstructionSystem : SharedConstructionSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;

        private readonly Dictionary<int, EntityUid> _ghosts = new();
        private readonly Dictionary<string, ConstructionGuide> _guideCache = new();

        private int _nextId;

        public bool CraftingEnabled { get; private set; }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PlayerAttachSysMessage>(HandlePlayerAttached);
            SubscribeNetworkEvent<AckStructureConstructionMessage>(HandleAckStructure);
            SubscribeNetworkEvent<ResponseConstructionGuide>(OnConstructionGuideReceived);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenCraftingMenu,
                    new PointerInputCmdHandler(HandleOpenCraftingMenu))
                .Bind(EngineKeyFunctions.Use,
                    new PointerInputCmdHandler(HandleUse))
                .Register<ConstructionSystem>();

            SubscribeLocalEvent<ConstructionGhostComponent, ExaminedEvent>(HandleConstructionGhostExamined);
        }

        private void OnConstructionGuideReceived(ResponseConstructionGuide ev)
        {
            _guideCache[ev.ConstructionId] = ev.Guide;
            ConstructionGuideAvailable?.Invoke(this, ev.ConstructionId);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            CommandBinds.Unregister<ConstructionSystem>();
        }

        public ConstructionGuide? GetGuide(ConstructionPrototype prototype)
        {
            if (_guideCache.TryGetValue(prototype.ID, out var guide))
                return guide;

            RaiseNetworkEvent(new RequestConstructionGuide(prototype.ID));
            return null;
        }

        private void HandleConstructionGhostExamined(EntityUid uid, ConstructionGhostComponent component, ExaminedEvent args)
        {
            if (component.Prototype == null) return;

            args.PushMarkup(Loc.GetString(
                "construction-ghost-examine-message",
                ("name", component.Prototype.Name)));

            if (!_prototypeManager.TryIndex(component.Prototype.Graph, out ConstructionGraphPrototype? graph))
                return;

            var startNode = graph.Nodes[component.Prototype.StartNode];

            if (!graph.TryPath(component.Prototype.StartNode, component.Prototype.TargetNode, out var path) ||
                !startNode.TryGetEdge(path[0].Name, out var edge))
            {
                return;
            }

            edge.Steps[0].DoExamine(args);
        }

        public event EventHandler<CraftingAvailabilityChangedArgs>? CraftingAvailabilityChanged;
        public event EventHandler<string>? ConstructionGuideAvailable;
        public event EventHandler? ToggleCraftingWindow;

        private void HandleAckStructure(AckStructureConstructionMessage msg)
        {
            ClearGhost(msg.GhostId);
        }

        private void HandlePlayerAttached(PlayerAttachSysMessage msg)
        {
            var available = IsCraftingAvailable(msg.AttachedEntity);
            UpdateCraftingAvailability(available);
        }

        private bool HandleOpenCraftingMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (args.State == BoundKeyState.Down)
                ToggleCraftingWindow?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private void UpdateCraftingAvailability(bool available)
        {
            if (CraftingEnabled == available)
                return;

            CraftingAvailabilityChanged?.Invoke(this, new CraftingAvailabilityChangedArgs(available));
            CraftingEnabled = available;
        }

        private static bool IsCraftingAvailable(EntityUid? entity)
        {
            if (entity == default)
                return false;

            // TODO: Decide if entity can craft, using capabilities or something
            return true;
        }

        private bool HandleUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (!args.EntityUid.IsValid() || !args.EntityUid.IsClientSide())
                return false;

            if (!EntityManager.TryGetComponent<ConstructionGhostComponent?>(args.EntityUid, out var ghostComp))
                return false;

            TryStartConstruction(ghostComp.GhostId);
            return true;
        }

        private string? GetTargetNodeEntity(ConstructionPrototype prototype)
        {
            if (!_prototypeManager.TryIndex(prototype.Graph, out ConstructionGraphPrototype? graph))
                return null;

            var targetNode = graph.Nodes[prototype.TargetNode];
            if (string.IsNullOrEmpty(targetNode.Entity))
                return null;

            return targetNode.Entity;
        }

        // Don't use the sprite component it isn't associated to any entity
        public SpriteComponent? GetTargetNodeSprite(ConstructionPrototype prototype)
        {
            var targetNode = GetTargetNodeEntity(prototype);
            if (targetNode == null)
                return null;

            if (!_prototypeManager.TryIndex<EntityPrototype>(targetNode, out EntityPrototype? entityPrototype))
                return null;

            if (!entityPrototype.TryGetComponent("Sprite", out SpriteComponent? sprite))
                return null;

            return sprite;
        }

        private void SetSpriteFromIcon(SpriteSpecifier icon, SpriteComponent sprite)
        {
            sprite.AddBlankLayer(0); // There is no way to actually check if this already exists, so we blindly insert a new one
            sprite.LayerSetSprite(0, icon);
            sprite.LayerSetVisible(0, true);
        }

        private bool SetSpriteFromTargetNode(ConstructionPrototype prototype, SpriteComponent sprite)
        {
            var spritePrototype = GetTargetNodeSprite(prototype);
            if (spritePrototype == null) {
                return false;
            }

            sprite.CopyFrom(spritePrototype);
            sprite.Visible = true;

            return true;
        }

        private bool SetSprite(ConstructionPrototype prototype, SpriteComponent sprite)
        {
            if (SetSpriteFromTargetNode(prototype, sprite))
            {
                return true;
            }

            SetSpriteFromIcon(prototype.Icon, sprite);
            return false;
        }

        private void SetGhostSprite(ConstructionPrototype prototype, SpriteComponent sprite)
        {
            // Save previous color
            var color = sprite.Color;

            // If the target node sprite is used, the default color is reset.
            if (SetSprite(prototype, sprite))
            {
                // Set back the default color
                sprite.Color = color;
            }
        }

        /// <summary>
        /// Creates a construction ghost at the given location.
        /// </summary>
        public void SpawnGhost(ConstructionPrototype prototype, EntityCoordinates loc, Direction dir)
        {
            if (_playerManager.LocalPlayer?.ControlledEntity is not { } user ||
                !user.IsValid())
            {
                return;
            }

            if (GhostPresent(loc)) return;

            // This InRangeUnobstructed should probably be replaced with "is there something blocking us in that tile?"
            var coords = loc.ToMap(EntityManager, _transformSystem);
            var predicate = GetPredicate(prototype.CanBuildInImpassable, coords);
            if (!_interactionSystem.InRangeUnobstructed(user, loc, 20f, predicate: predicate))
                return;

            foreach (var condition in prototype.Conditions)
            {
                if (!condition.Condition(user, loc, dir))
                    return;
            }

            var ghost = EntityManager.SpawnEntity("constructionghost", loc);
            var comp = EntityManager.GetComponent<ConstructionGhostComponent>(ghost);
            comp.Prototype = prototype;
            comp.GhostId = _nextId++;
            EntityManager.GetComponent<TransformComponent>(ghost).LocalRotation = dir.ToAngle();
            _ghosts.Add(comp.GhostId, ghost);
            var sprite = EntityManager.GetComponent<SpriteComponent>(ghost);
            SetGhostSprite(prototype, sprite);

            if (prototype.CanBuildInImpassable)
                EnsureComp<WallMountComponent>(ghost).Arc = new(Math.Tau);
        }

        /// <summary>
        /// Checks if any construction ghosts are present at the given position
        /// </summary>
        private bool GhostPresent(EntityCoordinates loc)
        {
            foreach (var ghost in _ghosts)
            {
                if (EntityManager.GetComponent<TransformComponent>(ghost.Value).Coordinates.Equals(loc)) return true;
            }

            return false;
        }

        private void TryStartConstruction(int ghostId)
        {
            var ghost = _ghosts[ghostId];
            var comp = EntityManager.GetComponent<ConstructionGhostComponent>(ghost);

            if (comp.Prototype == null)
            {
                throw new ArgumentException($"Can't start construction for a ghost with no prototype. Ghost id: {ghostId}");
            }

            var transform = EntityManager.GetComponent<TransformComponent>(ghost);
            var msg = new TryStartStructureConstructionMessage(transform.Coordinates, comp.Prototype.ID, transform.LocalRotation, ghostId);
            RaiseNetworkEvent(msg);
        }

        /// <summary>
        /// Starts constructing an item underneath the attached entity.
        /// </summary>
        public void TryStartItemConstruction(string prototypeName)
        {
            RaiseNetworkEvent(new TryStartItemConstructionMessage(prototypeName));
        }

        /// <summary>
        /// Removes a construction ghost entity with the given ID.
        /// </summary>
        public void ClearGhost(int ghostId)
        {
            if (_ghosts.TryGetValue(ghostId, out var ghost))
            {
                EntityManager.QueueDeleteEntity(ghost);
                _ghosts.Remove(ghostId);
            }
        }

        /// <summary>
        /// Removes all construction ghosts.
        /// </summary>
        public void ClearAllGhosts()
        {
            foreach (var (_, ghost) in _ghosts)
            {
                EntityManager.QueueDeleteEntity(ghost);
            }

            _ghosts.Clear();
        }
    }

    public sealed class CraftingAvailabilityChangedArgs : EventArgs
    {
        public bool Available { get; }

        public CraftingAvailabilityChangedArgs(bool available)
        {
            Available = available;
        }
    }
}
