using System.Collections.Generic;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    /// <summary>
    /// Bolt-action rifles
    /// </summary>
    [RegisterComponent]
    public sealed class PumpBarrelComponent : ServerRangedBarrelComponent, IMapInit
    {
        public override string Name => "PumpBarrel";

        public override int ShotsLeft
        {
            get
            {
                var chamberCount = _chamberContainer.ContainedEntity != null ? 1 : 0;
                return chamberCount + _spawnedAmmo.Count + _unspawnedCount;
            }
        }

        public override int Capacity => _capacity;
        private int _capacity;
        
        // Even a point having a chamber? I guess it makes some of the below code cleaner
        private ContainerSlot _chamberContainer;
        private Stack<IEntity> _spawnedAmmo;
        private Container _ammoContainer;

        private BallisticCaliber _caliber;

        private string _fillPrototype;
        private int _unspawnedCount;
        
        private bool _manualCycle;

        private AppearanceComponent _appearanceComponent;
        
        // Sounds
        private SoundComponent _soundComponent;
        private string _soundCycle;
        private string _soundInsert;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref _capacity, "capacity", 6);
            serializer.DataField(ref _fillPrototype, "fillPrototype", null);
            serializer.DataField(ref _manualCycle, "manualCycle", true);

            serializer.DataField(ref _soundCycle, "soundCycle", "/Audio/Guns/Cock/sf_rifle_cock.ogg");
            serializer.DataField(ref _soundInsert, "soundInsert", "/Audio/Guns/MagIn/bullet_insert.ogg");
            
            _spawnedAmmo = new Stack<IEntity>(_capacity - 1);
        }
        
        void IMapInit.MapInit()
        {
            if (_fillPrototype != null)
            {
                _unspawnedCount += Capacity - 1;
            }
            UpdateAppearance();
        }

        public override void Initialize()
        {
            base.Initialize();
            
            _ammoContainer =
                ContainerManagerComponent.Ensure<Container>($"{Name}-ammo-container", Owner, out var existing);

            if (existing)
            {
                foreach (var entity in _ammoContainer.ContainedEntities)
                {
                    _spawnedAmmo.Push(entity);
                    _unspawnedCount--;
                }
            }

            _chamberContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-chamber-container", Owner);

            if (Owner.TryGetComponent(out SoundComponent soundComponent))
            {
                _soundComponent = soundComponent;
            }
            
            if (Owner.TryGetComponent(out AppearanceComponent appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }
            
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            _appearanceComponent?.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoMax, Capacity);
        }

        public override IEntity PeekAmmo()
        {
            return _chamberContainer.ContainedEntity;
        }

        public override IEntity TakeProjectile()
        {
            var chamberEntity = _chamberContainer.ContainedEntity;
            if (!_manualCycle)
            {
                Cycle();
            }
            return chamberEntity?.GetComponent<AmmoComponent>().TakeBullet();
        }

        private void Cycle(bool manual = false)
        {
            var chamberedEntity = _chamberContainer.ContainedEntity;
            if (chamberedEntity != null)
            {
                _chamberContainer.Remove(chamberedEntity);
                var ammoComponent = chamberedEntity.GetComponent<AmmoComponent>();
                if (!ammoComponent.Caseless)
                {
                    EjectCasing(chamberedEntity);   
                }
            }

            if (_spawnedAmmo.TryPop(out var next))
            {
                _ammoContainer.Remove(next);
                _chamberContainer.Insert(next);
            }

            if (_unspawnedCount > 0)
            {
                _unspawnedCount--;
                var ammoEntity = Owner.EntityManager.SpawnEntity(_fillPrototype, Owner.Transform.GridPosition);
                _chamberContainer.Insert(ammoEntity);
            }

            if (manual)
            {
                if (_soundCycle != null)
                {
                    _soundComponent.Play(_soundCycle, AudioParams.Default.WithVolume(-3));
                }
            }
            
            // Dirty();
            UpdateAppearance();
        }

        public bool TryInsertBullet(AttackByEventArgs eventArgs)
        {
            if (!eventArgs.AttackWith.TryGetComponent(out AmmoComponent ammoComponent))
            {
                return false;
            }

            if (ammoComponent.Caliber != _caliber)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Wrong caliber"));
                return false;
            }

            if (_chamberContainer.ContainedEntity == null)
            {
                _chamberContainer.Insert(eventArgs.AttackWith);
                // Dirty();
                UpdateAppearance();
                if (_soundInsert != null)
                {
                    _soundComponent?.Play(_soundInsert);
                }
                return true;
            }

            if (_ammoContainer.ContainedEntities.Count < Capacity - 1)
            {
                _ammoContainer.Insert(eventArgs.AttackWith);
                _spawnedAmmo.Push(eventArgs.AttackWith);
                // Dirty();
                UpdateAppearance();
                if (_soundInsert != null)
                {
                    _soundComponent?.Play(_soundInsert);
                }
                return true;
            }
            
            Owner.PopupMessage(eventArgs.User, Loc.GetString("No room"));
            
            return false;
        }

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            Cycle(true);
            return true;
        }

        public override bool AttackBy(AttackByEventArgs eventArgs)
        {
            return TryInsertBullet(eventArgs);
        }
    }
}