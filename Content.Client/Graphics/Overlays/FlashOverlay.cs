﻿using System.Net.Mime;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.Graphics.Overlays
{
    public class FlashOverlay : Overlay
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IClyde _displayManager;
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 649

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;
        private double _startTime;
        private uint lastsFor = 5000;
        private Texture _screenshotTexture;

        public FlashOverlay() : base(nameof(FlashOverlay))
        {
            IoCManager.InjectDependencies(this);
            Shader = _prototypeManager.Index<ShaderPrototype>("FlashedEffect").Instance().Duplicate();
        }

        public override void BeforeDraw()
        {
            _startTime = _gameTiming.CurTime.TotalMilliseconds;
            _displayManager.Screenshot(ScreenshotType.BeforeUI, image =>
            {
                var rgba32Image = image.CloneAs<Rgba32>(Configuration.Default);
                _screenshotTexture = _displayManager.LoadTextureFromImage(rgba32Image);
            });
        }

        protected override void Draw(DrawingHandleBase handle)
        {
            var percentComplete = (float) ((_gameTiming.CurTime.TotalMilliseconds - _startTime) / lastsFor);
            Shader?.SetParameter("percentComplete", percentComplete);

            var screenSpaceHandle = handle as DrawingHandleScreen;
            var screenSize = UIBox2.FromDimensions((0, 0), _displayManager.ScreenSize);

            if (_screenshotTexture != null)
            {
                screenSpaceHandle?.DrawTextureRect(_screenshotTexture, screenSize);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _screenshotTexture = null;
        }
    }
}
