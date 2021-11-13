﻿using System.Collections.Generic;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Objectives.Requirements
{
    [DataDefinition]
    public class IncompatibleObjectivesRequirement : IObjectiveRequirement
    {
        [DataField("objectives")]
        private readonly List<string> _incompatibleObjectives = new();

        public bool CanBeAssigned(Mind.Mind mind)
        {
            foreach (var objective in mind.Objectives)
            {
                foreach (var incompatibleObjective in _incompatibleObjectives)
                {
                    if (incompatibleObjective == objective.Prototype.ID) return false;
                }
            }

            return true;
        }
    }
}
