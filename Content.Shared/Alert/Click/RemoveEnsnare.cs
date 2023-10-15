﻿using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using JetBrains.Annotations;

namespace Content.Shared.Alert.Click;
[UsedImplicitly]
[DataDefinition]
public sealed partial class RemoveEnsnare : IAlertClick
{
    public void AlertClicked(EntityUid player)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        if (entManager.TryGetComponent(player, out EnsnareableComponent? ensnareableComponent))
        {
            foreach (var ensnare in ensnareableComponent.Container.ContainedEntities)
            {
                if (!entManager.TryGetComponent(ensnare, out EnsnaringComponent? ensnaringComponent))
                    return;

                entManager.EntitySysManager.GetEntitySystem<SharedEnsnareableSystem>().TryFree(player, player, ensnare, ensnaringComponent);

                // Only one snare at a time.
                break;
            }
        }
    }
}
