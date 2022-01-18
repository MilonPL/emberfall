using System.Collections.Generic;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem
{
    private void OnBoltFireAttempt(EntityUid uid, BoltActionBarrelComponent component, GunFireAttemptEvent args)
    {
        if (args.Cancelled) return;

        if (component.BoltOpen || component.ChamberContainer.ContainedEntity == null)
            args.Cancel();
    }

    private void OnBoltMapInit(EntityUid uid, BoltActionBarrelComponent component, MapInitEvent args)
    {
        if (component.FillPrototype != null)
        {
            component.UnspawnedCount += component.Capacity;
            if (component.UnspawnedCount > 0)
            {
                component.UnspawnedCount--;
                var chamberEntity = EntityManager.SpawnEntity(component.FillPrototype, EntityManager.GetComponent<TransformComponent>(uid).Coordinates);
                component.ChamberContainer.Insert(chamberEntity);
            }
        }

        UpdateBoltAppearance(component);
    }

    private void UpdateBoltAppearance(BoltActionBarrelComponent component)
    {
        if (!EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearanceComponent)) return;

        appearanceComponent.SetData(BarrelBoltVisuals.BoltOpen, component.BoltOpen);
        appearanceComponent.SetData(AmmoVisuals.AmmoCount, component.ShotsLeft);
        appearanceComponent.SetData(AmmoVisuals.AmmoMax, component.Capacity);
    }

    private void OnBoltInit(EntityUid uid, BoltActionBarrelComponent component, ComponentInit args)
    {
        component.SpawnedAmmo = new Stack<EntityUid>(component.Capacity - 1);
        component.AmmoContainer = uid.EnsureContainer<Container>($"{nameof(component)}-ammo-container", out var existing);

        if (existing)
        {
            foreach (var entity in component.AmmoContainer.ContainedEntities)
            {
                component.SpawnedAmmo.Push(entity);
                component.UnspawnedCount--;
            }
        }

        component.ChamberContainer = uid.EnsureContainer<ContainerSlot>($"{nameof(component)}-chamber-container");

        if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearanceComponent))
        {
            appearanceComponent.SetData(MagazineBarrelVisuals.MagLoaded, true);
        }

        component.Dirty(EntityManager);
        UpdateBoltAppearance(component);
    }

    private void OnBoltUse(EntityUid uid, BoltActionBarrelComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        args.Handled = true;

        if (component.BoltOpen)
        {
            component.BoltOpen = false;
            _popup.PopupEntity(Loc.GetString("bolt-action-barrel-component-bolt-closed"), uid, Filter.Entities(args.User));
            return;
        }

        CycleBolt(component, true);
    }

    private void CycleBolt(BoltActionBarrelComponent component, bool manual = false)
    {
        TryEjectChamber(component);
        TryFeedChamber(component);

        if (component.ChamberContainer.ContainedEntity == null && manual)
        {
            component.BoltOpen = true;

            if (Owner.TryGetContainer(out var container))
            {
                Owner.PopupMessage(container.Owner, Loc.GetString("bolt-action-barrel-component-bolt-opened"));
            }
            return;
        }
        else
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), component._soundCycle.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2));
        }

        component.Dirty(EntityManager);
        UpdateBoltAppearance(component);
    }

    private bool TryEjectChamber(BoltActionBarrelComponent component)
    {
        if (component.ChamberContainer.ContainedEntity is {Valid: true} chambered)
        {
            if (!component.ChamberContainer.Remove(chambered))
                return false;

            if (EntityManager.TryGetComponent(chambered, out AmmoComponent? ammoComponent) && !ammoComponent.Caseless)
                EjectCasing(chambered);

            return true;
        }

        return false;
    }

    private bool TryFeedChamber(BoltActionBarrelComponent component)
    {
        if (component.ChamberContainer.ContainedEntity != null)
        {
            return false;
        }
        if (component.SpawnedAmmo.TryPop(out var next))
        {
            component.AmmoContainer.Remove(next);
            component.ChamberContainer.Insert(next);
            return true;
        }
        else if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            var ammoEntity = EntityManager.SpawnEntity(component.FillPrototype, EntityManager.GetComponent<TransformComponent>(component.Owner).Coordinates);
            component.ChamberContainer.Insert(ammoEntity);
            return true;
        }
        return false;
    }

    private void OnBoltInteractUsing(EntityUid uid, BoltActionBarrelComponent component, InteractUsingEvent args)
    {
        if (args.Handled) return;

        if (TryInsertBullet(args.User, args.Used, component))
            args.Handled = true;
    }

    public bool TryInsertBullet(EntityUid user, EntityUid ammo, BoltActionBarrelComponent component)
    {
        if (!EntityManager.TryGetComponent(ammo, out AmmoComponent? ammoComponent))
        {
            return false;
        }

        if (ammoComponent.Caliber != component.Caliber)
        {
            Owner.PopupMessage(user, Loc.GetString("bolt-action-barrel-component-try-insert-bullet-wrong-caliber"));
            return false;
        }

        if (!component.BoltOpen)
        {
            Owner.PopupMessage(user, Loc.GetString("bolt-action-barrel-component-try-insert-bullet-bolt-closed"));
            return false;
        }

        if (component.ChamberContainer.ContainedEntity == null)
        {
            component.ChamberContainer.Insert(ammo);
            SoundSystem.Play(Filter.Pvs(component.Owner), component._soundInsert.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2));
            component.Dirty(EntityManager);
            UpdateAppearance();
            return true;
        }

        if (component.AmmoContainer.ContainedEntities.Count < component.Capacity - 1)
        {
            component.AmmoContainer.Insert(ammo);
            component.SpawnedAmmo.Push(ammo);
            SoundSystem.Play(Filter.Pvs(Owner), component._soundInsert.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
            component.Dirty(EntityManager);
            UpdateAppearance();
            return true;
        }

        Owner.PopupMessage(user, Loc.GetString("bolt-action-barrel-component-try-insert-bullet-no-room"));

        return false;
    }

    private void OnBoltGetState(EntityUid uid, BoltActionBarrelComponent component, ref ComponentGetState args)
    {
        (int, int)? count = (component.ShotsLeft, component.Capacity);
        var chamberedExists = component.ChamberContainer.ContainedEntity != null;
        // (Is one chambered?, is the bullet spend)
        var chamber = (chamberedExists, false);

        if (chamberedExists && EntityManager.TryGetComponent<AmmoComponent?>(component.ChamberContainer.ContainedEntity!.Value, out var ammo))
        {
            chamber.Item2 = ammo.Spent;
        }

        args.State = new BoltActionBarrelComponentState(
            chamber,
            component.FireRateSelector,
            count,
            component.SoundGunshot.GetSound());
    }

    public EntityUid? PeekBoltAmmo(BoltActionBarrelComponent component)
    {
        return component.ChamberContainer.ContainedEntity;
    }

    public EntityUid? TakeProjectile(BoltActionBarrelComponent component, EntityCoordinates spawnAt)
    {
        if (component._autoCycle)
        {
            CycleBolt(component);
        }
        else
        {
            component.Dirty(EntityManager);
        }

        if (component.ChamberContainer.ContainedEntity is not {Valid: true} chamberEntity) return null;

        var ammoComponent = EntityManager.GetComponentOrNull<AmmoComponent>(chamberEntity);

        return ammoComponent == null ? null : TakeBullet(ammoComponent, spawnAt);
    }
}
