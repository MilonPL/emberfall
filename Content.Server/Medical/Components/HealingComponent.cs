using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;

namespace Content.Server.Medical.Components
{
    [RegisterComponent]
    public class HealingComponent : Component, IAfterInteract
    {
        public override string Name => "Healing";

        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        // This also requires changing the dictionary type, and removing a _prototypeManager.Index() call.
        // TODO QUESTION Would protype references be able to deal with this, loading a Dictionary<DamagePrototype, int> from the yaml file?
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [DataField("heal", required: true )]
        public Dictionary<string, int> Heal { get; private set; } = new();

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            if (!eventArgs.Target.TryGetComponent(out IDamageableComponent? damageable))
            {
                return true;
            }

            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(eventArgs.User))
            {
                return true;
            }

            if (eventArgs.User != eventArgs.Target &&
                !eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
            {
                return true;
            }

            if (Owner.TryGetComponent<SharedStackComponent>(out var stack) && !EntitySystem.Get<StackSystem>().Use(Owner.Uid, stack, 1))
            {
                return true;
            }

            foreach (var (damageTypeID, amount) in Heal)
            {
                damageable.ChangeDamage(_prototypeManager.Index<DamageTypePrototype>(damageTypeID), -amount, true);
            }

            return true;
        }
    }
}
