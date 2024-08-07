﻿using Content.Shared.Chemistry.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SolutionContainerComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string> SolutionIds = new(SharedSolutionSystem.SolutionAlloc);

    [DataField, AutoNetworkedField]
    public List<EntityUid> SolutionEntities = new(SharedSolutionSystem.SolutionAlloc);
}
