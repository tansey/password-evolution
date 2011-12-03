using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Phenomes.NeuralNets;
using SharpNeat.Network;
using SharpNeat.Utility;

namespace SharpNeatMarkovModels
{
    public class MarkovChain
    {
        MarkovChainNode[] _nodes;
        int _stepsPerActivation;
        RouletteWheelLayout[] _rouletteWheels;
        FastRandom _random;

        public MarkovChain(MarkovChainNode[] nodes, int stepsPerActivation)
            : this(nodes, stepsPerActivation, new FastRandom())
        {
            
        }

        public MarkovChain(MarkovChainNode[] nodes, int stepsPerActivation, FastRandom random)
        {
            _nodes = nodes;
            _stepsPerActivation = stepsPerActivation;
            _random = random;

            _rouletteWheels = new RouletteWheelLayout[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
                _rouletteWheels[i] = new RouletteWheelLayout(nodes[i].TransitionProbabilities);
        }

        /// <summary>
        /// Activates the Markov chain once and returns the resulting string.
        /// </summary>
        public string Activate()
        {
            int stateIdx = 0;
            string s = "";
            for (int i = 0; i < _stepsPerActivation; i++)
            {
                if (_rouletteWheels[stateIdx].Probabilities.Length == 0)
                    return s;
                s += _nodes[RouletteWheel.SingleThrow(_rouletteWheels[stateIdx], _random)].State;
            }

            return s;
        }
    }
}

