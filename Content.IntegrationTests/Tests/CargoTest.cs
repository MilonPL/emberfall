using System.Threading.Tasks;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class CargoTest : ContentIntegrationTest
{
    [Test]
    public async Task Arbitrage()
    {
        var server = StartServer();
        await server.WaitIdleAsync();

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var pricing = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<PricingSystem>();

        await server.WaitAssertion(() =>
        {
            var mapId = mapManager.CreateMap();

            foreach (var proto in protoManager.EnumeratePrototypes<CargoProductPrototype>())
            {
                var ent = entManager.SpawnEntity(proto.Product, new MapCoordinates(Vector2.Zero, mapId));
                var price = pricing.GetPrice(ent);

                Assert.That(price, Is.LessThan(proto.PointCost), $"Found arbitrage on {proto.ID} cargo product!");
            }
        });
    }
}
