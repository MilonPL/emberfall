using Content.Shared.Chemistry;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.Shared.GameObjects.EntitySystems
{
    public class ChemicalReactionSystem : EntitySystem
    {
        private IEnumerable<ReactionPrototype> _reactions;

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            _reactions = _prototypeManager.EnumeratePrototypes<ReactionPrototype>();
        }

        /// <summary>
        /// Checks if a solution has the reactants required to cause a specified reaction.
        /// </summary>
        /// <param name="solution">The solution to check for reaction conditions.</param>
        /// <param name="reaction">The reaction whose reactants will be checked for in the solution.</param>
        /// <param name="unitReactions">The number of times the reaction can occur with the given solution.</param>
        /// <returns></returns>
        public bool SolutionValidReaction(Solution solution, ReactionPrototype reaction, out ReagentUnit unitReactions)
        {
            unitReactions = ReagentUnit.MaxValue; //Set to some impossibly large number initially
            foreach (var reactant in reaction.Reactants)
            {
                if (!solution.ContainsReagent(reactant.Key, out ReagentUnit reagentQuantity))
                {
                    return false;
                }
                var currentUnitReactions = reagentQuantity / reactant.Value.Amount;
                if (currentUnitReactions < unitReactions)
                {
                    unitReactions = currentUnitReactions;
                }
            }

            if (unitReactions == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
