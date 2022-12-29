using Content.Shared.Paper;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Utility;

namespace Content.Client.Paper.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class PaperWindow : DefaultWindow
    {
        public PaperWindow()
        {
            RobustXamlLoader.Load(this);

            //var resourceCache = IoCManager.Resolve<IResourceCache>();
            //var backgroundTexture = resourceCache.GetTexture("/Textures/Interface/Nano/lined_paper.svg.96dpi.png");
        }

        public void InitVisuals(PaperVisualsComponent visuals)
        {
            var resCache = IoCManager.Resolve<IResourceCache>();

            _texturesDirty = true;

            //<todo.eoin there is surely sugar for this?
            _backgroundImage = visuals.BackgroundImagePath != null? resCache.GetResource<TextureResource>(visuals.BackgroundImagePath) : null;
            if(visuals.BackgroundPatchMargin != null)
            {
                _backgroundPatchMargin = (Box2)visuals.BackgroundPatchMargin;
            }
            else
            {
                _backgroundPatchMargin = new();
            }

            if (visuals.BackgroundModulate != null)
            {
                PaperBackground.ModulateSelfOverride = (Color)visuals.BackgroundModulate;
            }

            _backgroundImageMode = visuals.BackgroundImageTile ? StyleBoxTexture.StretchMode.Tile : StyleBoxTexture.StretchMode.Stretch;

            _contentImage = visuals.ContentImagePath != null ? resCache.GetResource<TextureResource>(visuals.ContentImagePath) : null;

            if (visuals.ContentImageModulate != null)
            {
                PaperContent.ModulateSelfOverride = (Color)visuals.ContentImageModulate;
            }
            if (visuals.ContentMargin != null)
            {
                _contentsMargin = (Box2)visuals.ContentMargin;
            }

            if (visuals.HeaderImagePath != null)
            {
                ImageHeader.TexturePath = visuals.HeaderImagePath;
                ImageHeader.MinSize = ImageHeader.TextureNormal?.Size ?? Vector2.Zero;
            }
            if (visuals.HeaderImageModulate != null)
            {
                ImageHeader.ModulateSelfOverride = (Color)visuals.HeaderImageModulate;
            }

            if (visuals.HeaderMargin != null)
            {
                var m = (Box2)visuals.HeaderMargin;
                ImageHeader.Margin = new Thickness(m.Left, m.Top, m.Right, m.Bottom);
            }

            if (visuals.FontAccentColor != null)
            {
                Label.ModulateSelfOverride = Color.FromHex(visuals.FontAccentColor);
            }

            if (visuals.MaxWritableArea != null)
            {
                PaperContent.MinSize = Vector2.Zero;
                PaperContent.MinSize = (Vector2)(visuals.MaxWritableArea);
                PaperContent.MaxSize = (Vector2)(visuals.MaxWritableArea);
            }
        }

        private bool _texturesDirty = false;
        private TextureResource? _paperBorderBackground;
        private Box2 _borderRepeatCenter = new();
        private Box2 _contentsPatch = new();
        private TextureResource? _paperContentBackground;


        /// Good:
        private TextureResource? _backgroundImage;
        private Box2 _backgroundPatchMargin = new();
        private StyleBoxTexture.StretchMode _backgroundImageMode;
        private TextureResource? _contentImage;
        private Box2 _contentsMargin = new();
        private TextureResource? _headerImage;

        private void _updateTextures()
        {
            // For some reason, the UserInterfaceManager.ThemeDefaults.DefaultFont.GetLineHeight(1) == 0
            // So hardcode some reasonable numbers here in case the style is unset. (There must be a better
            // way to get the real font that is used by a RichTextLabel)
            float fontLineHeight = 12;
            float fontDescent = 4;
            if( Label.TryGetStyleProperty<Font>("font", out var font) )
            {
                fontLineHeight = font.GetLineHeight(UIScale);
                fontDescent = font.GetDescent(UIScale);
            }

            if (_backgroundImage != null)
            {
                PaperBackground.PanelOverride = new StyleBoxTexture
                {
                    Texture = _backgroundImage,
                    Mode = _backgroundImageMode,
                    PatchMarginLeft = _backgroundPatchMargin.Left,
                    PatchMarginBottom = _backgroundPatchMargin.Bottom,
                    PatchMarginRight = _backgroundPatchMargin.Right,
                    PatchMarginTop = _backgroundPatchMargin.Top
                };

            }
            else
            {
                PaperBackground.PanelOverride = null;
            }

            if(_contentImage != null)
            {
                var texHeight = _contentImage.Texture.Height;
                PaperContent.PanelOverride = new StyleBoxTexture
                {
                    Texture = _contentImage,
                    Mode = StyleBoxTexture.StretchMode.Tile,
                    // This positions the texture so the font baseline is on the bottom:
                    ExpandMarginTop = fontDescent,
                    // And this scales the texture so that it's a single text line:
                    //Scale = new Vector2(1, fontLineHeight / texHeight)
                };
            }

            PaperContentContainer.Margin = new Thickness(
                    _contentsMargin.Left, _contentsMargin.Top,
                    _contentsMargin.Right, _contentsMargin.Bottom);

            _texturesDirty = false;
        }


        protected override void Draw(DrawingHandleScreen handle)
        {
            if(_texturesDirty)
            {
                _updateTextures();
            }
            base.Draw(handle);
        }

        public void Populate(SharedPaperComponent.PaperBoundUserInterfaceState state)
        {
            bool isEditing = state.Mode == SharedPaperComponent.PaperAction.Write;
            InputContainer.Visible = isEditing;

            var msg = new FormattedMessage();
            // Remove any newlines from the end of the message. There can be a trailing
            // new line at the end of user input, and we would like to display the input
            // box immeditely on the next line.
            msg.AddMarkupPermissive(state.Text.TrimEnd('\r', '\n'));
            Label.SetMessage(msg);

            BlankPaperIndicator.Visible = !isEditing && state.Text.Length == 0;

            StampDisplay.RemoveAllChildren();
            foreach(var stamper in state.StampedBy)
            {
                StampDisplay.AddChild(new StampWidget{ Stamper = stamper });
            }
        }
    }
}
