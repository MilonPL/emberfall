using Content.Server._Emberfall.Planetside.Components;

namespace Content.Server._Emberfall.Planetside.Systems;

/// <summary>
/// This handles spawning a new map from a TerrainTemplatePrototype.
/// <seealso cref="PlanetsideTerrainSystem"/>
/// </summary>
public sealed class PlanetsideTerrainSpawnerSystem : EntitySystem
{
    [Dependency] private readonly PlanetsideTerrainSystem _planetside = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlanetsideTerrainSpawnerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PlanetsideTerrainSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnShutdown(Entity<PlanetsideTerrainSpawnerComponent> ent, ref ComponentShutdown args)
    {
        QueueDel(ent.Comp.Map);
    }

    private void OnMapInit(Entity<PlanetsideTerrainSpawnerComponent> ent, ref MapInitEvent args)
    {
        // No terrain - needed for test to not fail
        if (string.IsNullOrEmpty(ent.Comp.Terrain))
            return;

        if (ent.Comp.GridPath is { } path)
        {
            ent.Comp.Map = _planetside.GenerateTerrainWithStructures(ent.Comp.Terrain, path.ToString());
            return;
        }

        ent.Comp.Map = _planetside.GenerateTerrain(ent.Comp.Terrain);
    }
}
