﻿using Content.Client.Stylesheets.Redux.Sheetlets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.StylesheetHelpers;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.NTSheetlets;

public sealed class PalettedWindowSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var buttonCfg = (IButtonConfig) sheet;

        var headerStylebox = new StyleBoxTexture
        {
            Texture = sheet.GetTexture("window_header.png"),
            PatchMarginBottom = 3,
            ExpandMarginBottom = 3,
            ContentMarginBottomOverride = 0
        };

        // TODO: This would probably be better palette-based but we can leave it for now.
        var headerAlertStylebox = new StyleBoxTexture
        {
            Texture = sheet.GetTexture("window_header_alert.png"),
            PatchMarginBottom = 3,
            ExpandMarginBottom = 3,
            ContentMarginBottomOverride = 0
        };

        var backgroundStylebox = new StyleBoxTexture()
        {
            Texture = sheet.GetTexture("window_background.png")
        };
        backgroundStylebox.SetPatchMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);
        backgroundStylebox.SetExpandMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);

        var borderedBackgroundStylebox = new StyleBoxTexture
        {
            Texture = sheet.GetTexture("window_background_bordered.png"),
        };
        borderedBackgroundStylebox.SetPatchMargin(StyleBox.Margin.All, 2);

        var closeButtonTex = sheet.GetTexture("cross.svg.png");

        var rules = new List<StyleRule>()
        {
            Element().Class(DefaultWindow.StyleClassWindowPanel)
                .Panel(backgroundStylebox),
            Element().Class(DefaultWindow.StyleClassWindowHeader)
                .Panel(headerStylebox),
            Element().Class(StyleClasses.AlertWindowHeader)
                .Panel(headerAlertStylebox),
            Element().Class(StyleClasses.BorderedWindowPanel)
                .Panel(borderedBackgroundStylebox),
            E<TextureButton>().Class(DefaultWindow.StyleClassWindowCloseButton)
                .Prop(TextureButton.StylePropertyTexture, closeButtonTex)
                .Margin(3),
        };

        NTButtonSheetlet.MakeButtonRules(buttonCfg, rules, buttonCfg.NegativeButtonPalette, DefaultWindow.StyleClassWindowCloseButton);

        return rules.ToArray();
    }
}

public interface IPanelPalette : ISheetletConfig
{
    /// <summary>
    ///     Color used for window backgrounds.
    /// </summary>
    public Color BackingPanelPalette { get; }
}
