using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Silicons.StationAi;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Timing;

namespace Content.Client.Silicons.StationAi;

[GenerateTypedNameReferences]
public sealed partial class StationAiMenu : RadialMenu
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public event Action<BaseStationAiAction>? OnAiRadial;

    private EntityUid _tracked;

    public StationAiMenu()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);
    }

    public void Track(EntityUid owner)
    {
        _tracked = owner;

        if (!_entManager.EntityExists(_tracked))
        {
            Close();
            return;
        }

        BuildButtons();
        UpdatePosition();
    }

    private void BuildButtons()
    {
        var ev = new GetStationAiRadialEvent();
        _entManager.EventBus.RaiseLocalEvent(_tracked, ref ev);

        var main = FindControl<RadialContainer>("Main");
        main.DisposeAllChildren();
        var sprites = _entManager.System<SpriteSystem>();

        foreach (var action in ev.Actions)
        {
            // TODO: This radial boilerplate is quite annoying
            var button = new StationAiMenuButton(action.Event)
            {
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(64f, 64f),
                ToolTip = action.Tooltip != null ? Loc.GetString(action.Tooltip) : null,
            };

            if (action.Sprite != null)
            {
                var tex = new TextureRect
                {
                    VerticalAlignment = VAlignment.Center,
                    HorizontalAlignment = HAlignment.Center,
                    Texture = sprites.Frame0(action.Sprite),
                    TextureScale = new Vector2(2f, 2f),
                };

                button.AddChild(tex);
            }

            button.OnPressed += args =>
            {
                OnAiRadial?.Invoke(action.Event);
                Close();
            };
            main.AddChild(button);
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (!_entManager.TryGetComponent(_tracked, out TransformComponent? xform))
        {
            Close();
            return;
        }

        if (!xform.Coordinates.IsValid(_entManager))
        {
            Close();
            return;
        }

        var coords = _eyeManager.CoordinatesToScreen(xform.Coordinates);

        if (!coords.IsValid)
        {
            Close();
            return;
        }

        OpenScreenAt(coords.Position, _clyde);
    }
}

public sealed class StationAiMenuButton(BaseStationAiAction action) : RadialMenuTextureButton
{
    public BaseStationAiAction Action = action;
}
