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
            if (args.Length < 5)
            {
                Console.WriteLine("Usage: ModelEvaluator.exe <model_id> <model_file> <results_file> <finished_flag> <config_file> [passwords] [pw_length]");
                return;
            }

            PasswordEvolutionExperiment experiment = new PasswordEvolutionExperiment();

            int curArg = 0;
            int modelId = int.Parse(args[++curArg]);
            string modelFile = args[++curArg];
            string resultsFile = args[++curArg];
            string finishedFlag = args[++curArg];
            string configFile = args[++curArg];

            // Optionally load the passwords from somewhere besides the file specified
            // in the experiment config file.
            if (args.Length > curArg)
            {
                string passwordFile = args[++curArg];

                int? pwLength = null;
                if (args.Length > curArg)
                    pwLength = int.Parse(args[++curArg]);

                // Load the passwords to evaluate with
                experiment.Passwords = PasswordUtil.LoadPasswords(passwordFile, pwLength);
            }

            // Load the XML configuration file
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(configFile);

            experiment.Initialize("evaluation", xmlConfig.DocumentElement);

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
                    double val = fitnessInfo._fitness;
                    double val2 = fitnessInfo._alternativeFitness;
                    tw.WriteLine(fitnessInfo._fitness + " " + fitnessInfo._alternativeFitness);
                }
            }

            File.Create(finishedFlag);
        }
    }
}
