﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Domains;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Core;
using SharpNeat.DistanceMetrics;
using SharpNeat.SpeciationStrategies;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Decoders;
using System.Threading.Tasks;
using System.Xml;
using SharpNeatMarkovModels;
using SharpNeat.Network;
using System.IO;

namespace PasswordEvolution
{
    public class PasswordEvolutionExperiment
    {
        NeatEvolutionAlgorithmParameters _eaParams;
        NeatGenomeParameters _neatGenomeParams;
        string _name;
        int _populationSize;
        int _specieCount;
        NetworkActivationScheme _activationScheme;
        string _complexityRegulationStr;
        int? _complexityThreshold;
        string _description;
        ParallelOptions _parallelOptions;
        string[] _states;
        int _guesses;
        Dictionary<string, PasswordInfo> _passwords;
        IActivationFunctionLibrary _activationFnLibrary;
        PasswordCrackingEvaluator _evaluator;
        int _outputs;

        #region Properties
        public Dictionary<string, PasswordInfo> Passwords
        {
            get { return _passwords; }
            set { _passwords = value; }
        }

        /// <summary>
        /// Gets the name of the experiment.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets human readable explanatory text for the experiment.
        /// </summary>
        public string Description
        {
            get { return _description; }
        }

        /// <summary>
        /// Gets the number of inputs required by the MC that the underlying problem domain is based on.
        /// </summary>
        public int InputCount
        {
            get { return 1; }
        }

        /// <summary>
        /// Gets the number of outputs required by the MC that the underlying problem domain is based on.
        /// </summary>
        public int OutputCount
        {
            get { return _outputs; }
            set { _outputs = value; }
        }

        /// <summary>
        /// Gets the default population size to use for the experiment.
        /// </summary>
        public int DefaultPopulationSize
        {
            get { return _populationSize; }
        }

        /// <summary>
        /// Gets the NeatEvolutionAlgorithmParameters to be used for the experiment. Parameters on this object can be 
        /// modified. Calls to CreateEvolutionAlgorithm() make a copy of and use this object in whatever state it is in 
        /// at the time of the call.
        /// </summary>
        public NeatEvolutionAlgorithmParameters NeatEvolutionAlgorithmParameters
        {
            get { return _eaParams; }
        }

        /// <summary>
        /// Gets the NeatGenomeParameters to be used for the experiment. Parameters on this object can be modified. Calls
        /// to CreateEvolutionAlgorithm() make a copy of and use this object in whatever state it is in at the time of the call.
        /// </summary>
        public NeatGenomeParameters NeatGenomeParameters
        {
            get { return _neatGenomeParams; }
        }

        public PasswordCrackingEvaluator Evaluator
        {
            get { return _evaluator; }
        }
        public int GuessesPerIndividual { get { return _guesses; } set { _guesses = value; } }
        public int ValidationGuesses { get; set; }
        public bool Hashed { get; set; }
        #endregion

        /// <summary>
        /// Initialize the experiment with some optional XML configutation data.
        /// </summary>
        public void Initialize(string name, XmlElement xmlConfig)
        {
            _name = name;
            _populationSize = XmlUtils.GetValueAsInt(xmlConfig, "PopulationSize");
            _specieCount = XmlUtils.GetValueAsInt(xmlConfig, "SpecieCount");
            _activationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "Activation");
            _complexityThreshold = XmlUtils.TryGetValueAsInt(xmlConfig, "ComplexityThreshold");
            _description = XmlUtils.TryGetValueAsString(xmlConfig, "Description");
            _parallelOptions = ExperimentUtils.ReadParallelOptions(xmlConfig);

            _guesses = XmlUtils.GetValueAsInt(xmlConfig, "Guesses");
            Hashed = XmlUtils.TryGetValueAsBool(xmlConfig, "Hashed").HasValue ? XmlUtils.GetValueAsBool(xmlConfig, "Hashed") : false;
            ValidationGuesses = XmlUtils.GetValueAsInt(xmlConfig, "ValidationGuesses");

            // Load the passwords from file
            string pwdfile = XmlUtils.TryGetValueAsString(xmlConfig, "ValidationPasswordFile");
            if (pwdfile != null)
            {
                Console.Write("Loading passwords from [{0}]...", pwdfile);
                if (_passwords == null || _passwords.Count == 0)
                {
                    int? pwLength = XmlUtils.TryGetValueAsInt(xmlConfig, "PasswordLength");
                    if (pwLength.HasValue)
                        Console.Write("Filtering to {0}-character passwords...", pwLength.Value);
                    _passwords = PasswordUtil.LoadPasswords(pwdfile, pwLength);
                }
                else
                    Console.WriteLine("WARNING: Not loading passwords for experiment (already set)");
            }
            else
                Console.WriteLine("WARNING: Not loading passwords for experiment (not provided in config file)");
            _eaParams = new NeatEvolutionAlgorithmParameters();
            _eaParams.SpecieCount = _specieCount;
            _neatGenomeParams = new NeatGenomeParameters();
            _neatGenomeParams.FeedforwardOnly = false;
            _neatGenomeParams.AddNodeMutationProbability = 0.03;
            _neatGenomeParams.AddConnectionMutationProbability = 0.05;

            // TODO: Load states from XML config file
            // Generates all the valid states in the MC using all viable ASCII characters
            var stateList = new List<string>();
            for (uint i = 32; i < 127; i++)
                stateList.Add(((char)i).ToString());
            stateList.Add(null);
            _states = stateList.ToArray();
            _activationFnLibrary = MarkovActivationFunctionLibrary.CreateLibraryMc(_states);
        }

        

        /// <summary>
        /// Load a population of genomes from an XmlReader and returns the genomes in a new list.
        /// The genome factory for the genomes can be obtained from any one of the genomes.
        /// </summary>
        public List<NeatGenome> LoadPopulation(XmlReader xr)
        {
            NeatGenomeFactory genomeFactory = (NeatGenomeFactory)CreateGenomeFactory();
            return NeatGenomeXmlIO.ReadCompleteGenomeList(xr, true, genomeFactory);
        }

        /// <summary>
        /// Save a population of genomes to an XmlWriter.
        /// </summary>
        public void SavePopulation(XmlWriter xw, IList<NeatGenome> genomeList)
        {
            // Writing node IDs is not necessary for NEAT.
            NeatGenomeXmlIO.WriteComplete(xw, genomeList, true);
        }

        /// <summary>
        /// Create a genome decoder for the experiment.
        /// </summary>
        public IGenomeDecoder<NeatGenome, MarkovChain> CreateGenomeDecoder()
        {
            return new MarkovDecoder(_activationScheme, _activationFnLibrary);
        }

        /// <summary>
        /// Create a genome factory for the experiment.
        /// Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
        /// </summary>
        public IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            
            return new MarkovGenomeFactory(InputCount, OutputCount, _activationFnLibrary, _neatGenomeParams);
        }

        /// <summary>
        /// Create and return a NeatEvolutionAlgorithm object ready for running the NEAT algorithm/search. Various sub-parts
        /// of the algorithm are also constructed and connected up.
        /// Uses the experiments default population size defined in the experiment's config XML.
        /// </summary>
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeListEvaluator<NeatGenome> eval = null)
        {
            return CreateEvolutionAlgorithm(_populationSize, eval);
        }

        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(List<NeatGenome> seeds, IGenomeListEvaluator<NeatGenome> eval = null)
        {
            // Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
            IGenomeFactory<NeatGenome> genomeFactory = CreateGenomeFactory();

            // Create an initial population of randomly generated genomes.
            List<NeatGenome> genomeList = genomeFactory.CreateGenomeList(_populationSize, 0, seeds);

            return CreateEvolutionAlgorithm(genomeFactory, genomeList);
        }

        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(NeatGenome seed, IGenomeListEvaluator<NeatGenome> eval = null)
        {
            // Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
            IGenomeFactory<NeatGenome> genomeFactory = CreateGenomeFactory();

            // Create an initial population of randomly generated genomes.
            List<NeatGenome> genomeList = genomeFactory.CreateGenomeList(_populationSize, 0, seed);

            return CreateEvolutionAlgorithm(genomeFactory, genomeList, eval);
        }


        /// <summary>
        /// Create and return a NeatEvolutionAlgorithm object ready for running the NEAT algorithm/search. Various sub-parts
        /// of the algorithm are also constructed and connected up.
        /// This overload accepts a population size parameter that specifies how many genomes to create in an initial randomly
        /// generated population.
        /// </summary>
        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize, IGenomeListEvaluator<NeatGenome> eval = null)
        {
            // Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
            IGenomeFactory<NeatGenome> genomeFactory = CreateGenomeFactory();

            // Create an initial population of randomly generated genomes.
            List<NeatGenome> genomeList = genomeFactory.CreateGenomeList(populationSize, 0);

            // Create evolution algorithm.
            return CreateEvolutionAlgorithm(genomeFactory, genomeList, eval);
        }

        public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList, IGenomeListEvaluator<NeatGenome> eval = null)
        {
            // Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
            IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
            ISpeciationStrategy<NeatGenome> speciationStrategy = new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, _parallelOptions);

            // Create complexity regulation strategy.
            IComplexityRegulationStrategy complexityRegulationStrategy = new NullComplexityRegulationStrategy();// ExperimentUtils.CreateComplexityRegulationStrategy(_complexityRegulationStr, _complexityThreshold);

            // Create the evolution algorithm.
            NeatEvolutionAlgorithm<NeatGenome> ea = new NeatEvolutionAlgorithm<NeatGenome>(_eaParams, speciationStrategy, complexityRegulationStrategy);

            // Create the MC evaluator
            PasswordCrackingEvaluator.Passwords = _passwords;

            // Create genome decoder.
            IGenomeDecoder<NeatGenome, MarkovChain> genomeDecoder = CreateGenomeDecoder();

            // If we're running specially on Condor, skip this
            if (eval == null)
            {
                _evaluator = new PasswordCrackingEvaluator(_guesses, Hashed);

                // Create a genome list evaluator. This packages up the genome decoder with the genome evaluator.
                //    IGenomeListEvaluator<NeatGenome> innerEvaluator = new ParallelGenomeListEvaluator<NeatGenome, MarkovChain>(genomeDecoder, _evaluator, _parallelOptions);
                IGenomeListEvaluator<NeatGenome> innerEvaluator = new ParallelNEATGenomeListEvaluator<NeatGenome, MarkovChain>(genomeDecoder, _evaluator, this);

                /*
                // Wrap the list evaluator in a 'selective' evaulator that will only evaluate new genomes. That is, we skip re-evaluating any genomes
                // that were in the population in previous generations (elite genomes). This is determiend by examining each genome's evaluation info object.
                IGenomeListEvaluator<NeatGenome> selectiveEvaluator = new SelectiveGenomeListEvaluator<NeatGenome>(
                                                                                        innerEvaluator,
                                                                                        SelectiveGenomeListEvaluator<NeatGenome>.CreatePredicate_OnceOnly());
                */


                // Initialize the evolution algorithm.
                ea.Initialize(innerEvaluator, genomeFactory, genomeList);
            }
            else
                // Initialize the evolution algorithm.
                ea.Initialize(eval, genomeFactory, genomeList);

            

            // Finished. Return the evolution algorithm
            return ea;
        }

        
        /* UNCOMMENT THIS TO ENABLE GUI
        /// <summary>
        /// Create a System.Windows.Forms derived object for displaying genomes.
        /// </summary>
        public AbstractGenomeView CreateGenomeView()
        {
            return new NeatGenomeView();
        }

        /// <summary>
        /// Create a System.Windows.Forms derived object for displaying output for a domain (e.g. show best genome's output/performance/behaviour in the domain). 
        /// </summary>
        public AbstractDomainView CreateDomainView()
        {
            return null;
        }
        */
    }
}
