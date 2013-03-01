using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PasswordEvolution;
using System.Xml;
using System.IO;
using SharpNeat.Core;

namespace ModelEvaluator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 7)
            {
				Console.WriteLine("Usage: ModelEvaluator.exe <results_file> <model_id> <model_file> <finished_flag> <passwords_found_file> <config_file> <outputs> [passwords] [pw_length]");
                return;
            }
			Console.WriteLine("Starting");
            PasswordEvolutionExperiment experiment = new PasswordEvolutionExperiment();

            int curArg = 0;
			string resultsFile = args[curArg++];
            int modelId = int.Parse(args[curArg++]);
			string modelFile = args[curArg++];
			string finishedFlag = args[curArg++];
			string passwordsFoundFile = args[curArg++];
			string configFile = args[curArg++];
			int outputs = int.Parse(args[curArg++]);
			experiment.OutputCount = outputs;

			// Load the XML configuration file
			XmlDocument xmlConfig = new XmlDocument();
			xmlConfig.Load(configFile);

			experiment.Initialize("evaluation", xmlConfig.DocumentElement);

            // Optionally load the passwords from somewhere besides the file specified
            // in the experiment config file.
            if (args.Length > curArg)
            {
				Console.WriteLine("Passwords file: {0}", args[curArg]);
				string passwordFile = args[curArg++];

                int? pwLength = null;
                if (args.Length > curArg)
				{
					Console.WriteLine("Password Length: {0}", args[curArg]);
					pwLength = int.Parse(args[curArg++]);
				}
                // Load the passwords to evaluate with
                experiment.Passwords = PasswordUtil.LoadPasswords(passwordFile, pwLength);
				Console.WriteLine("Passwords loaded");
            }
			PasswordCrackingEvaluator.Passwords = experiment.Passwords;

            PasswordCrackingEvaluator eval = new PasswordCrackingEvaluator(experiment.GuessesPerIndividual, experiment.Hashed);

            var modelGenome = experiment.LoadPopulation(XmlReader.Create(modelFile))[0];
            var model = experiment.CreateGenomeDecoder().Decode(modelGenome);

            using (TextWriter tw = new StreamWriter(resultsFile))
            {
                // Evaluate
                if (model == null)
                {   // Non-viable genome.
                    tw.WriteLine("0.0 0.0");
                }
                else
                {
                    FitnessInfo fitnessInfo = eval.Evaluate(model);
                    tw.WriteLine(fitnessInfo._fitness + " " + fitnessInfo._alternativeFitness);
                }
            }

			using(TextWriter writer = new StreamWriter(passwordsFoundFile))
				foreach(var pw in eval.FoundPasswords)
					writer.WriteLine(pw);

            File.Create(finishedFlag);
        }
    }
}
