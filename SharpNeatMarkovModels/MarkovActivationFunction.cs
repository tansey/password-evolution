using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Network;
using SharpNeat;

namespace SharpNeatMarkovModels
{
    public class MarkovActivationFunction : IMarkovActivationFunction
    {
        // TODO: Do we want to make the state be an auxilary arg
        //       and allow it to mutate?
        string _state;

        #region Constructors
        public MarkovActivationFunction(string state)
        {
            _state = state;
        }
        #endregion

        public string State { get { return _state; } }

        #region IActivationFunction properties
        public bool AcceptsAuxArgs
        {
            get { return false; }
        }

        public float Calculate(float x, float[] auxArgs)
        {
            return 0;
        }

        public double Calculate(double x, double[] auxArgs)
        {
            return 0;
        }

        public string FunctionDescription
        {
            get { return "Appends " + _state + " to the output string."; }
        }

        public string FunctionId
        {
            get { return "MC-" + _state; }
        }

        public string FunctionString
        {
            get { return _state; }
        }

        
        public double[] GetRandomAuxArgs(SharpNeat.Utility.FastRandom rng, double connectionWeightRange)
        {
            throw new SharpNeatException("GetRandomAuxArgs() called on activation function that does not use auxiliary arguments.");
        }

        public void MutateAuxArgs(double[] auxArgs, SharpNeat.Utility.FastRandom rng, SharpNeat.Utility.ZigguratGaussianSampler gaussianSampler, double connectionWeightRange)
        {
            throw new SharpNeatException("MutateAuxArgs() called on activation function that does not use auxiliary arguments.");
        }
        #endregion
    }
}
