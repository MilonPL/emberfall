﻿using System.Collections.Generic;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Objectives.Requirements
{
    [DataDefinition]
    public class IncompatibleConditionsRequirement : IObjectiveRequirement
    {
        [DataField("conditions")]
        private readonly List<string> _incompatibleConditions = new();

        public bool CanBeAssigned(Mind.Mind mind)
        {
            foreach (var objective in mind.Objectives)
            {
                foreach (var condition in objective.Conditions)
                {
                    foreach (var incompatibleCondition in _incompatibleConditions)
                    {
                        if (incompatibleCondition == condition.GetType().Name) return false;
                    }
                }
            }

            return true;
        }
    }
}
