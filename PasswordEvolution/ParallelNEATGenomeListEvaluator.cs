﻿using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeatMarkovModels;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeatMarkovModels;
using System.Threading.Tasks;
using System.IO;

namespace PasswordEvolution
{
    /// <summary>
    /// A concrete implementation of IGenomeListEvaluator that evaulates genomes independently of each 
    /// other and in parallel (on multiple execution threads).
    /// 
    /// Genome decoding is performed by a provided IGenomeDecoder.
    /// Phenome evaluation is performed by a provided IPhenomeEvaluator.
    /// </summary>
    public class ParallelNEATGenomeListEvaluator<TGenome, TPhenome> : IGenomeListEvaluator<TGenome>
        where TGenome : class, IGenome<TGenome>
        where TPhenome : class
    {
        readonly IGenomeDecoder<NeatGenome, MarkovChain> _genomeDecoder;
        readonly PasswordCrackingEvaluator _passwordCrackingEvaluator;
        PasswordEvolutionExperiment experiment;
        delegate void EvaluationMethod(IList<NeatGenome> genomeList);

        #region Constructors

        /// <summary>
        /// Construct with the provided IGenomeDecoder and IPhenomeEvaluator. 
        /// Phenome caching is enabled by default.
        /// The number of parallel threads defaults to Environment.ProcessorCount.
        /// </summary>
        public ParallelNEATGenomeListEvaluator(IGenomeDecoder<NeatGenome, MarkovChain> genomeDecoder,
                                           PasswordCrackingEvaluator passwordCrackingEvaluator, PasswordEvolutionExperiment experiment)
            : this(genomeDecoder, passwordCrackingEvaluator, true)
        {
            this.experiment = experiment;
        }

    

        /// <summary>
        /// Construct with the provided IGenomeDecoder, IPhenomeEvaluator, ParalleOptions and enablePhenomeCaching flag.
        /// </summary>
        public ParallelNEATGenomeListEvaluator(IGenomeDecoder<NeatGenome, MarkovChain> genomeDecoder,
                                           PasswordCrackingEvaluator passwordCrackingEvaluator, bool hashed = false)
        {
            _genomeDecoder = genomeDecoder;
            _passwordCrackingEvaluator = passwordCrackingEvaluator;
          
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

        public void Evaluate(IList<TGenome> genomeList)
        {
            //EvaluateNeat((IList<NeatGenome>)genomeList);
            EvaluateCondor((IList<NeatGenome>)genomeList);
        }

        /// <summary>
        /// Evaluates a list of genomes. Here we decode each genome in using the contained IGenomeDecoder
        /// and evaluate the resulting TPhenome using the contained IPhenomeEvaluator.
        /// </summary>
        public void EvaluateNeat(IList<NeatGenome> genomeList)
        {
            //Console.WriteLine("Number of genomes right before EvaluateNeat: " + genomeList.Count);
            Parallel.ForEach(genomeList, delegate(NeatGenome genome)
            {
                MarkovChain phenome = _genomeDecoder.Decode(genome);
                if (null == phenome)
                {
                    genome.EvaluationInfo.SetFitness(0.0);
                    genome.EvaluationInfo.AlternativeFitness = 0.0;
                }

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
            });


            foreach (string p in _passwordCrackingEvaluator.FoundPasswords)
            {
                //PasswordCrackingEvaluator.Passwords.Remove(p);
                double val = PasswordCrackingEvaluator.Passwords[p].Reward;
                PasswordCrackingEvaluator.Passwords[p].Reward = val * 0.75;
            }
        }

        /// <summary>
        /// Evaluates a list of genomes. Here we decode each genome in using the contained IGenomeDecoder
        /// and evaluate the resulting TPhenome using the contained IPhenomeEvaluator.
        /// </summary>
        public void EvaluateCondor(IList<NeatGenome> genomeList)
        {
            int totalNumberGenomes = genomeList.Count;
            int numberGenomes = 0;
            string[] finishedFiles = Directory.GetFiles(@"..\..\..\experiments\genomes\genome-finished\", "*.txt"); //finished files used as flags

            // Delete existing finished files that are used as flags
            foreach (string finishedFile in finishedFiles)
            {
                File.Delete(finishedFile);
            }


            // Write genome to file
            Parallel.For(0, genomeList.Count, a =>
            {
                NeatGenome genome = genomeList[a];

                var doc = NeatGenomeXmlIO.SaveComplete(new List<NeatGenome>() { genome }, true);
                doc.Save(@"..\..\..\experiments\genomes\genome-" + a + ".xml");

            });

            GenomeEvaluator.Evaluate(_genomeDecoder, _passwordCrackingEvaluator, experiment);

            /* launch condor */

            do
            {
                string[] flags = Directory.GetFiles(@"..\..\..\experiments\genomes\genome-finished\", "*.txt");
                numberGenomes = flags.Length;
                //System.Threading.Thread.Sleep(1000); //??

            } while (numberGenomes != totalNumberGenomes);


            for (int a = 0; a < genomeList.Count; a++)
            {
                NeatGenome genome = genomeList[a];
                try
                {
                    string name = "genome-" + a + "-finished.txt";
                    StreamReader sr = new StreamReader(@"..\..\..\experiments\genomes\genome-results\genome-" + a + "-results.txt");
                    string line = sr.ReadLine();
                    string[] values = line.Split(' ');
                    genome.EvaluationInfo.SetFitness(double.Parse(values[0]));
                    genome.EvaluationInfo.AlternativeFitness = double.Parse(values[1]);
                }
                catch (System.Exception e)
                {
                    System.Console.WriteLine("File not found.");
                }
            }
          


            foreach (string p in _passwordCrackingEvaluator.FoundPasswords)
            {
                
                double val = PasswordCrackingEvaluator.Passwords[p].Reward;
                PasswordCrackingEvaluator.Passwords[p].Reward = val * 0.75;
            }
        }

        #endregion
    }
}
