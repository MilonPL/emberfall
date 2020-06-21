using System;
using Content.Server.GameObjects.EntitySystems.AI.Steering;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.AI.Operators.Movement
{
    public sealed class MoveToGridOperator : AiOperator
    {
        private readonly IEntity _owner;
        private GridTargetSteeringRequest _request;
        private readonly GridCoordinates _target;
        public float DesiredRange { get; set; }

        public MoveToGridOperator(IEntity owner, GridCoordinates target, float desiredRange = 1.5f)
        {
            _owner = owner;
            _target = target;
            DesiredRange = desiredRange;
        }

        public override bool TryStartup()
        {
            if (!base.TryStartup())
            {
                return true;
            }

            var steering = EntitySystem.Get<AiSteeringSystem>();
            _request = new GridTargetSteeringRequest(_target, DesiredRange);
            steering.Register(_owner, _request);
            return true;
        }
        
        public override void Shutdown(Outcome outcome)
        {
            base.Shutdown(outcome);
            var steering = EntitySystem.Get<AiSteeringSystem>();
            steering.Unregister(_owner);
        }

        public override Outcome Execute(float frameTime)
        {
            switch (_request.Status)
            {
                case SteeringStatus.Pending:
                    return Outcome.Continuing;
                case SteeringStatus.NoPath:
                    return Outcome.Failed;
                case SteeringStatus.Arrived:
                    return Outcome.Success;
                case SteeringStatus.Moving:
                    return Outcome.Continuing;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}