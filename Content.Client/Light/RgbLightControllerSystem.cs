using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.Light;
using Content.Shared.Light.Component;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.Light
{
    public sealed class RgbLightControllerSystem : SharedRgbLightControllerSystem
    {
        [Dependency] private IGameTiming _gameTiming = default!;
        [Dependency] private ItemSystem _itemSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RgbLightControllerComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<RgbLightControllerComponent, ComponentShutdown>(OnComponentShutdown);
            SubscribeLocalEvent<RgbLightControllerComponent, ComponentStartup>(OnComponentStart);

            SubscribeLocalEvent<RgbLightControllerComponent, GotUnequippedEvent>(OnGotUnequipped);

            SubscribeLocalEvent<RgbLightControllerComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
            SubscribeLocalEvent<RgbLightControllerComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        }

        private void OnComponentStart(EntityUid uid, RgbLightControllerComponent rgb, ComponentStartup args)
        {
            GetOriginalColors(uid, rgb);

            // trigger visuals updated events
            _itemSystem.VisualsChanged(uid);
        }

        private void OnComponentShutdown(EntityUid uid, RgbLightControllerComponent rgb, ComponentShutdown args)
        {
            ResetOriginalColors(uid, rgb);

            // and reset any in-hands or clothing sprites
            _itemSystem.VisualsChanged(uid);
        }

        private void OnGotUnequipped(EntityUid uid, RgbLightControllerComponent rgb, GotUnequippedEvent args)
        {
            rgb.Holder = null;
            rgb.HolderLayers = null;
        }

        private void OnHeldVisualsUpdated(EntityUid uid, RgbLightControllerComponent rgb, HeldVisualsUpdatedEvent args)
        {
            if (args.RevealedLayers.Count == 0)
            {
                rgb.Holder = null;
                rgb.HolderLayers = null;
                return;
            }

            rgb.Holder = args.User;
            rgb.HolderLayers = new();

            if (!TryComp(args.User, out SpriteComponent? sprite))
                return;

            foreach (var key in args.RevealedLayers)
            {
                if (!sprite.LayerMapTryGet(key, out var index) || sprite[index] is not Layer layer)
                    continue;

                if (layer.ShaderPrototype == "unshaded")
                    rgb.HolderLayers.Add(key);
            }
        }

        private void OnEquipmentVisualsUpdated(EntityUid uid, RgbLightControllerComponent rgb, EquipmentVisualsUpdatedEvent args)
        {
            rgb.Holder = args.Equipee;
            rgb.HolderLayers = new();

            if (!TryComp(args.Equipee, out SpriteComponent? sprite))
                return;

            foreach (var key in args.RevealedLayers)
            {
                if (!sprite.LayerMapTryGet(key, out var index) || sprite[index] is not Layer layer)
                    continue;

                if (layer.ShaderPrototype == "unshaded")
                    rgb.HolderLayers.Add(key);
            }
        }
        private void OnHandleState(EntityUid uid, RgbLightControllerComponent rgb, ref ComponentHandleState args)
        {
            if (args.Current is not RgbLightControllerState state)
                return;

            ResetOriginalColors(uid, rgb);
            rgb.CycleRate = state.CycleRate;
            rgb.Layers = state.Layers;
            GetOriginalColors(uid, rgb);

        }

        private void GetOriginalColors(EntityUid uid, RgbLightControllerComponent? rgb = null, PointLightComponent? light = null, SpriteComponent? sprite = null)
        {
            if (!Resolve(uid, ref rgb, ref sprite, ref light))
                return;

            rgb.OriginalLightColor = light.Color;
            rgb.OriginalLayerColors = new();

            var layerCount = sprite.AllLayers.Count();

            // if layers is null, get unshaded layers
            if (rgb.Layers == null)
            {
                rgb.Layers = new();

                for (var i = 0; i < layerCount; i++)
                {
                    if (sprite[i] is Layer layer && layer.ShaderPrototype == "unshaded")
                    {
                        rgb.Layers.Add(i);
                        rgb.OriginalLayerColors[i] = layer.Color;
                    }
                }
                return;
            }

            foreach (var index in rgb.Layers.ToArray())
            {
                if (index < layerCount)
                    rgb.OriginalLayerColors[index] = sprite[index].Color;
                else
                {
                    // admeme fuck-ups or bad yaml?
                    Logger.Warning($"RGB light attempted to us invalid sprite index {index} on entity {ToPrettyString(uid)}");
                    rgb.Layers.Remove(index);
                }
            }
        }

        private void ResetOriginalColors(EntityUid uid, RgbLightControllerComponent? rgb = null, PointLightComponent? light = null, SpriteComponent? sprite = null)
        {
            if (!Resolve(uid, ref rgb, ref sprite, ref light))
                return;

            light.Color = rgb.OriginalLightColor;

            if (rgb.Layers == null || rgb.OriginalLayerColors == null)
                return;

            foreach (var (layer, color) in rgb.OriginalLayerColors)
            {
                sprite.LayerSetColor(layer, color);
            }
        }

        public override void FrameUpdate(float frameTime)
        {
            foreach (var (rgb, light, sprite) in EntityManager.EntityQuery<RgbLightControllerComponent, PointLightComponent, SpriteComponent>())
            {
                var color = GetCurrentRgbColor(_gameTiming.RealTime, rgb.CreationTick.Value * _gameTiming.TickPeriod, rgb);

                light.Color = color;

                if (rgb.Layers != null)
                {
                    foreach (var layer in rgb.Layers)
                    {
                        sprite.LayerSetColor(layer, color);
                    }
                }

                // is the entity being held by someone?
                if (rgb.HolderLayers == null || !TryComp(rgb.Holder, out SpriteComponent? holderSprite))
                    continue;

                foreach (var layer in rgb.HolderLayers)
                {
                    holderSprite.LayerSetColor(layer, color);
                }
            }
        }

        public static Color GetCurrentRgbColor(TimeSpan curTime, TimeSpan offset, RgbLightControllerComponent rgb)
        {
            return Color.FromHsv(new Vector4(
                (float) (((curTime.TotalSeconds - offset.TotalSeconds) * rgb.CycleRate + Math.Abs(rgb.Owner.GetHashCode() * 0.1)) % 1),
                1.0f,
                1.0f,
                1.0f
            ));
        }
    }
}
