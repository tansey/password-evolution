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


        public static CondorParameters GetParameters(string[] args)
        {
            CondorParameters cp = DefaultParameters(args[0]);

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
                        string name = args[++i];
                        cp.Name = name;
                        break;
                    case "experimentdir":
                    case "expdir":
                        string experimentDir = args[++i];
                        cp.ExperimentDir = experimentDir;
                        break;
                    case "config":
                    case "c":
                        string configFile = args[++i];
                        cp.ConfigFile = configFile;
                        break;

                }

            }
            
            return cp;
        }


        public static CondorParameters DefaultParameters(string name)
        {
            CondorParameters cp = new CondorParameters()
            {
                Name = name,
                EnsembleSize = 5,
                Generations = 200,
                //EnsembleGuesses = //default value? 
                //TrainingDb = ,
                //EvolutionDb = ,
                //ValidationDb = ,
                PasswordLength = 8,
                PopulationSize = 100,
                ValidationGuesses = 1000000000,
                EvaluationGuesses = 10000000 //same as guesses?

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
            Console.WriteLine("-config -c".PadRight(25) + "Name of the config file");

            Console.WriteLine("-game -g".PadRight(25) + "Name of the game. Valid options: tictactoe, connect4, reversi. Default: tictactoe");
            Console.WriteLine("-inputs -i".PadRight(25) + "Number of inputs for the neural network (usually # of board spaces). Default: 9");
            Console.WriteLine("-outputs -o".PadRight(25) + "Number of outputs for the neural network (usually # of board spaces). Default: 9");
            Console.WriteLine("-evaluator -eval -e".PadRight(25) + "Evaluation function to use. Valid options: random, coevolve, minimax, blondie, mcts. Default: random");
            Console.WriteLine("-name -n".PadRight(25) + "Name of the experiment.");
            Console.WriteLine("-winreward -win".PadRight(25) + "Reward an agent receives for winning a game. Default: 2");
            Console.WriteLine("-tiereward -tie".PadRight(25) + "Reward an agent receives for tying a game. Default: 1");

        }




    }
}
