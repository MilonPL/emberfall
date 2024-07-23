﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Systems.Actions.Windows;

[GenerateTypedNameReferences]
public sealed partial class ActionsWindow : DefaultWindow
{
    public MultiselectOptionButton<Filters> FilterButton { get; private set; }

    /// <summary>
    /// Whether the displayed actions or search filter needs updating.
    /// </summary>
    public bool UpdateNeeded;

    public ActionsWindow()
    {
        RobustXamlLoader.Load(this);

        SearchContainer.AddChild(FilterButton = new MultiselectOptionButton<Filters>
        {
            Label = Loc.GetString("ui-actionmenu-filter-button")
        });

        foreach (var filter in Enum.GetValues<Filters>())
        {
            FilterButton.AddItem(Loc.GetString($"ui-actionmenu-{filter.ToString().ToLower()}"), filter);
        }
    }

    public enum Filters
    {
        Enabled,
        Item,
        Innate,
        Instant,
        Targeted
    }
}
