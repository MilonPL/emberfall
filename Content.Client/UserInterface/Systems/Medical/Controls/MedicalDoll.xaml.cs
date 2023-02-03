﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Systems.Medical.Controls;

[GenerateTypedNameReferences]
public sealed partial class MedicalDoll : BoxContainer
{
    public MedicalDoll()
    {
        RobustXamlLoader.Load(this);
    }

    public void UpdateUI(SpriteView? dummy)
    {
        if (dummy == null)
            return;
        FrontDummy.Sprite = dummy.Sprite;
        BackDummy.Sprite = dummy.Sprite;
        FrontDummy.OverrideDirection = Direction.South;
        BackDummy.OverrideDirection = Direction.North;
    }
}
