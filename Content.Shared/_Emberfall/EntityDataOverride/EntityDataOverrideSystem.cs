using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._Emberfall.EntityDataOverride;

/// <summary>
///     Handles applying name and description overrides from EntityDataOverridePrototypes.
/// </summary>
public sealed class EntityDataOverrideSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private EntityQuery<MetaDataComponent> _metaQuery;
    private readonly Dictionary<string, EntityDataOverridePrototype> _overrideCache = new();

    public override void Initialize()
    {
        base.Initialize();

        _metaQuery = GetEntityQuery<MetaDataComponent>();
        SubscribeLocalEvent<MetaDataComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MetaDataComponent, ComponentStartup>(OnStartup);

        // Cache our overrides for faster lookup
        _prototype.PrototypesReloaded += OnPrototypesReloaded;
        CacheOverrides();
    }

    private void OnStartup(Entity<MetaDataComponent> ent, ref ComponentStartup args)
    {
        // Apply override for newly spawned entities
        TryApplyOverride(ent);
    }

    private void OnMapInit(Entity<MetaDataComponent> ent, ref MapInitEvent ev)
    {
        // Apply overrides for map-spawned entities
        TryApplyOverride(ent);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.ContainsKey(typeof(EntityDataOverridePrototype)))
            return;

        // Recache and reapply overrides
        _overrideCache.Clear();
        CacheOverrides();

        var query = AllEntityQuery<MetaDataComponent>();
        while (query.MoveNext(out var uid, out var meta))
        {
            TryApplyOverride((uid, meta));
        }
    }

    private void CacheOverrides()
    {
        foreach (var proto in _prototype.EnumeratePrototypes<EntityDataOverridePrototype>())
        {
            _overrideCache[proto.ID] = proto;
        }
    }

    /// <summary>
    ///     Attempts to apply an entity data override to the given entity if one exists.
    /// </summary>
    [PublicAPI]
    public void TryApplyOverride(Entity<MetaDataComponent> ent)
    {
        var meta = ent.Comp;
        if (meta.EntityPrototype == null)
            return;

        // Check if we have an override for this entity's prototype
        if (!_overrideCache.TryGetValue(meta.EntityPrototype.ID, out var override_)) // funny
            return;

        // Only apply non-null overrides
        if (override_.Name != null)
            _metaData.SetEntityName(ent, override_.Name, meta);

        if (override_.Description != null)
            _metaData.SetEntityDescription(ent, override_.Description, meta);
    }
}
