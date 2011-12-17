using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeatMarkovModels;
using SharpNeat.Core;
using System.Threading;
using System.IO;

namespace PasswordEvolution
{
    public class PasswordCrackingEvaluator : IPhenomeEvaluator<MarkovChain>
    {
        public static Dictionary<string, int> Passwords;
        MD5HashChecker _md5;
        int _guesses;
        ulong _evalCount;
        ulong optimal;
        bool isOptimal;

        public PasswordCrackingEvaluator(int guessesPerIndividual, bool hashed = false)
        {
            _guesses = guessesPerIndividual;
            FoundPasswords = new HashSet<string>();
            //if(optimal == 0)
            //    optimal = (ulong)Passwords.Sum(p => p.Value);
            if (hashed)
                _md5 = new MD5HashChecker(Passwords);
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
            get { return isOptimal; }
        }

        public HashSet<string> FoundPasswords { get; set; }


        public FitnessInfo Evaluate(MarkovChain phenome)
        {
            double score = 0;
            int uniques = 0;
            HashSet<string> guessed = new HashSet<string>();
            for (int i = 0; i < _guesses; i++)
            {
                var guess = phenome.Activate();
                if (guessed.Contains(guess))
                    continue;
                //guessed.Add(guess);

                int count;
                if (_md5 != null)
                    lock (_md5)
                        count = _md5.InDatabase(guess);
                else
                    Passwords.TryGetValue(guess, out count);

                if (count > 0)
                {
                    score += count;
                    uniques++;
                    lock (FoundPasswords)
                        FoundPasswords.Add(guess);
                    guessed.Add(guess);
                }
            }

            if (score == optimal)
                isOptimal = true;

            _evalCount++;

            return new FitnessInfo(score, uniques);
        }

        public FitnessInfo Validate(MarkovChain model)
        {
            double score = 0;
            int uniques = 0;
            
            HashSet<string> found = new HashSet<string>();
            for (int i = 0; i < _guesses; i++)
            {
                if (i > 0 && i % 100000000 == 0)
                    Console.WriteLine(i);

                var guess = model.Activate();

                if (found.Contains(guess))
                    continue;

                int count;
                if (_md5 != null)
                    count = _md5.InDatabase(guess);
                else
                    Passwords.TryGetValue(guess, out count);

                if (count > 0)
                {
                    score += count;
                    uniques++;
                    found.Add(guess);
                }
            }

            return new FitnessInfo(score, uniques);
        }

        // QUICK HACK to get this done by the paper deadline
        public Dictionary<string, int> OneTimePasswordDeal;
        public FitnessInfo Validate(MarkovChain model, string logfile, int interval)
        {
            double score = 0;
            int uniques = 0;
            //QUICK HACK. Need to remove the Length==8 filter.
            double totalAccounts = OneTimePasswordDeal.Where(kv => kv.Key.Length == 8).Sum(kv => kv.Value);
            double totalUniques = OneTimePasswordDeal.Where(kv => kv.Key.Length == 8).Count();

            using (StreamWriter writer = new StreamWriter(logfile))
            {
                writer.WriteLine("Guesses,Accounts Cracked,Passwords Cracked,% Accounts,% Passwords");
                HashSet<string> found = new HashSet<string>();
                for (int i = 0; i < _guesses; i++)
                {
                    if (i > 0 && i % interval == 0)
                        writer.WriteLine("{0},{1},{2},{3}%,{4}%", i, score, uniques, score / totalAccounts * 100, uniques / totalUniques * 100);

                    var guess = model.Activate();

                    if (found.Contains(guess))
                        continue;

                    int count;
                    if (_md5 != null)
                        count = _md5.InDatabase(guess);
                    else
                        OneTimePasswordDeal.TryGetValue(guess, out count);

                    if (count > 0)
                    {
                        score += count;
                        uniques++;
                        found.Add(guess);
                    }
                }
                writer.WriteLine("{0},{1},{2},{3}%,{4}%", _guesses, score, uniques, score / totalAccounts * 100, uniques / totalUniques * 100);
            }

            return new FitnessInfo(score, uniques);
        }
        
    }
}
