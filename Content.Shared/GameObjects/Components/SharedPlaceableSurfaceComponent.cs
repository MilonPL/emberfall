﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedPlaceableSurfaceComponent : Component
    {
        public override string Name => "PlaceableSurface";
        public override uint? NetID => ContentNetIDs.PLACEABLE_SURFACE;
        public virtual bool IsPlaceable { get; set; }
        public virtual bool PlaceCentered { get; set; }
        public virtual Vector2 PositionOffset { get; set; }
    }

    [Serializable, NetSerializable]
    public class PlaceableSurfaceComponentState : ComponentState
    {
        public readonly bool IsPlaceable;

        public PlaceableSurfaceComponentState(bool placeable) : base(ContentNetIDs.PLACEABLE_SURFACE)
        {
            IsPlaceable = placeable;
        }
    }
}
