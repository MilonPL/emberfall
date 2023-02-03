﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Systems.Medical.Controls;

[GenerateTypedNameReferences]
public sealed partial class WoundList : ScrollContainer
{
    private SortedDictionary<EntityUid, WoundEntry> _wounds = new();

    public WoundList()
    {
        RobustXamlLoader.Load(this);
    }

    public bool AddWound(EntityUid woundEntity, string woundName, string severityString)
    {
        var woundEntry = new WoundEntry();
        woundEntry.WoundName.Text = woundName;
        woundEntry.WoundSeverity.Text = severityString;
        if (!_wounds.TryAdd(woundEntity, woundEntry))
            return false;
        Children.Add(woundEntry);
        return true;
    }

    public bool RemoveWound(EntityUid woundEntity)
    {
        if (!_wounds.TryGetValue(woundEntity, out var woundEntry))
            return false;
        RemoveChild(woundEntry);
        _wounds.Remove(woundEntity);
        return true;
    }
}
