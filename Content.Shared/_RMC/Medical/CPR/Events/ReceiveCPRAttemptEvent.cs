using Content.Shared.Inventory;

namespace Content.Shared._RMC.Medical.CPR.Events;

// ReSharper disable InconsistentNaming
[ByRefEvent]
public record struct ReceiveCPRAttemptEvent(
    EntityUid Performer,
    EntityUid Target,
    bool Start,
    SlotFlags TargetSlots = SlotFlags.MASK,
    bool Cancelled = false) : IInventoryRelayEvent;
