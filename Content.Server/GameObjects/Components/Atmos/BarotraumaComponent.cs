﻿using System.Runtime.CompilerServices;
using CannyFastMath;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Atmos;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Mobs;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NFluidsynth;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Logger = Robust.Shared.Log.Logger;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    ///     Barotrauma: injury because of changes in air pressure.
    /// </summary>
    [RegisterComponent]
    public class BarotraumaComponent : Component
    {
        public override string Name => "Barotrauma";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(float frameTime)
        {
            if (!Owner.TryGetComponent(out DamageableComponent damageable)) return;
            Owner.TryGetComponent(out ServerStatusEffectsComponent status);

            var coordinates = Owner.Transform.GridPosition;
            var gridAtmos = EntitySystem.Get<AtmosphereSystem>().GetGridAtmosphere(coordinates.GridID);
            var tile = gridAtmos?.GetTile(coordinates);

            var pressure = 1f;
            var highPressureMultiplier = 1f;
            var lowPressureMultiplier = 1f;

            foreach (var protection in Owner.GetAllComponents<IPressureProtection>())
            {
                highPressureMultiplier *= protection.HighPressureMultiplier;
                lowPressureMultiplier *= protection.LowPressureMultiplier;
            }

            if (tile?.Air != null)
                pressure = MathF.Max(tile.Air.Pressure, 1f);

            switch (pressure)
            {
                // Low pressure.
                case var _ when pressure <= Atmospherics.WarningLowPressure:
                    pressure *= lowPressureMultiplier;

                    if(pressure > Atmospherics.WarningLowPressure)
                        goto default;

                    // TODO ATMOS Uncomment this when saltern is pressurized
                    //damageable.TakeDamage(DamageType.Brute, Atmospherics.LowPressureDamage, Owner, null);

                    if (status == null) break;

                    if (pressure <= Atmospherics.HazardLowPressure)
                    {
                        status.ChangeStatusEffect(StatusEffect.Pressure, "/Textures/Interface/StatusEffects/Pressure/lowpressure2.png", null);
                        break;
                    }

                    status.ChangeStatusEffect(StatusEffect.Pressure, "/Textures/Interface/StatusEffects/Pressure/lowpressure1.png", null);
                    break;

                // High pressure.
                case var _ when pressure >= Atmospherics.WarningHighPressure:
                    pressure *= highPressureMultiplier;

                    if(pressure < Atmospherics.WarningHighPressure)
                        goto default;

                    var damage = (int) MathF.Min((pressure / Atmospherics.HazardHighPressure) * Atmospherics.PressureDamageCoefficient, Atmospherics.MaxHighPressureDamage);

                    // TODO ATMOS Uncomment this when saltern is pressurized
                    //damageable.TakeDamage(DamageType.Brute, damage, Owner, null);

                    if (status == null) break;

                    if (pressure >= Atmospherics.HazardHighPressure)
                    {
                        status.ChangeStatusEffect(StatusEffect.Pressure, "/Textures/Interface/StatusEffects/Pressure/highpressure2.png", null);
                        break;
                    }

                    status.ChangeStatusEffect(StatusEffect.Pressure, "/Textures/Interface/StatusEffects/Pressure/highpressure1.png", null);
                    break;

                // Normal pressure.
                default:
                    status?.RemoveStatusEffect(StatusEffect.Pressure);
                    break;
            }

        }
    }
}
