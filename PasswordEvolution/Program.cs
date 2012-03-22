using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SharpNeat.Genomes.Neat;
using SharpNeat.EvolutionAlgorithms;
using System.Threading;
using System.IO;
using System.Security;
using SharpNeat.Utility;
using SharpNeat.Core;
using SharpNeatMarkovModels;
using System.Threading.Tasks;
//
using SharpNeat.Domains;
//

namespace PasswordEvolution
{
    class Program
    {
        static PasswordEvolutionExperiment _experiment;
        static NeatEvolutionAlgorithm<NeatGenome> _ea;
        const string CHAMPION_FILE_ROOT = @"..\..\..\experiments\champions\champion"; // To be used: append ("gen_{0}.xml", _gens)
        const string CHAMPION_FILE = @"..\..\..\experiments\champion.xml";
        const string CONFIG_FILE = @"..\..\..\experiments\config.xml";
        const string SEED_FILE = @"..\..\..\experiments\seed.xml";
        const string PASSWORD_MODEL_FILE = @"..\..\..\passwords\phpbb-withcount.txt";
        const string PASSWORD_FILE = @"..\..\..\passwords\phpbb-withcount.txt";
        const string RESULTS_FILE = @"..\..\..\experiments\phpbb_myspace-filtered_results.csv";
        const string TEST_MARKOV_CONFIG_FILE = @"..\..\..\experiments\test_relaxing.xml";
        const string TEST_MARKOV_SEED = @"..\..\..\experiments\test_markov_seed.xml";
        const string TEST_MARKOV_PASSWORDS = @"..\..\..\passwords\test_markov.txt";
        const string ENGLISH_WORDS = @"..\..\..\passwords\english.txt";// only 5 of 350k words have numbers
        const string MORPHED_ENGLISH_WORDS = @"..\..\..\passwords\morphed_english.txt";//10% of all words have numbers
        const string FORCED_MORPHED_ENGLISH_WORDS = @"..\..\..\passwords\forced_morphed_english.txt";//all words required to have at least one number
        const string MORPHED_SEED_FILE = @"..\..\..\experiments\morphed_seed.xml";
        const string MORPHED_CONFIG_FILE = @"..\..\..\experiments\morphed.config.xml";
        const string MORPHED_RESULTS_FILE = @"..\..\..\experiments\morphed_results.csv";
        const string HASHED_CONFIG_FILE = @"..\..\..\experiments\hashed.config.xml";
        const string HASHED_PASSWORDS_FILE = @"..\..\..\passwords\battlefield_heroes.txt";
        const string HASHED_RESULTS_FILE = @"..\..\..\experiments\hashed_results.csv";
        const string FILTERED_MYSPACE_PASSWORDS = @"..\..\..\passwords\myspace-filtered-withcount.txt";
        //const int VALIDATION_GUESSES = 1000000000; // config.xml
        const int VALIDATION_GUESSES = 1000000; // mini-project.config.xml

        const int MAX_GENERATIONS = 10;

        const bool VALIDATE_ALL_STAR = false;

        const string PHPBB_DATASET = @"..\..\..\passwords\phpbb-withcount.txt";
        const string PHPBB_SEED_FILE = @"..\..\..\experiments\phpbb_seed.xml";
        const string PHPBB_CONFIG_FILE = @"..\..\..\experiments\mini-project.config.xml";
        const string PHPBB_RESULTS_FILE = @"..\..\..\experiments\phpbb_results.csv";

        // For the toyDistributionSet
        const string TOY_DISTRIBUTION_CONFIG_FILE = @"..\..\..\experiments\mini-project.config.xml";

        static void Main(string[] args)
        {
            /////////////////////////////////////////////////////////////////////////////////////////
            // Below are some examples of possible experiments and functions you may wish to call. //
            /////////////////////////////////////////////////////////////////////////////////////////


            // Morph an english word dictionary into a password database where 10% of passwords have a number
            // ToyProblemUtil.MorphEnglish(ENGLISH_WORDS, MORPHED_ENGLISH_WORDS);

            // Morph an english word dictionary into a password database where all passwords are at least 8 characters
            // and contain at least one number
            // ToyProblemUtil.MorphEnglish(ENGLISH_WORDS, FORCED_MORPHED_ENGLISH_WORDS, requireDigit: true, minLength: 8);

            // Train on the no-rule morphed english words db and evolve against the morphed english db with 
            // the digit and length creation rules enforced.
            // RunExperiment(MORPHED_ENGLISH_WORDS, MORPHED_SEED_FILE, MORPHED_CONFIG_FILE, MORPHED_RESULTS_FILE, false);


            //Train on the phppb dataset and evolve against the rockyou dataset
            //RunExperiment(PHPBB_DATASET, PHPBB_SEED_FILE, PHPBB_CONFIG_FILE, PHPBB_RESULTS_FILE, false);

            //Train on the toyDistribution dataset and evolve against the toyDistribution dataset
            RunExperiment(TOY_DISTRIBUTION_CONFIG_FILE, false);

            // Print some summary statistics about the distribution of passwords in the two morphed english dictionaries.
            // PasswordUtil.PrintStats(@"..\..\..\passwords\morphed_english.txt"); // no creation rules
            // PasswordUtil.PrintStats(@"..\..\..\passwords\forced_morphed_english.txt"); // digit and length rules

            // Run a really big analysis comparing the first-order Markov model to an 8-layered one.
            // PrepareMarkovModelRuns();
            // Parallel.For(0, _datasetFilenames.Length, i => RunAllMarkovModelPairs(i));

            // Check if a database of hashed passwords contains some common passwords (check for creation rules)
            // MD5HashChecker md5 = new MD5HashChecker(@"..\..\..\passwords\stratfor_hashed.txt");
            // md5.PrintCounts();
        }

        /// <summary>
        /// Trains a Markov model on a the training set of passwords, then evolves it against the target password database
        /// specified in the config file. At the end of the evolution, the champion model is evaluated for a larger number
        /// of guesses.
        /// </summary>
        /// <param name="trainingSetFile">The file containing the passwords from which to build the initial Markov model.</param>
        /// <param name="seedFile">The file to which the initial Markov model will be saved.</param>
        /// <param name="configFile">The file containing all the configuration parameters of the evolution.</param>
        /// <param name="resultsFile">The file to which the results will be saved at each generation.</param>
        /// <param name="validateSeed">If true, the seed model will first be validated against a large number of guesses.</param>
        //private static void RunExperiment(string trainingSetFile, string seedFile, string configFile, string resultsFile, bool validateSeed = false)
        private static void RunExperiment(string configFile, bool validateSeed = false)
        {
            Console.Write("Building Markov model...");
            
            // Load the XML configuration file
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(configFile);
            XmlElement xmlConfigElement = xmlConfig.DocumentElement;

            // Set Training File
            string trainingSetFile = XmlUtils.GetValueAsString(xmlConfigElement, "TrainingFile");

            // Create seedFile
            string seedFile = XmlUtils.GetValueAsString(xmlConfigElement, "SeedFile");

            // Create results file.
            string resultsFile = XmlUtils.GetValueAsString(xmlConfigElement, "ResultsFile");

            Console.WriteLine("\nTraining File: {0}\nSeed File: {1}\nResults File: {2}", trainingSetFile, seedFile, resultsFile);
            

            // Load the training set passwords from file
            var passwords = PasswordUtil.LoadPasswords(trainingSetFile, 8); 
            
            // Create a Markov model from the passwords. This model will be used
            // as our seed for the evolution.
            int outputs = MarkovFilterCreator.GenerateFirstOrderMarkovFilter(seedFile, passwords);

            // Free up the memory used by the passwords
            passwords = null;

            Console.WriteLine("Done! Outputs: {0}", outputs);

            _experiment = new PasswordEvolutionExperiment();
            _experiment.OutputCount = outputs;
            
            // Initialize the experiment with the specifications in the config file.
            _experiment.Initialize("PasswordEvolution", xmlConfig.DocumentElement);

            // Set the passwords to be used by the fitness evaluator.
            // These are the passwords our models will try to guess.
            // PasswordsWithAccounts is the file used for validation. Its account values won't be changed.
            PasswordCrackingEvaluator.Passwords = _experiment.Passwords;

            Console.WriteLine("Loading seed...");
            
            // Load the seed model that we created at the start of this function
            var seed = _experiment.LoadPopulation(XmlReader.Create(seedFile))[0];

            // Validates the seed model by running it for a large number of guesses 
            if (validateSeed)
            {
                Console.WriteLine("Validating seed model...");
                var seedModel = _experiment.CreateGenomeDecoder().Decode(seed);
                ValidateModel(seedModel, _experiment.Passwords, VALIDATION_GUESSES, _experiment.Hashed);
            }

            // Create evolution algorithm using the seed model to initialize the population
            Console.WriteLine("Creating population...");
            _ea = _experiment.CreateEvolutionAlgorithm(seed);

            // Attach an update event handler. This will be called at the end of every generation
            // to log the progress of the evolution (see function logEvolutionProgress below).
            _ea.UpdateEvent += new EventHandler(logEvolutionProgress);
            //_ea.UpdateScheme = new UpdateScheme(1);//.UpdateMode.
            
            // Setup results file
            using (TextWriter writer = new StreamWriter(resultsFile))
                writer.WriteLine("Generation,Champion Accounts,Champion Uniques,Average Accounts,Average Uniques,Total Accounts,Total Uniques");
            _generationalResultsFile = resultsFile;

            // Start algorithm (it will run on a background thread).
            Console.WriteLine("Starting evolution. Pop size: {0} Guesses: {1}", _experiment.DefaultPopulationSize, _experiment.GuessesPerIndividual);
            _ea.StartContinue();

            // Wait until the evolution is finished.
            while (_ea.RunState == RunState.Running) { Thread.Sleep(1000); }

            if (VALIDATE_ALL_STAR)
            {
                // Validate the champions of each generation.
                List<MarkovChain> championModels = new List<MarkovChain>();
                for (int i = 0; i < MAX_GENERATIONS; i++)
                {
                    var currentChamp = _experiment.LoadPopulation(XmlReader.Create(CHAMPION_FILE_ROOT + "_gen_" + i + ".xml"))[0];
                    var champModel = _experiment.CreateGenomeDecoder().Decode(currentChamp);
                    championModels.Add(champModel);
                }
                ValidateAllstarTeam(championModels, _experiment.Passwords, VALIDATION_GUESSES, _experiment.Hashed);
            }
            else
            {
                // Validate the resulting model.
                var decoder = _experiment.CreateGenomeDecoder();
                var champ = decoder.Decode(_ea.CurrentChampGenome);
                ValidateModel(champ, _experiment.Passwords, VALIDATION_GUESSES, _experiment.Hashed);
            }

        }


        static void ValidateAllstarTeam(List<MarkovChain> models, Dictionary<string, PasswordInfo> passwords, int guesses, bool hashed) //passwords here is not used...
        {
           // Console.WriteLine("Number of champion models: {0}", models.Count);
            Console.WriteLine("Validating All-Star Team on {0} guesses...", guesses);
            PasswordCrackingEvaluator eval = new PasswordCrackingEvaluator(guesses, hashed);
            eval.ValidatePopulation(models);
            double accounts = eval.FoundValidationPasswords.Sum(s => PasswordCrackingEvaluator.Passwords[s].Accounts);
            double uniques = eval.FoundValidationPasswords.Count;
            Console.WriteLine("Accounts: {0} Uniques: {1}", accounts, uniques);
        }

        static void ValidateModel(MarkovChain model, Dictionary<string, PasswordInfo> passwords, int guesses, bool hashed ) //passwords here is not used...
        {
            Console.Write("Validating on {0} guesses... ", guesses);
            PasswordCrackingEvaluator eval = new PasswordCrackingEvaluator(guesses, hashed);
            var results = eval.Validate(model);
            Console.WriteLine("Accounts: {0} Uniques: {1}", results._fitness, results._alternativeFitness);
        }

        static int _gens = 0;
        static string _generationalResultsFile;

        /// <summary>
        /// This method is called at the end of every generation and logs the progress of the EA.
        /// </summary>
        static void logEvolutionProgress(object sender, EventArgs e)
        {
            //Console.WriteLine("Number of genomes at the start of logEvol: " + _ea.GenomeList.Count);
            var maxFitness = _ea.GenomeList.Max(g => g.EvaluationInfo.Fitness);
            var maxAltFitness = _ea.GenomeList.Max(g => g.EvaluationInfo.AlternativeFitness);
            var genChampion = _ea.GenomeList.First(g => g.EvaluationInfo.Fitness == maxFitness);
            // Write the results to file.
            using (TextWriter writer = new StreamWriter(_generationalResultsFile, true))
            {
                Console.WriteLine("Gen {0}: {1} ({2}) Total: {3}", _ea.CurrentGeneration,
                                                            maxFitness,
                                                            maxAltFitness,
                                                            _experiment.Evaluator.FoundPasswords.Count);

                lock (_experiment.Evaluator.FoundPasswords)
                {
                    Console.WriteLine("{0},{1},{2},{3},{4},{5},{6}", _ea.CurrentGeneration,
                                                                maxFitness,
                                                                maxAltFitness,
                                                                _ea.GenomeList.Average(g => g.EvaluationInfo.Fitness),
                                                                _ea.GenomeList.Average(g => g.EvaluationInfo.AlternativeFitness),
                                                                _experiment.Evaluator.FoundPasswords.Sum(s => PasswordCrackingEvaluator.Passwords[s].Accounts),
                                                                _experiment.Evaluator.FoundPasswords.Count);
                    writer.WriteLine("{0},{1},{2},{3},{4},{5},{6}", _ea.CurrentGeneration,
                                                                    maxFitness,
                                                                    maxAltFitness,
                                                                    _ea.GenomeList.Average(g => g.EvaluationInfo.Fitness),
                                                                    _ea.GenomeList.Average(g => g.EvaluationInfo.AlternativeFitness),
                                                                    _experiment.Evaluator.FoundPasswords.Sum(s => PasswordCrackingEvaluator.Passwords[s].Accounts),
                                                                    _experiment.Evaluator.FoundPasswords.Count);
                }
                Console.WriteLine("Done.");
            }
          
            // Save the best genome to file
            var doc = NeatGenomeXmlIO.SaveComplete(new List<NeatGenome>() { genChampion }, true);
            doc.Save(CHAMPION_FILE_ROOT + "_gen_" + _gens + ".xml");

            // If we've reached the maximum number of generations,
            // tell the algorithm to stop.
            if (_gens >= MAX_GENERATIONS)
                _ea.Stop();

            _gens++;
        }

        #region Code to run the static model comparison of first-order vs. layered
        // TODO: Clean up and refactor this entire section.

        static Dictionary<string, PasswordInfo>[] _passwords;
        static PasswordDatasetInfo[] _datasetFilenames;
        static object _writerLock = new object();

        // Loads all the passwords and the configuration file.
        static void PrepareMarkovModelRuns()
        {
            const string PASSWORD_OFFSET = @"..\..\..\passwords\";
            _datasetFilenames = new PasswordDatasetInfo[]
            {
                //new PasswordDataset(){ Filename = "faithwriters-withcount.txt", Name = "faithwriters" },
                //new PasswordDataset(){ Filename = "myspace-filtered-withcount.txt", Name = "myspace" },
                //new PasswordDataset(){ Filename = "phpbb-withcount.txt", Name = "phpbb" },
                //new PasswordDataset(){ Filename = "rockyou-withcount.txt", Name = "rockyou" },
                //new PasswordDataset(){ Filename = "singles.org-withcount.txt", Name = "singles.org" },
                new PasswordDatasetInfo() { Filename = "morphed_english.txt", Name = "training" },
                new PasswordDatasetInfo() { Filename = "forced_morphed_english.txt", Name = "testing" }
            };

            Console.WriteLine("Loading all {0} password datasets...", _datasetFilenames.Length);
            _passwords = new Dictionary<string, PasswordInfo>[_datasetFilenames.Length];
            for (int i = 0; i < _passwords.Length; i++)
            {
                Console.WriteLine(_datasetFilenames[i].Name);
                _passwords[i] = PasswordUtil.LoadPasswords(PASSWORD_OFFSET + _datasetFilenames[i].Filename);
            }
            Console.WriteLine("Done.");

            _experiment = new PasswordEvolutionExperiment();

            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(CONFIG_FILE);
            _experiment.Passwords = _passwords[0];
            _experiment.Initialize("PasswordEvolution", xmlConfig.DocumentElement);

            using (TextWriter writer = new StreamWriter(@"..\..\..\experiments\summary_results.csv"))
                writer.WriteLine("TrainingSet,TestingSet,Accounts Cracked,Passwords Cracked,% Accounts,% Passwords");
        }

        // Runs a comparison of the two model types.
        static void RunAllMarkovModelPairs(object special)
        {
            const string EXPERIMENT_OFFSET = @"..\..\..\experiments\intermediate\";
            string[] models = new string[]
            {
                "first-order",
                "8-layer"
            };
            
            // For every dataset, create a model
            for (int i = 0; i < _datasetFilenames.Length; i++)
            {
                if (i != (int)special)
                    continue;
                for (int m = 0; m < 2; m++)
                {
                    int outputs;
                    string seedFile = EXPERIMENT_OFFSET + "seed-" + models[m] + "-" + _datasetFilenames[i].Name + ".xml";
                    Console.Write("Building {0} Markov model...", models[m]);
                    if (m == 0)
                        outputs = MarkovFilterCreator.GenerateFirstOrderMarkovFilter(seedFile, _passwords[i]);
                    else
                        outputs = MarkovFilterCreator.GenerateLayeredMarkovFilter(seedFile, _passwords[i], 8);

                    Console.WriteLine("Done! Outputs: {0}", outputs);
                    _experiment.OutputCount = outputs;

                    Console.WriteLine("Loading seed...");
                    var seed = _experiment.LoadPopulation(XmlReader.Create(seedFile))[0];

                    Console.WriteLine("Creating model...");
                    var model = _experiment.CreateGenomeDecoder().Decode(seed);

                    // For every dataset, test the model
                    for (int j = 0; j < _datasetFilenames.Length; j++)
                    {
                        Console.Write("Validating {0} {1} model on {2} with {3} guesses... ", models[m], _datasetFilenames[i].Name, _datasetFilenames[j].Name, VALIDATION_GUESSES);
                        PasswordCrackingEvaluator eval = new PasswordCrackingEvaluator(VALIDATION_GUESSES, false);
                        var results = eval.Validate(model, _passwords[j], EXPERIMENT_OFFSET + models[m] + "-" + _datasetFilenames[i].Name + "-" + _datasetFilenames[j].Name + ".csv", 10000);
                        Console.WriteLine("Total Reward: {0} Uniques: {1}", results._fitness, results._alternativeFitness);

                        lock(_writerLock)
                            using (TextWriter writer = new StreamWriter(@"..\..\..\experiments\summary_results.csv", true))
                                writer.WriteLine("{0},{1},{2},{3},{4}%,{5}%", 
                                    _datasetFilenames[i].Name, 
                                    _datasetFilenames[j].Name, 
                                    results._fitness, 
                                    results._alternativeFitness,
                                    results._fitness / (double)_passwords[j].Sum(kv => kv.Value.Reward) * 100,
                                    results._alternativeFitness / (double)_passwords[j].Count * 100); 
                    }
                }
            }
        }
        #endregion




    }
}
