using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged;

public partial class SharedNewGunSystem
{
    protected virtual void InitializeRevolver()
    {
        SubscribeLocalEvent<SharedRevolverAmmoProviderComponent, ComponentInit>(OnRevolverInit);
        SubscribeLocalEvent<SharedRevolverAmmoProviderComponent, ComponentGetState>(OnRevolverGetState);
        SubscribeLocalEvent<SharedRevolverAmmoProviderComponent, ComponentHandleState>(OnRevolverHandleState);
        SubscribeLocalEvent<SharedRevolverAmmoProviderComponent, TakeAmmoEvent>(OnRevolverTakeAmmo);
        SubscribeLocalEvent<SharedRevolverAmmoProviderComponent, GetVerbsEvent<Verb>>(OnRevolverVerbs);
    }

    private void OnRevolverHandleState(EntityUid uid, SharedRevolverAmmoProviderComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RevolverAmmoProviderComponentState state) return;

        component.CurrentIndex = state.CurrentIndex;
        component.AmmoSlots = state.AmmoSlots;
        component.Chambers = state.Chambers;
    }

    private void OnRevolverGetState(EntityUid uid, SharedRevolverAmmoProviderComponent component, ref ComponentGetState args)
    {
        args.State = new RevolverAmmoProviderComponentState
        {
            CurrentIndex = component.CurrentIndex,
            AmmoSlots = component.AmmoSlots,
            Chambers = component.Chambers,
        };
    }

    private void OnRevolverVerbs(EntityUid uid, SharedRevolverAmmoProviderComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || args.CanInteract) return;

        args.Verbs.Add(new Verb()
        {
            Text = "Spin revolver",
            // Category = VerbCategory.G,
            Act = () => SpinRevolver(component)
        });
    }

    protected abstract void SpinRevolver(SharedRevolverAmmoProviderComponent component, EntityUid? user = null);

    private void OnRevolverTakeAmmo(EntityUid uid, SharedRevolverAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var currentIndex = component.CurrentIndex;
        Cycle(component, args.Shots);

        for (var i = 0; i < args.Shots; i++)
        {
            var index = (currentIndex + i) % component.Capacity;
            var chamber = component.Chambers[index];

            // Get unspawned ent first if possible.
            if (chamber != null)
            {
                if (chamber == true)
                {
                    var ent = Spawn(component.FillPrototype, args.Coordinates);
                    component.Chambers[index] = false;
                    args.Ammo.Add(EnsureComp<NewAmmoComponent>(ent));
                }
            }
            else if (component.AmmoSlots[index] != null)
            {
                var ent = component.AmmoSlots[index]!;
                component.AmmoContainer.Remove(ent.Value);
                component.AmmoSlots[index] = null;
                args.Ammo.Add(EnsureComp<NewAmmoComponent>(ent.Value));
                Transform(ent.Value).Coordinates = args.Coordinates;
            }
        }

        Dirty(component);
    }

    private void Cycle(SharedRevolverAmmoProviderComponent component, int count = 1)
    {
        component.CurrentIndex = (component.CurrentIndex + count) % component.Capacity;
    }

    private void OnRevolverInit(EntityUid uid, SharedRevolverAmmoProviderComponent component, ComponentInit args)
    {
        component.AmmoContainer = Containers.EnsureContainer<Container>(uid, "revolver-ammo");
        component.AmmoSlots = new EntityUid?[component.Capacity];
        component.Chambers = new bool?[component.Capacity];

        if (component.FillPrototype != null)
        {
            for (var i = 0; i < component.Capacity; i++)
            {
                if (component.AmmoSlots[i] != null)
                {
                    component.Chambers[i] = null;
                    continue;
                }

                component.Chambers[i] = true;
            }
        }
    }

    [Serializable, NetSerializable]
    private sealed class RevolverAmmoProviderComponentState : ComponentState
    {
        public int CurrentIndex;
        public EntityUid?[] AmmoSlots = default!;
        public bool?[] Chambers = default!;
    }

    public sealed class RevolverSpinEvent : EntityEventArgs
    {

    }
}
