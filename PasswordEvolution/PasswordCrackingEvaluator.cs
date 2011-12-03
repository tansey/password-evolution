using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeatMarkovModels;
using SharpNeat.Core;

namespace PasswordEvolution
{
    public class PasswordCrackingEvaluator : IPhenomeEvaluator<MarkovChain>
    {
        Dictionary<string, int> _passwords;
        int _guesses;
        ulong _evalCount;
        bool optimal = false;

        public PasswordCrackingEvaluator(Dictionary<string, int> passwords, int guessesPerIndividual)
        {
            _passwords = passwords;
            _guesses = guessesPerIndividual;
        }

        public ulong EvaluationCount
        {
            get { return _evalCount; }
        }

        public void Reset()
        {
            
        }

        public bool StopConditionSatisfied
        {
            get { return optimal; }
        }

        public FitnessInfo Evaluate(MarkovChain phenome)
        {
            double score = 0;

            HashSet<string> guessed = new HashSet<string>();
            for (int i = 0; i < _guesses; i++)
            {
                var guess = phenome.Activate();
                if (guessed.Contains(guess))
                    continue;
                guessed.Add(guess);

                int count;
                if(_passwords.TryGetValue(guess, out count))
                    score += count;
            }

            _evalCount++;

            return new FitnessInfo(score, score);
        }

        
    }
}
