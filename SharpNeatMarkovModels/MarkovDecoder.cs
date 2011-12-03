using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Genomes.Neat;
using SharpNeat.Core;
using SharpNeat.Decoders;
using System.Diagnostics;
using SharpNeat.Network;

namespace SharpNeatMarkovModels
{
    public class MarkovDecoder : IGenomeDecoder<NeatGenome,MarkovChain>
    {
        NetworkActivationScheme _scheme;
        IActivationFunctionLibrary _fnLib;

        public MarkovDecoder(NetworkActivationScheme scheme, IActivationFunctionLibrary fnLib)
        {
            _scheme = scheme;
            _fnLib = fnLib;
        }

        public MarkovChain Decode(NeatGenome genome)
        {
            Debug.Assert(genome.InputNodeCount == 1);

            MarkovChainNode[] nodes = new MarkovChainNode[genome.NeuronGeneList.Count];
            List<int>[] dest = new List<int>[nodes.Length];
            List<double>[] probs = new List<double>[nodes.Length];
            Dictionary<uint, int> geneToNode = new Dictionary<uint,int>(); 

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new MarkovChainNode(){ State = getState(genome.NeuronGeneList[i]) };
                geneToNode[genome.NeuronGeneList[i].Id] = i;
            }

            // Do not permit the nodes to go backwards to the non-text generating states
            uint originId = genome.NeuronGeneList[0].Id;
            uint biasId = genome.NeuronGeneList[1].Id;

            for (int i = 0; i < nodes.Length; i++)
            {
                dest[i] = new List<int>();
                probs[i] = new List<double>();

                // Find all connections with this node at the start,
                // filtering out any that may lead to invalid nodes like the
                // bias or the origin.
                var conns = genome.ConnectionList.Where(t => t.SourceNodeId == genome.NeuronGeneList[i].Id 
                                                             && t.TargetNodeId != originId 
                                                             && t.TargetNodeId != biasId
                                                        );
                if (conns == null)
                    continue;

                double sum = conns.Sum(c => Math.Abs(c.Weight));
                foreach (var conn in conns)
                {
                    dest[i].Add(geneToNode[conn.TargetNodeId]);
                    probs[i].Add(Math.Abs(conn.Weight) / sum);
                }
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].TransitionDestinations = dest[i].ToArray();
                nodes[i].TransitionProbabilities = probs[i].ToArray();
            }

            return new MarkovChain(nodes, _scheme.TimestepsPerActivation);
        }

        private string getState(NeuronGene neuronGene)
        {
            return ((IMarkovActivationFunction)(_fnLib.GetFunction(neuronGene.ActivationFnId))).State;
        }

    }
}
