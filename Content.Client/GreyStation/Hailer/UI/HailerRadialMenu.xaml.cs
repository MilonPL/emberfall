using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Controls;
using Content.Shared.GreyStation.Hailer;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using System.Numerics;

namespace Content.Client.GreyStation.Hailer.UI;

[GenerateTypedNameReferences]
public sealed partial class HailerRadialMenu : RadialMenu
{
    public event Action<uint>? OnLinePicked;

    public HailerRadialMenu(EntityUid owner, IEntityManager entMan, IPlayerManager playerMan, SharedHailerSystem hailer, SpriteSystem sprite)
    {
        RobustXamlLoader.Load(this);

        var ent = (owner, entMan.GetComponent<HailerComponent>(owner));
        if (playerMan.LocalSession?.AttachedEntity is not {} user)
            return;

        var lines = hailer.GetLines(ent, user);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var button = new RadialMenuTextureButton()
            {
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(64f, 64f)
            };

            // TODO: scream texture
            var tex = new TextureRect()
            {
                VerticalAlignment = VAlignment.Center,
                HorizontalAlignment = HAlignment.Center,
                Texture = sprite.Frame0(line.Icon),
                TextureScale = new Vector2(2f, 2f),
            };

            button.AddChild(tex);

            // don't want to capture i since it becomes lines.Count and is useless
            var index = (uint) i;
            button.OnButtonUp += _ => OnLinePicked?.Invoke(index);

            Container.AddChild(button);
        }
    }
}
