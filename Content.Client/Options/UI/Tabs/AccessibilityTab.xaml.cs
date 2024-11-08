using Content.Shared.CCVar;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Options.UI.Tabs;

[GenerateTypedNameReferences]
public sealed partial class AccessibilityTab : Control
{
    public AccessibilityTab()
    {
        RobustXamlLoader.Load(this);

        Control.AddOptionCheckBox(CCVars.ChatEnableColorName, EnableColorNameCheckBox);
        Control.AddOptionCheckBox(CCVars.AccessibilityColorblindFriendly, ColorblindFriendlyCheckBox);
        Control.AddOptionCheckBox(CCVars.ReducedMotion, ReducedMotionCheckBox);
        Control.AddOptionPercentSlider(CCVars.ChatWindowOpacity, ChatWindowOpacitySlider);
        Control.AddOptionPercentSlider(CCVars.ScreenShakeIntensity, ScreenShakeIntensitySlider);
        Control.AddOptionPercentSlider(CCVars.SpeechBubbleOpacitySlider, SpeechBubbleOpacitySlider);
        Control.AddOptionPercentSlider(CCVars.SpeechTextAndNameOpacitySlider, SpeechTextAndNameOpacitySlider);

        Control.Initialize();
    }
}

