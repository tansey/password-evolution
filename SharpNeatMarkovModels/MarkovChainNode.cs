using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpNeatMarkovModels
{
    public struct MarkovChainNode
    {
        public string State;
        public double[] TransitionProbabilities;
        public int[] TransitionDestinations;
    }
}
