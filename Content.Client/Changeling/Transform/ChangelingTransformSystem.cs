﻿using Content.Shared.Changeling.Transform;

namespace Content.Client.Changeling.Transform;

public sealed class ChangelingTransformSystem : SharedChangelingTransformSystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void OnTransformAction(EntityUid uid,
        ChangelingTransformComponent component,
        ChangelingTransformActionEvent args)
    {

    }
}
