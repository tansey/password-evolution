using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Utility;

namespace SharpNeatMarkovModels
{
    public class MarkovGenomeFactory : NeatGenomeFactory
    {
        #region Constructors
        /// <summary>
        /// Constructs with default NeatGenomeParameters, ID generators initialized to zero and the
        /// provided IActivationFunctionLibrary.
        /// </summary>
        public MarkovGenomeFactory(int inputNeuronCount, int outputNeuronCount,
                                 IActivationFunctionLibrary activationFnLibrary)
            : base(inputNeuronCount, outputNeuronCount, activationFnLibrary)
        {
        }

        /// <summary>
        /// Constructs with the provided IActivationFunctionLibrary and NeatGenomeParameters.
        /// </summary>
        public MarkovGenomeFactory(int inputNeuronCount, int outputNeuronCount,
                                 IActivationFunctionLibrary activationFnLibrary,
                                 NeatGenomeParameters neatGenomeParams)
            : base(inputNeuronCount,outputNeuronCount, activationFnLibrary, neatGenomeParams)
        {
        }

        /// <summary>
        /// Constructs with the provided IActivationFunctionLibrary, NeatGenomeParameters and ID generators.
        /// </summary>
        public MarkovGenomeFactory(int inputNeuronCount, int outputNeuronCount,
                                 IActivationFunctionLibrary activationFnLibrary,
                                 NeatGenomeParameters neatGenomeParams,
                                 UInt32IdGenerator genomeIdGenerator, UInt32IdGenerator innovationIdGenerator)
            : base(inputNeuronCount, outputNeuronCount, activationFnLibrary, neatGenomeParams, genomeIdGenerator, innovationIdGenerator)
        {
        }
        #endregion

        #region Public Methods [NeatGenome Specific / MarkovNeat Overrides]

        /// <summary>
        /// Override that randomly assigns activation functions to neuron's from an activation function library
        /// based on each library item's selection probability.
        /// </summary>
        public override NeuronGene CreateNeuronGene(uint innovationId, NodeType neuronType)
        {
            switch (neuronType)
            {
                case NodeType.Bias:
                case NodeType.Input:
                    {   // Use the ID of the first function. By convention this will be the a sigmoid function in nEAT and RBF-NEAT
                        // but in actual fact bias and input neurons don't use their activation function.
                        int activationFnId = _activationFnLibrary.GetFunctionList()[0].Id;
                        return new NeuronGene(innovationId, neuronType, activationFnId);
                    }
                case NodeType.Output:
                    {
                        int activationFnId = _activationFnLibrary.GetFunctionList().Last().Id;
                        return new NeuronGene(innovationId, neuronType, activationFnId);
                    }
                default:
                    {
                        ActivationFunctionInfo fnInfo = _activationFnLibrary.GetRandomFunction(_rng);
                        IActivationFunction actFn = fnInfo.ActivationFunction;
                        double[] auxArgs = null;
                        if (actFn.AcceptsAuxArgs)
                            auxArgs = actFn.GetRandomAuxArgs(_rng, _neatGenomeParamsCurrent.ConnectionWeightRange);

                        return new NeuronGene(innovationId, neuronType, fnInfo.Id, auxArgs);
                    }
            }
        }

        #endregion
    }
}
