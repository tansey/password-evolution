using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Utility;
using System.IO;

namespace PasswordEvolution
{
    public static class ToyProblemUtil
    {
        /// <summary>
        /// The probability of choosing each letter. People are much more
        /// likely to choose the numbers 1 and 0, so those receive a higher
        /// weight.
        /// </summary>
        static double[] defaultDigitProbs = new double[]
        {
            0.14, //0
            0.3,//1
            0.07,//2
            0.07,//3
            0.07, //4
            0.07, //5
            0.07, //6
            0.07, //7
            0.07, //8
            0.07 //9
        };

        /// <summary>
        /// The probability of adding a letter at the beginning, end, or a random index
        /// in the password. In the default case, we don't even both having one 90% of the time.
        /// </summary>
        static double[] defaultDigitPositionProbs = new double[]
        {
            0.9,//do nothing
            0.025,//prepend
            0.06,//append
            0.015//random
        };

        /// <summary>
        /// The probability of adding a letter at the beginning, end, or a random index
        /// in the password. In the required-digit case, we add a number 100% of the time
        /// and draw the prepend/append/random distribution from the Microsoft Research
        /// paper in WWW'07 on password pattern distributions.
        /// </summary>
        static double[] requiredDigitPositionProbs = new double[]
        {
            0,//do nothing
            0.25,//prepend
            0.6,//append
            0.15//random
        };

        /// <summary>
        /// Takes in an English dictionary and morphs it so that some words contain numbers, and it looks more password-like.
        /// Note: this probably can be improved by using a Markov model to enforce the minimum length creation rule. It
        /// could also be enhanced with the ability to generate more rules or more realistic passwords.
        /// </summary>
        /// <param name="inFile">The English dictionary file.</param>
        /// <param name="outFile">The file in which to save the morphed dictionary.</param>
        /// <param name="counts">Whether to assume each password is in the dictionary once or to add counts.</param>
        /// <param name="minLength">The minimum length of a password in the dictionary. If a word isn't long enough, it will have number added until it is.</param>
        public static void MorphEnglish(string inFile, string outFile, bool requireDigit = false, bool counts = false, int minLength = 0)
        {
            // Load the passwords
            var english = PasswordUtil.LoadPasswords(inFile);

            // Morph the dictionary either with a required digit creation rule or not, depending on the requireDigit value
            var morphed = requireDigit 
                            ? morphEnglish(english, defaultDigitProbs, requiredDigitPositionProbs, minLength) // Use a digit-required creation rule
                            : morphEnglish(english, defaultDigitProbs, defaultDigitPositionProbs, minLength); // No creation rule

            // Write the results to file.
            using (TextWriter writer = new StreamWriter(outFile))
                foreach (var kv in morphed)
                    if (counts)
                        writer.WriteLine("{0} {1}", kv.Value, kv.Key);
                    else
                        writer.WriteLine(kv.Key);
        }

        static Dictionary<string, int> morphEnglish(Dictionary<string, int> english, double[] digitProbs, double[] posProbs, int minLength = 0)
        {
            Dictionary<string, int> results = new Dictionary<string, int>();

            FastRandom random = new FastRandom();
            
            Console.WriteLine("Probs sum: {0}", digitProbs.Sum());
            RouletteWheelLayout digitLayout = new RouletteWheelLayout(digitProbs);
            RouletteWheelLayout posLayout = new RouletteWheelLayout(posProbs);
            int alreadyNumbered = 0;
            foreach (string s in english.Keys)
            {
                bool numbered = false;
                for (int i = 0; i < s.Length; i++)
                    if (s[i] >= '0' && s[i] <= '9')
                    {
                        alreadyNumbered++;
                        numbered = true;
                        break;
                    }
                string morphedPassword = s;
                while (!numbered || morphedPassword.Length < minLength)
                {
                    int toAdd = RouletteWheel.SingleThrow(digitLayout, random);
                    int pos = RouletteWheel.SingleThrow(posLayout, random);

                    if (pos == 0)
                        break;
                    else if (pos == 1)
                        morphedPassword = toAdd + morphedPassword;
                    else if (pos == 2)
                        morphedPassword = morphedPassword + toAdd;
                    else
                    {
                        pos = random.Next(morphedPassword.Length);
                        morphedPassword = morphedPassword.Substring(0, pos) + toAdd + morphedPassword.Substring(pos, morphedPassword.Length - pos);
                    }
                    numbered = true;
                }
                int val;
                if (!results.TryGetValue(morphedPassword, out val))
                    results.Add(morphedPassword, 1);
            }
            Console.WriteLine("Had numbers already: {0}", alreadyNumbered);
            return results;
        }
    }
}
