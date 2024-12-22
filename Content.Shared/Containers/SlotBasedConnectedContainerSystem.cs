using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Containers;

/// <summary>
/// System for getting container that is linked to subject entity. Container is supposed to be present in certain character slot.
/// Can be used for linking ammo feeder, solution source for spray nozzle, etc.
/// </summary>
public class SlotBasedConnectedContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    /// <summary>
    /// Try get connected container entity in character slots for <see cref="uid"/>.
    /// </summary>
    /// <param name="uid">
    /// Entity for which connected container is required. If <see cref="SlotBasedConnectedContainerComponent"/>
    /// is used - tries to find container in slot, returns false and null <see cref="slotEntity"/> otherwise. 
    /// </param>
    /// <param name="slotEntity">Found connected container entity or null.</param>
    /// <returns>True if connected container was found, false otherwise.</returns>
    public bool TryGetConnectedContainer(EntityUid uid, [NotNullWhen(true)] out EntityUid? slotEntity)
    {
        if (!TryComp<SlotBasedConnectedContainerComponent>(uid, out var component))
        {
            slotEntity = null;
            return false;
        }

        return TryGetConnectedContainer(uid, component.TargetSlot, component.ContainerWhitelist, out slotEntity);
    }

    private bool TryGetConnectedContainer(EntityUid uid, SlotFlags slotFlag, EntityWhitelist? providerWhitelist, [NotNullWhen(true)] out EntityUid? slotEntity)
    {
        slotEntity = null;

        if (!_containers.TryGetContainingContainer((uid, null, null), out var container))
            return false;

        var user = container.Owner;
        if (!_inventory.TryGetContainerSlotEnumerator(user, out var enumerator, slotFlag))
            return false;

        while (enumerator.NextItem(out var item))
        {
            if (_whitelistSystem.IsWhitelistFailOrNull(providerWhitelist, item))
                continue;

            slotEntity = item;
            return true;
        }

        return false;
    }
}
