using Content.Client.Stylesheets.Redux.Sheetlets;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.Stylesheets;

public sealed partial class InterfaceStylesheet : IButtonConfig
{
    ResPath IButtonConfig.BaseButtonTexturePath => new("button.svg.96dpi.png");
    ResPath IButtonConfig.OpenLeftButtonTexturePath => new("button.svg.96dpi.png");
    ResPath IButtonConfig.OpenRightButtonTexturePath => new("button.svg.96dpi.png");
    ResPath IButtonConfig.OpenBothButtonTexturePath => new("button.svg.96dpi.png");
    Color[] IButtonConfig.ButtonPalette => PrimaryPalette;
    Color[] IButtonConfig.PositiveButtonPalette => PositivePalette;
    Color[] IButtonConfig.NegativeButtonPalette => NegativePalette;
}
