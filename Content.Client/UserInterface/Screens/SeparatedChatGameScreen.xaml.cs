using Content.Client.UserInterface.Systems.Chat.Widgets;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Screens;

[GenerateTypedNameReferences]
public sealed partial class SeparatedChatGameScreen : BaseGameScreen
{
    public SeparatedChatGameScreen()
    {
        RobustXamlLoader.Load(this);

        AutoscaleMaxResolution = new Vector2i(1080, 770);

        SetAnchorPreset(ScreenContainer, LayoutPreset.Wide);
        SetAnchorPreset(ViewportContainer, LayoutPreset.Wide);
        SetAnchorPreset(MainViewport, LayoutPreset.Wide);
        SetAnchorAndMarginPreset(Actions, LayoutPreset.BottomLeft, margin: 10);
        SetAnchorAndMarginPreset(Ghost, LayoutPreset.BottomWide, margin: 80);
        SetAnchorAndMarginPreset(Hotbar, LayoutPreset.BottomWide, margin: 5);
        SetAnchorAndMarginPreset(Alerts, LayoutPreset.CenterRight, margin: 10);

        ScreenContainer.OnSplitResizeFinish += (first, second) => OnChatResized!(second);
    }

    public override ChatBox ChatBox => GetWidget<ChatBox>()!;
    public override ScreenType ScreenType => ScreenType.Separated;

    public override void SetChatSize(Vector2 size)
    {
        ScreenContainer.GetChild(1).Measure(size);
    }
}
