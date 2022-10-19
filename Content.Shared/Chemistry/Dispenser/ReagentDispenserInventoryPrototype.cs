﻿using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Chemistry.Dispenser
{
    /// <summary>
    /// Is simply a list of reagents defined in yaml. This can then be set as a
    /// <see cref="SharedReagentDispenserComponent"/>s <c>pack</c> value (also in yaml),
    /// to define which reagents it's able to dispense. Based off of how vending
    /// machines define their inventory.
    /// </summary>
    [Serializable, NetSerializable, Prototype("reagentDispenserInventory")]
    public readonly record struct ReagentDispenserInventoryPrototype : IPrototype
    {
        [DataField("inventory", customTypeSerializer: typeof(PrototypeIdListSerializer<ReagentPrototype>))]
        private readonly List<string> _inventory = new();

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        public List<string> Inventory => _inventory;
    }
}
