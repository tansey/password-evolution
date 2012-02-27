/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2006, 2009-2010 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * SharpNEAT is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with SharpNEAT.  If not, see <http://www.gnu.org/licenses/>.
 */
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeatMarkovModels;
//using PasswordEvolution;
//using namespace SharpNeat.Core;
namespace PasswordEvolution
//namespace SharpNeat.Core
{
    /// <summary>
    /// A concrete implementation of IGenomeListEvaluator that evaulates genomes independently of each 
    /// other and in parallel (on multiple execution threads).
    /// 
    /// Genome decoding is performed by a provided IGenomeDecoder.
    /// Phenome evaluation is performed by a provided IPhenomeEvaluator.
    /// </summary>
    public class SerialGenomeListEvaluator<TGenome, TPhenome> : IGenomeListEvaluator<TGenome>
        where TGenome : class, IGenome<TGenome>
        where TPhenome : class
    {
        readonly IGenomeDecoder<NeatGenome, MarkovChain> _genomeDecoder;
        readonly PasswordCrackingEvaluator _passwordCrackingEvaluator;
        
        readonly EvaluationMethod _evalMethod;

        delegate void EvaluationMethod(IList<NeatGenome> genomeList);

        #region Constructors

        /// <summary>
        /// Construct with the provided IGenomeDecoder and IPhenomeEvaluator. 
        /// Phenome caching is enabled by default.
        /// The number of parallel threads defaults to Environment.ProcessorCount.
        /// </summary>
        public SerialGenomeListEvaluator(IGenomeDecoder<NeatGenome, MarkovChain> genomeDecoder,
                                           PasswordCrackingEvaluator passwordCrackingEvaluator)
            : this(genomeDecoder, passwordCrackingEvaluator, true)
        {
        }

    

        /// <summary>
        /// Construct with the provided IGenomeDecoder, IPhenomeEvaluator, ParalleOptions and enablePhenomeCaching flag.
        /// </summary>
        public SerialGenomeListEvaluator(IGenomeDecoder<NeatGenome, MarkovChain> genomeDecoder,
                                           PasswordCrackingEvaluator passwordCrackingEvaluator, bool hashed = false)
        {
            _genomeDecoder = genomeDecoder;
            _passwordCrackingEvaluator = passwordCrackingEvaluator;

            _evalMethod = Evaluate_Serial;
            // Determine the appropriate evaluation method.
          
        }

        #endregion

        #region IGenomeListEvaluator<TGenome> Members

        /// <summary>
        /// Gets the total number of individual genome evaluations that have been performed by this evaluator.
        /// </summary>
        public ulong EvaluationCount
        {
            get { return _passwordCrackingEvaluator.EvaluationCount; }
        }

        /// <summary>
        /// Gets a value indicating whether some goal fitness has been achieved and that
        /// the the evolutionary algorithm/search should stop. This property's value can remain false
        /// to allow the algorithm to run indefinitely.
        /// </summary>
        public bool StopConditionSatisfied
        {
            get { return _passwordCrackingEvaluator.StopConditionSatisfied; }
        }

        /// <summary>
        /// Reset the internal state of the evaluation scheme if any exists.
        /// </summary>
        public void Reset()
        {
            _passwordCrackingEvaluator.Reset();
        }

        /// <summary>
        /// Evaluates a list of genomes. Here we decode each genome in using the contained IGenomeDecoder
        /// and evaluate the resulting TPhenome using the contained IPhenomeEvaluator.
        /// </summary>
        public void Evaluate(IList<TGenome> genomeList)
        {
            _evalMethod((IList<NeatGenome>)genomeList);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Main genome evaluation loop with no phenome caching (decode on each loop).
        /// </summary>
        private void Evaluate_Serial(IList<NeatGenome> genomeList)
        {
            foreach (NeatGenome genome in genomeList)
            {
                MarkovChain phenome = _genomeDecoder.Decode(genome);
                if (null == phenome)
                {   // Non-viable genome.
                    genome.EvaluationInfo.SetFitness(0.0);
                    genome.EvaluationInfo.AlternativeFitness = 0.0;
                }
                else
                {
                    FitnessInfo fitnessInfo = _passwordCrackingEvaluator.Evaluate(phenome);
                    genome.EvaluationInfo.SetFitness(fitnessInfo._fitness);
                    genome.EvaluationInfo.AlternativeFitness = fitnessInfo._alternativeFitness;
                }

            }

            foreach (string p in _passwordCrackingEvaluator.FoundPasswords)
            {
                //PasswordCrackingEvaluator.Passwords.Remove(p);
                double val = PasswordCrackingEvaluator.Passwords[p];
                PasswordCrackingEvaluator.Passwords[p] = val * 0.75;
            }
        }

        #endregion
    }
}
