#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.NodeContainer.NodeGroups
{
    public interface IPipeNet : INodeGroup, IGasMixtureHolder
    {
        /// <summary>
        ///     Causes gas in the PipeNet to react.
        /// </summary>
        void Update();
    }

    [NodeGroup(NodeGroupID.Pipe)]
    public class PipeNet : BaseNodeGroup, IPipeNet
    {
        [ViewVariables] public GasMixture Air { get; set; } = new() {Temperature = Atmospherics.T20C};

        [ViewVariables] private readonly List<PipeNode> _pipes = new();

        [ViewVariables] private AtmosphereSystem? _atmosphereSystem;

        [ViewVariables]
        private IGridAtmosphereComponent? GridAtmos =>
            _atmosphereSystem?.GetGridAtmosphere(GridId);

        public override Color VisColor => Color.Cyan;

        public override void Initialize(Node sourceNode)
        {
            base.Initialize(sourceNode);

            _atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();
            GridAtmos?.AddPipeNet(this);
        }

        public void Update()
        {
            Air.React(this);
        }

        public override void LoadNodes(List<Node> groupNodes)
        {
            base.LoadNodes(groupNodes);

            foreach (var node in groupNodes)
            {
                var pipeNode = (PipeNode) node;
                _pipes.Add(pipeNode);
                pipeNode.JoinPipeNet(this);
                Air.Volume += pipeNode.Volume;
            }
        }

        public override void RemoveNode(Node node)
        {
            base.RemoveNode(node);

            var pipeNode = (PipeNode) node;
            Air.Volume -= pipeNode.Volume;
            // TODO: Bad O(n^2)
            _pipes.Remove(pipeNode);
        }

        public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
        {
            RemoveFromGridAtmos();

            var buffer = new GasMixture(Air.Volume) {Temperature = Air.Temperature};

            foreach (var newGroup in newGroups)
            {
                if (newGroup.Key is not IPipeNet newPipeNet)
                    continue;

                var newAir = newPipeNet.Air;
                var newVolume = newGroup.Cast<PipeNode>().Sum(n => n.Volume);

                buffer.Clear();
                buffer.Merge(Air);
                buffer.Multiply(MathF.Min(newVolume / Air.Volume, 1f));
                newAir.Merge(buffer);
            }
        }

        private void RemoveFromGridAtmos()
        {
            GridAtmos?.RemovePipeNet(this);
        }
    }
}
