﻿using Content.Server.Chemistry.PlantMetabolism;
using Content.Shared.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    public class AffectGrowth : PlantAdjustAttribute
    {
        public override void Metabolize(IEntity plantHolder, Solution.ReagentQuantity amount)
        {
            if (!CanMetabolize(plantHolder, out var plantHolderComp))
                return;

            plantHolderComp.AffectGrowth((int) Amount);
        }
    }
}
