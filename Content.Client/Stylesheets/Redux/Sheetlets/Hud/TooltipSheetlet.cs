﻿using Content.Client.Examine;
using Content.Client.Stylesheets.Redux.Fonts;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets.Hud;

[CommonSheetlet]
public sealed class TooltipSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var tooltipBox = sheet.GetTexture("tooltip.png").IntoPatch(StyleBox.Margin.All, 2);
        tooltipBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 7);

        var whisperBox = sheet.GetTexture("whisper.png").IntoPatch(StyleBox.Margin.All, 2);
        whisperBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 7);

        return new StyleRule[]
        {
            E<Tooltip>()
                .Prop(Tooltip.StylePropertyPanel, tooltipBox),
            E<PanelContainer>().Class(ExamineSystem.StyleClassEntityTooltip)
                .Panel(tooltipBox),
            E<PanelContainer>().Class("speechBox", "sayBox")
                .Panel(tooltipBox),
            E<PanelContainer>().Class("speechBox", "whisperBox")
                .Panel(whisperBox),

            E<PanelContainer>().Class("speechBox", "whisperBox")
                .ParentOf(E<RichTextLabel>().Class("bubbleContent"))
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12, FontStack.FontKind.Italic)),
            E<PanelContainer>().Class("speechBox", "emoteBox")
                .ParentOf(E<RichTextLabel>().Class("bubbleContent"))
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12, FontStack.FontKind.Italic))
        };
    }
}
