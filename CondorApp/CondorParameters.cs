using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CondorApp
{
    class CondorParameters
    {
        public string Name { get; set; }
        public string ExperimentDir { get; set; }
        public string ConfigFile { get; set; }
        public int EnsembleSize {get; set;}
        public int Generations { get; set; }
        public int EnsembleGuesses {get; set;} //allocation -> guess weighting
        //public string Database { get; set; }
        public string TrainingDb {get; set;}
        public string EvolutionDb {get; set;}
        public string ValidationDb {get; set;}
        public int PasswordLength {get; set;}
        public int PopulationSize {get; set;} //-Pop size - speciation, generations
        public int ValidationGuesses {get; set;} 
        public int EvaluationGuesses {get; set;}
        public string ResultsPath { get; set; }

        public string ExperimentPath { get; set; }
        public string SeedFile { get; set; }
        public string ResultsFile { get; set; }
        public string ChampionFilePath { get; set; }


        public static CondorParameters GetParameters(string[] args)
        {
            CondorParameters cp = DefaultParameters();

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i][0] != '-')
                {
                    Console.WriteLine("Invalid option: '{0}'. Options must be prefixed with '-'", args[i]); return null;
                }

                switch (args[i].Substring(1).Trim().ToLower())
                {
                    case "help": PrintHelp(); return null;
                    case "name":
                    case "n":
                        cp.Name = args[++i];
                        break;
                    case "experimentdir":
                    case "expdir":
                        cp.ExperimentDir = args[++i];
                        break;
                    case "config":
                    case "c":
                        cp.ConfigFile = args[++i];
                        break;
                    case "geneartions":
                    case "g":
                        cp.Generations = Convert.ToInt32(args[++i]);
                        break;
                    case "trainingdb":
                    case "tdb":
                        cp.TrainingDb = args[++i];
                        break;
                    case "evolutiondb":
                    case "edb":
                         cp.TrainingDb = args[++i];
                        break;
                    case "validationdb":
                    case "vdb":
                        cp.ValidationDb = args[++i];
                        break;
                    case "passwordlength":
                    case "pwd":
                        cp.PasswordLength = Convert.ToInt32(args[++i]);
                        break;
                    case "popultaion":
                    case "p":
                        cp.PopulationSize = Convert.ToInt32(args[++i]);
                        break;
                    case "validation":
                    case "v":
                        cp.ValidationGuesses = Convert.ToInt32(args[++i]);
                        break;
                    case "evaluation":
                    case "e":
                        cp.EvaluationGuesses = Convert.ToInt32(args[++i]);
                        break;
                    case "ensemble":
                    case "es":
                        cp.EnsembleSize = Convert.ToInt32(args[++i]);
                        break;
                    case "seed":
                    case "s":
                        cp.SeedFile = args[++i];
                        break;
                    case "results":
                    case "r":
                        cp.ResultsFile = args[++i];
                        break;
                    case "champion":
                    case "ch":
                        cp.ChampionFilePath = args[++i];
                        break;
                }

            }
            cp.ResultsPath = cp.ExperimentPath + "_results.csv";  //gg.ExperimentPath + gg.Name + "_results.csv";
            
            return cp;
        }


        public static CondorParameters DefaultParameters()
        {
            CondorParameters cp = new CondorParameters()
            {
               // Name = name,
                EnsembleSize = 200,
                Generations = 200,
                //EnsembleGuesses = //default value? 
                //TrainingDb = ,
                //EvolutionDb = ,
                //ValidationDb = ,
                PasswordLength = 8,
                PopulationSize = 100,
                ValidationGuesses = 1000000000,
                EvaluationGuesses = 10000000 ,//same as guesses?
                ChampionFilePath = @"../../../experiments/champions/champion"
                //ExperimentPath =   // what's the path of the experiment
            };

            return cp;
        }

       static void PrintHelp()
        {
            Console.WriteLine("Usage: AgentBenchmark.exe <name> [-options...]");
            Console.WriteLine("<name>".PadRight(25) + "File prefix to use when saving the config and results files.");
            Console.WriteLine("-help".PadRight(25) + "Prints the usage summary.");
            Console.WriteLine("-name -n".PadRight(25) + "Name of the experiment.");
            Console.WriteLine("-experimentdir -expdir".PadRight(25) + "Directory of the experiment.");
            Console.WriteLine("-config -c".PadRight(25) + "Name of the config file.");
            Console.WriteLine("-generations -g".PadRight(25) + "Number of generations. Default: 200");
            Console.WriteLine("-trainingdb -tdb".PadRight(25) + "Training Database file name.");
            Console.WriteLine("-evolutiondb -edb".PadRight(25) + "Evolution Database file name.");
            Console.WriteLine("-validationdb -vdb".PadRight(25) + "Validation Database file name.");
            Console.WriteLine("-passwordlength -pwd".PadRight(25) + "Length of the passwords. Default: 8");
            Console.WriteLine("-popultaion -p".PadRight(25) + "Size of the population. Default: 100");
            Console.WriteLine("-validation -v".PadRight(25) + "Number of validation guesses. Default: 1000000000");
            Console.WriteLine("-evaluation -e".PadRight(25) + "Number of evaluation guesses. Default: 10000000");
            Console.WriteLine("-ensemble -es".PadRight(25) + "Size of the ensemble. Default: 200");
            Console.WriteLine("-seed -s".PadRight(25) + "The Seed file.");
            Console.WriteLine("-results -r".PadRight(25) + "The file where the results will be written.");
            Console.WriteLine("-champion -ch".PadRight(25) + "The path where the champion will be written. Default:const string CHAMPION_FILE_ROOT = ../../../experiments/champions/champion");
        }




    }
}
