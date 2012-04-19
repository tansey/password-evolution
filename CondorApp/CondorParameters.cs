using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CondorApp
{
    class CondorParameters
    {
        public string Name { get; set; }
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
    }
}
