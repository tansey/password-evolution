using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PasswordEvolution
{
    /// <summary>
    /// A collection of helper functions to load and analyze password datasets.
    /// </summary>
    public static class PasswordUtil
    {
        public static Dictionary<string, int> LoadPasswords(string pwdfile, int? length = null)
        {
            var passwords = new Dictionary<string, int>();
            ulong best = 0;
            int[] countsHistogram = new int[20];
            using (TextReader reader = new StreamReader(pwdfile))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.TrimStart();
                    if (line == "")
                        continue;
                    string[] tokens = line.Split();

                    string pw = tokens.Length == 1 ? line : tokens.Skip(1).Concatenate(" ");

                    // Check if something went wrong or the database had a weird token
                    if (pw.Length == 0)
                        continue;

                    int count = tokens.Length == 1 ? 1 : int.Parse(tokens[0]);

                    if (!length.HasValue || pw.Length == length.Value)
                    {
                        // Add it to the list
                        try
                        {
                            passwords.Add(pw, count);
                            best += (ulong)count;
                            if (count < countsHistogram.Length)
                                countsHistogram[(int)(count - 1)]++;
                            else
                                countsHistogram[countsHistogram.Length - 1]++;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }

            Console.WriteLine("Uniques: {0}", passwords.Values.Count);
            Console.WriteLine("Best possible: {0}", best);
            Console.WriteLine("Contains \"password\"? {0}", passwords.ContainsKey("password"));
            for (int i = 0; i < countsHistogram.Length; i++)
                Console.WriteLine("PWs of Count {0}: {1}", i + 1, countsHistogram[i]);

            return passwords;
        }

        /// <summary>
        /// Prints summary statistics for a database of passwords.
        /// </summary>
        /// <param name="filename">The file containing the passwords.</param>
        public static void PrintStats(string filename)
        {
            PrintStats(LoadPasswords(filename));
        }

        /// <summary>
        /// Prints summary statistics for a database of passwords.
        /// </summary>
        public static void PrintStats(Dictionary<string, int> passwords)
        {
            int total = passwords.Count;
            int lowercase = 0;
            int numerical = 0;
            int nonAlphaNumeric = 0;
            int uppercase = 0;
            int[] lengths = new int[20];

            foreach (var kv in passwords)
            {
                bool hasNum = false, hasNonAN = false, hasUpper = false, hasLower = false;
                string key = kv.Key;

                lengths[key.Length > 20 ? 19 : key.Length - 1]++;

                for (int i = 0; i < key.Length; i++)
                {
                    char c = key[i];
                    if (c.IsLowerCase())
                    {
                        if (!hasLower)
                        {
                            hasLower = true;
                            lowercase++;
                        }
                    }
                    else if (c.IsNumeric())
                    {
                        if (!hasNum)
                        {
                            hasNum = true;
                            numerical++;
                        }
                    }
                    else if (c.IsUpperCase())
                    {
                        if (!hasUpper)
                        {
                            hasUpper = true;
                            uppercase++;
                        }
                    }
                    else
                    {
                        if (!hasNonAN)
                        {
                            hasNonAN = true;
                            nonAlphaNumeric++;
                        }
                    }
                }
            }

            Console.WriteLine("Total Accounts: {0}", passwords.Sum(kp => kp.Value));
            Console.WriteLine("Total Unique Passwords: {0}", total);
            Console.WriteLine("- Contain lowercase: {0} ({1:N2}%)", lowercase, lowercase / (double)total * 100);
            Console.WriteLine("- Contain uppercase: {0} ({1:N2}%)", uppercase, uppercase / (double)total * 100);
            Console.WriteLine("- Contain number:    {0} ({1:N2}%)", numerical, numerical / (double)total * 100);
            Console.WriteLine("- Contain other:     {0} ({1:N2}%)", nonAlphaNumeric, nonAlphaNumeric / (double)total * 100);
            Console.WriteLine("Length distributions:");
            for (int i = 0; i < lengths.Length; i++)
                if (lengths[i] != 0)
                    Console.WriteLine("{2}{0}: {1}", i + 1, lengths[i], i == lengths.Length - 1 ? ">= " : "");
        }
    }
}
