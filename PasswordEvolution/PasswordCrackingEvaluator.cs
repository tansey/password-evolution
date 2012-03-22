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
    /// <summary>
    /// The fitness evaluator for each Markov model. Each model is evaluated
    /// for a fixed number of guesses and the number of passwords cracked
    /// is used as a fitness function.
    /// </summary>
    public class PasswordCrackingEvaluator : IPhenomeEvaluator<MarkovChain>
    {
        public static Dictionary<string, PasswordInfo> Passwords;
        MD5HashChecker _md5;
        int _guesses;
        ulong _evalCount;
        bool isOptimal;

        public PasswordCrackingEvaluator(int guessesPerIndividual, bool hashed = false)
        {
            _guesses = guessesPerIndividual;
            FoundPasswords = new HashSet<string>();

            // Begin Janek added
            FoundValidationPasswords = new HashSet<string>();
            // End Janek added

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
            get { return false; }
        }

        public HashSet<string> FoundPasswords { get; set; }

        public HashSet<string> FoundValidationPasswords { get; set; }

        /// <summary>
        /// The fitness function for a candidate Markov model. It uses the number of accounts
        /// cracked as the fitness. 
        /// 
        /// This method is thread-safe, so you can run it in parallel.
        /// </summary>
        /// <param name="phenome">The Markov model to evaluate.</param>
        /// <returns>The fitness of the Markov model (number of passwords cracked).</returns>
        public FitnessInfo Evaluate(MarkovChain phenome)
        {
            // The score is the number of accounts cracked.
            // Since some passwords are reused across multiple
            // accounts, a particular password may be worth more
            // than 1.
            double score = 0;

            // This is how many unique passwords were cracked.
            int uniques = 0;

            // Track previous guesses so we don't count duplicates.
            HashSet<string> guessed = new HashSet<string>();

            // The model gets a fixed number of guesses.
            for (int i = 0; i < _guesses; i++)
            {
                // Generate a guess
                var guess = phenome.Activate();

                // If the model already guessed this password previously,
                // just skip it.
                if (guessed.Contains(guess))
                    continue;

                double count=0;
                PasswordInfo temp;
                // If the database is hashed, then we need to hash the guess.
                if (_md5 != null)
                    lock (_md5)
                        count = _md5.InDatabase(guess);
                // If it's plaintext, we can simply look it up in the dictionary.
                else
                {
                    if(Passwords.TryGetValue(guess, out temp))
                        count = temp.Reward;
                }
                // If the password was in the dictionary, then this model guessed
                // it correctly.
                if (count > 0)
                {
                    // Add the number of accounts cracked to the model's score.
                    score += count;
                    uniques++;

                    // Add this password to the list of total found passwords.
                    lock (FoundPasswords)
                        FoundPasswords.Add(guess);

                    // Add the guess to the list of previous guesses for this model.
                    // Ideally, we'd like to add passwords that weren't in the database
                    // so we could skip those too, but for large guess sizes it will
                    // take up too much memory.
                    guessed.Add(guess);
                }
            }

            _evalCount++;

            // Return the fitness as the number of accounts cracked. The alternative
            // fitness is the unique accounts cracked. You can try switching
            // these two around to see which gets better performance.
            return new FitnessInfo(score, uniques);
        }

        /// <summary>
        /// Validate a model against a large number of guesses.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
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

                double count=0;
                PasswordInfo temp;
                if (_md5 != null)
                    count = _md5.InDatabase(guess);
                else
                {
                    if(Passwords.TryGetValue(guess, out temp))
                        count = temp.Accounts;
                }

                if (count > 0)
                {
                    score += count;
                    uniques++;
                    found.Add(guess);
                }
            }

            return new FitnessInfo(score, uniques);
        }

        /// <summary>
        /// Validates a model against a large series of guesses and writes the results
        /// to a log file.
        /// </summary>
        /// <param name="model">The model to validate.</param>
        /// <param name="db">The database of passwords.</param>
        /// <param name="logfile">The file in which to save the results.</param>
        /// <param name="interval">The frequency with which to write progress to file.</param>
        /// <param name="passwordLength">The length of passwords to try cracking.</param>
        /// <returns></returns>
        public FitnessInfo Validate(MarkovChain model, Dictionary<string, PasswordInfo> db, string logfile, int interval, int passwordLength = 8)
        {
            double score = 0;
            int uniques = 0;
            double totalAccounts = db.Where(kv => kv.Key.Length == passwordLength).Sum(kv => kv.Value.Accounts);
            double totalUniques = db.Where(kv => kv.Key.Length == passwordLength).Count();

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

                    double count;
                    PasswordInfo temp;
                    if (_md5 != null)
                        count = _md5.InDatabase(guess);
                    else
                    {
                        db.TryGetValue(guess, out temp);
                        count = temp.Reward;
                    }
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

        public bool ValidatePopulation(List<MarkovChain> models)
        {
            foreach (MarkovChain m in models)
                ValidatePopHelper(m);
            return true;
        }

        /// <summary>
        /// Validate a model against a large number of guesses.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool ValidatePopHelper(MarkovChain model)
        {
          //  double score = 0;
          //  int uniques = 0;

            HashSet<string> found = new HashSet<string>();
            for (int i = 0; i < _guesses; i++)
            {
                if (i > 0 && i % 100000000 == 0)
                    Console.WriteLine(i);

                var guess = model.Activate();

                if (found.Contains(guess))
                    continue;

                double count = 0;
                PasswordInfo temp;
                if (_md5 != null)
                    count = _md5.InDatabase(guess);
                else
                {
                    if (Passwords.TryGetValue(guess, out temp))
                        count = temp.Accounts;
                }

                if (count > 0)
                {
                  //  score += count;
                  //  uniques++;
                    lock (FoundValidationPasswords)
                        FoundValidationPasswords.Add(guess);
                }
            }

            return true;
        }
    }
}
