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
using PasswordEvolution;
using SharpNeat.Domains;
namespace CondorApp
{
    class Program
    {
        static CondorParameters cp;
        static CondorEvaluator ce;
        static PasswordEvolutionExperiment _experiment;
        static NeatEvolutionAlgorithm<NeatGenome> _ea;
        const bool VALIDATE_ALL_STAR = true;
        static void Main(string[] args)
        {
            int curArg = 0;
            //string experimentDir = args[curArg++];
            //string configFile = args[curArg++];

           cp = CondorParameters.GetParameters(args);
           
           
        }

        private static void RunExperiment()
        {
            // Create evolution algorithm using the seed model to initialize the population
            Console.WriteLine("Creating population...");
            Console.Write("Building Markov model...");

            // Load the XML configuration file
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(cp.ConfigFile);
            XmlElement xmlConfigElement = xmlConfig.DocumentElement;

            // Set Training File
            string trainingSetFile = cp.TrainingDb;//XmlUtils.GetValueAsString(xmlConfigElement, "TrainingFile");

            // Create seedFile
            string seedFile = cp.SeedFile;

            // Create results file.
            string resultsFile = cp.ResultsFile;
            Console.WriteLine();
            Console.WriteLine("Training File: {0}", trainingSetFile);
            Console.WriteLine("Seed File: {0}", seedFile);
            Console.WriteLine("Results File: {0}", resultsFile);

            // Load the training set passwords from file
            var passwords = PasswordUtil.LoadPasswords(trainingSetFile, cp.PasswordLength);


            // Create a Markov model from the passwords. This model will be used
            // as our seed for the evolution.
            int outputs = MarkovFilterCreator.GenerateFirstOrderMarkovFilter(seedFile, passwords);

            // Free up the memory used by the passwords
            passwords = null;

            Console.WriteLine("Done! Outputs: {0}", outputs);

            _experiment = new PasswordEvolutionExperiment();
            _experiment.OutputCount = outputs;

            // Initialize the experiment with the specifications in the config file.
            _experiment.Initialize("PasswordEvolution", xmlConfig.DocumentElement); //cmd arguments

            // Set the passwords to be used by the fitness evaluator.
            // These are the passwords our models will try to guess.
            // PasswordsWithAccounts is the file used for validation. Its account values won't be changed.
            PasswordCrackingEvaluator.Passwords = _experiment.Passwords;

            Console.WriteLine("Loading seed...");

            // Load the seed model that we created at the start of this function
            var seed = _experiment.LoadPopulation(XmlReader.Create(seedFile))[0];

            // Create evolution algorithm using the seed model to initialize the population
            Console.WriteLine("Creating population...");

            ce = new CondorEvaluator(cp.ExperimentDir, cp.ConfigFile, CondorGroup.Undergrad,  PasswordCrackingEvaluator.Passwords, cp.PasswordLength);
            _ea = _experiment.CreateEvolutionAlgorithm(seed, ce);


            // Attach an update event handler. This will be called at the end of every generation
            // to log the progress of the evolution (see function logEvolutionProgress below).
            _ea.UpdateEvent += new EventHandler(logEvolutionProgress);
            //_ea.UpdateScheme = new UpdateScheme(1);//.UpdateMode.

            // Setup results file
            using (TextWriter writer = new StreamWriter(resultsFile))
                writer.WriteLine("Generation,Champion Accounts,Champion Uniques,Average Accounts,Average Uniques,Total Accounts,Total Uniques");
            //_generationalResultsFile = resultsFile;

            // Start algorithm (it will run on a background thread).
            Console.WriteLine("Starting evolution. Pop size: {0} Guesses: {1}", _experiment.DefaultPopulationSize, _experiment.GuessesPerIndividual);
            _ea.StartContinue();

            // Wait until the evolution is finished.
            while (_ea.RunState == RunState.Running) { Thread.Sleep(1000); }


        }

        static int _gens = 0;
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
            using (TextWriter writer = new StreamWriter(cp.ResultsFile, true))
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
            if (_gens <= cp.Generations)
            {
                // Save the best genome to file
                var doc = NeatGenomeXmlIO.SaveComplete(new List<NeatGenome>() { genChampion }, true);
                doc.Save(cp.ChampionFilePath + "_gen_" + _gens + ".xml");
            }

            // If we've reached the maximum number of generations,
            // tell the algorithm to stop.
            if (_gens >= cp.Generations)
                _ea.Stop();

            _gens++;
        }

    }
}
