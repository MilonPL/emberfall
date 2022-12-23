﻿using Content.Server.Botany.Systems;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    public sealed class PlantAffectGrowth : PlantAdjustAttribute
    {
        public override void Effect(ref ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            var plantHolder = args.EntityManager.System<PlantHolderSystem>();

            plantHolder.AffectGrowth(args.SolutionEntity, (int) Amount, plantHolderComp);
        }
    }
}
