﻿using Content.Server.Hands.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Holiday.Christmas;

/// <summary>
/// This handles granting players their gift.
/// </summary>
public sealed class GiftPackinSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly List<string> _possibleGiftsSafe = new();
    private readonly List<string> _possibleGiftsUnsafe = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        _prototype.PrototypesReloaded += OnPrototypesReloaded;
        SubscribeLocalEvent<GiftPackinComponent, MapInitEvent>(OnGiftMapInit);
        SubscribeLocalEvent<GiftPackinComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<GiftPackinComponent, ExaminedEvent>(OnExamined);
        BuildIndex();
    }

    private void OnExamined(EntityUid uid, GiftPackinComponent component, ExaminedEvent args)
    {
        if (!component.ContentsViewers.IsValid(args.Examiner, EntityManager) || component.SelectedEntity is null)
            return;

        var name = _prototype.Index<EntityPrototype>(component.SelectedEntity).Name;
        args.Message.PushNewline();
        args.Message.AddText(Loc.GetString("gift-packin-contains", ("name", name)));
    }

    private void OnUseInHand(EntityUid uid, GiftPackinComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (component.SelectedEntity is null)
            return;

        var coords = Transform(args.User).Coordinates;
        var handsEnt = Spawn(component.SelectedEntity, coords);
        EnsureComp<ItemComponent>(handsEnt); // For insane mode.
        if (component.Wrapper is not null)
            Spawn(component.Wrapper, coords);

        args.Handled = true;
        Del(uid);
        _hands.PickupOrDrop(args.User, handsEnt);
    }

    private void OnGiftMapInit(EntityUid uid, GiftPackinComponent component, MapInitEvent args)
    {
        if (component.InsaneMode)
            component.SelectedEntity = _random.Pick(_possibleGiftsUnsafe);
        else
            component.SelectedEntity = _random.Pick(_possibleGiftsSafe);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        BuildIndex();
    }

    private void BuildIndex()
    {
        _possibleGiftsSafe.Clear();
        _possibleGiftsUnsafe.Clear();
        var itemCompName = _componentFactory.GetComponentName(typeof(ItemComponent));
        var mapGridCompName = _componentFactory.GetComponentName(typeof(MapGridComponent));
        var physicsCompName = _componentFactory.GetComponentName(typeof(PhysicsComponent));

        foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract || proto.NoSpawn || proto.Components.ContainsKey(mapGridCompName) || !proto.Components.ContainsKey(physicsCompName))
                continue;

            _possibleGiftsUnsafe.Add(proto.ID);

            if (!proto.Components.ContainsKey(itemCompName))
                continue;

            _possibleGiftsSafe.Add(proto.ID);
        }
    }
}
