using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PasswordEvolution
{
    /// <summary>
    /// This is a collection of utilities to process the various hacked password databases that are publicly available.
    /// The idea is to get all the different formats into a common format that simply stores each password and the
    /// number of accounts that have that password.
    /// </summary>
    public static class FileFormatUtil
    {
        /// <summary>
        /// Converts the hackforums.net database to the common text format.
        /// </summary>
        private static void ProcessHackForumsFile()
        {
            using (TextReader reader = new StreamReader(@"..\..\..\passwords\hackforums.sql"))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("INSERT INTO"))
                        break;
                }
                using (TextWriter writer = new StreamWriter(@"..\..\..\passwords\hackforums.txt"))
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line == "")
                            continue;
                        int quotes = 0;
                        int start = 0;
                        int end = 0;
                        for (int i = 0; i < line.Length; i++)
                            if (line[i] == '\'')
                            {
                                quotes++;
                                if (quotes == 3)
                                    start = i + 1;
                                if (quotes == 4)
                                {
                                    end = i;
                                    break;
                                }
                            }
                        string password = line.Substring(start, end - start);

                        start = end + 4;
                        end = line.IndexOf("\'", start + 1);
                        string salt = line.Substring(start, end - start);
                        writer.WriteLine("\"\";\"{0}\";\"{1}\"", password, salt);
                    }
            }
        }

        /// <summary>
        /// Converts the hashed (MD5) Battlefield Heroes Beta file to the common text format.
        /// </summary>
        private static void ProcessBattlefieldHeroesFile()
        {
            Dictionary<string, int> passwords = new Dictionary<string, int>();
            using (TextReader reader = new StreamReader(@"..\..\..\passwords\battlefield_heroes.csv"))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line == "")
                        continue;

                    var tokens = line.Split(';');
                    string pw = tokens.Last().Trim('"');
                    int val;
                    if (!passwords.TryGetValue(pw, out val))
                        passwords.Add(pw, 0);

                    passwords[pw]++;
                }
            }
            using (TextWriter writer = new StreamWriter(@"..\..\..\passwords\battlefield_heroes.txt"))
                foreach (var kv in passwords.OrderByDescending(s => s.Value))
                    writer.WriteLine(kv.Value + " " + kv.Key);
        }

        /// <summary>
        /// Filters the phished MySpace dataset to only realistic passwords. The phishing site did not check that
        /// the passwords were correct, so some passwords are clearly not viable/likely, such as ones with length
        /// greater than 20 characters or less than 6. Also, I believe MySpace had a creation rule requiring
        /// the user use a number or special character, based on the most popular passwords being things like
        /// "password1" and "i love you".
        /// </summary>
        private static void ProcessMySpaceFile()
        {
            using (TextReader reader = new StreamReader(@"..\..\..\passwords\myspace-unfiltered-withcount.txt"))
            {
                using (TextWriter writer = new StreamWriter(@"..\..\..\passwords\myspace-filtered-withcount.txt"))
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line == "")
                            continue;

                        line = line.TrimStart();
                        string[] tokens = line.Split();
                        string pw = tokens.Length == 1 ? line : tokens.Skip(1).Concatenate(" ");

                        if ((!pw.ContainsNumber() && !pw.ContainsSpecialCharacter())  || pw.Length > 20 || pw.Length < 6)
                            continue;

                        writer.WriteLine(line);
                    }
                }
            }
        }
    }
}
