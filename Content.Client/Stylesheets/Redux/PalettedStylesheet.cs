﻿using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Resources;
using Content.Client.Stylesheets.Redux.Fonts;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.StylesheetHelpers;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux;

/// <summary>
///     The base class for all stylesheets, providing core functionality and helpers.
/// </summary>
[PublicAPI]
public abstract partial class PalettedStylesheet : BaseStylesheet
{
    protected PalettedStylesheet(object config) : base(config)
    {
    }

    public StyleRule[] BaseRules()
    {
        var rules = new List<StyleRule>()
        {
            Element()
                .Prop(StyleProperties.PrimaryPalette, PrimaryPalette)
                .Prop(StyleProperties.SecondaryPalette, SecondaryPalette)
                .Prop(StyleProperties.PositivePalette, PositivePalette)
                .Prop(StyleProperties.NegativePalette, NegativePalette)
                .Prop(StyleProperties.HighlightPalette, HighlightPalette)
        };

        var palettes = new List<(string, Color[])>
        {
            (StyleClasses.PrimaryColor, PrimaryPalette),
            (StyleClasses.SecondaryColor, SecondaryPalette),
            (StyleClasses.PositiveColor, PositivePalette),
            (StyleClasses.NegativeColor, NegativePalette),
            (StyleClasses.HighlightColor, HighlightPalette),
        };

        foreach (var (styleclass, palette) in palettes)
        {
            for (uint i = 0; i < palette.Length; i++)
            {
                rules.Add(
                    Element().Class(StyleClasses.GetColorClass(styleclass, i))
                        .Modulate(palette[i])
                    );
            }
        }

        return rules.ToArray();
    }

    public StyleRule[] GetSheetletRules(Type sheetletTy)
    {
        return GetSheetletRules<PalettedStylesheet>(sheetletTy);
    }

    public StyleRule[] GetSheetletRules<T>()
        where T: Sheetlet<PalettedStylesheet>
    {
        return GetSheetletRules<T, PalettedStylesheet>();
    }

    public StyleRule[] GetAllSheetletRules<T>()
        where T: Attribute
    {
        return GetAllSheetletRules<PalettedStylesheet, T>();
    }
}
