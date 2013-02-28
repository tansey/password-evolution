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
		public static Dictionary<string, PasswordInfo> Accounts;

        static void Main(string[] args)
        {
			Console.WriteLine("App Domain: {0}", AppDomain.CurrentDomain);
            //string experimentDir = args[curArg++];
            //string configFile = args[curArg++];
            cp = CondorParameters.GetParameters(args);
			if (cp == null)
			{
				CondorParameters.PrintHelp();
				return;
			}
			else
				Console.WriteLine(cp);
            RunExperiment();
        }

        private static void RunExperiment()
        {
            // Create evolution algorithm using the seed model to initialize the population
            Console.WriteLine("Creating population...");
            Console.Write("Building Markov model...");

            // Load the XML configuration file
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(cp.ConfigFile);
            
            // Set Training File
            string trainingSetFile = cp.TrainingDb;

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

            if(cp.EvolutionDb != null)
			{
				Console.WriteLine("Using command-line password file for evolution: {0}", cp.EvolutionDb);
				Console.WriteLine("Password length: {0}", cp.PasswordLength);
				PasswordCrackingEvaluator.Passwords = PasswordUtil.LoadPasswords(cp.EvolutionDb, cp.PasswordLength);
				Console.WriteLine("PasswordCrackingEvaluator.Passwords = {0}", PasswordCrackingEvaluator.Passwords == null ? "NULL" : "NOT NULL");
			}
			else
			{
				// Set the passwords to be used by the fitness evaluator.
				// These are the passwords our models will try to guess.
				PasswordCrackingEvaluator.Passwords = _experiment.Passwords;
				Console.WriteLine("Using config file passwords for evolution.");
			}
			Accounts = PasswordCrackingEvaluator.Passwords;

            Console.WriteLine("Loading seed...");

            // Load the seed model that we created at the start of this function
            var seed = _experiment.LoadPopulation(XmlReader.Create(seedFile))[0];

            // Create evolution algorithm using the seed model to initialize the population
            Console.WriteLine("Creating population...");

            ce = new CondorEvaluator(cp.ExperimentDir, cp.ConfigFile, cp.ResultsFile, CondorGroup.Grad, outputs, PasswordCrackingEvaluator.Passwords, cp.PasswordLength);
            _ea = _experiment.CreateEvolutionAlgorithm(seed, ce);


            // Attach an update event handler. This will be called at the end of every generation
            // to log the progress of the evolution (see function logEvolutionProgress below).
			_ea.UpdateScheme = new UpdateScheme(1);
			_ea.UpdateEvent += new EventHandler(logEvolutionProgress);
            
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
        public static void logEvolutionProgress(object sender, EventArgs e)
        {
            var maxFitness = _ea.GenomeList.Max(g => g.EvaluationInfo.Fitness);
            var maxAltFitness = _ea.GenomeList.Max(g => g.EvaluationInfo.AlternativeFitness);
            var genChampion = _ea.GenomeList.First(g => g.EvaluationInfo.Fitness == maxFitness);
            // Write the results to file.
//            using (TextWriter writer = new StreamWriter(cp.ResultsFile, true))
//            {
//				Console.WriteLine("Gen {0}: {1} ({2}) Total: {3}", _ea.CurrentGeneration,
//                                                            maxFitness,
//                                                            maxAltFitness,
//				                  							ce.Found.Count);
//				long sum = 0;
//				foreach(var s in ce.Found)
//				{
//					Console.WriteLine(s);
//					try{
//					Console.WriteLine("\t{0}", PasswordCrackingEvaluator.Passwords == null ? "NULL" : "NOT NULL");
//					Console.WriteLine("\t{0}", PasswordCrackingEvaluator.Passwords.Count);
//					Console.WriteLine("\t{0}", PasswordCrackingEvaluator.Passwords[s] == null ? "NULL" : "NOT NULL");
//					Console.WriteLine("\t{0}", PasswordCrackingEvaluator.Passwords[s].Accounts);
//					}catch(Exception ex){
//						Console.WriteLine(ex.Message);
//					}
//					sum += PasswordCrackingEvaluator.Passwords[s].Accounts;
//					Console.WriteLine("\t{0}", sum);
//				}
//				Console.WriteLine("Done with that.");
//				Console.WriteLine("{0},{1},{2},{3},{4},{5},{6}", _ea.CurrentGeneration,
//                                                            maxFitness,
//                                                            maxAltFitness,
//                                                            _ea.GenomeList.Average(g => g.EvaluationInfo.Fitness),
//                                                            _ea.GenomeList.Average(g => g.EvaluationInfo.AlternativeFitness),
//                                                            ce.Found.Sum(s => PasswordCrackingEvaluator.Passwords[s].Accounts),
//                                                            ce.Found.Count
//				                  							);
//                writer.WriteLine("{0},{1},{2},{3},{4},{5},{6}", _ea.CurrentGeneration,
//                                                                maxFitness,
//                                                                maxAltFitness,
//                                                                _ea.GenomeList.Average(g => g.EvaluationInfo.Fitness),
//                                                                _ea.GenomeList.Average(g => g.EvaluationInfo.AlternativeFitness),
//												                ce.Found.Sum(s => PasswordCrackingEvaluator.Passwords[s].Accounts),
//												                ce.Found.Count);
//            }
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
