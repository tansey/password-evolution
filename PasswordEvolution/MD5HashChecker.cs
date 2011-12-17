using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace PasswordEvolution
{
    public class MD5HashChecker
    {
        MD5 _md5;
        MD5Crypt _md5salt;
        Dictionary<string, int> _passwords;
        List<string> _salts;

        public MD5HashChecker(Dictionary<string, int> passwords)
        {
            _passwords = passwords;
            _md5 = MD5.Create();
        }

        public MD5HashChecker(string dbFilename, bool salted = false)
        {
            _passwords = new Dictionary<string, int>();
            _md5 = MD5.Create();
            if (salted)
            {
                _salts = new List<string>();
                _md5salt = new MD5Crypt();
            }
            using (TextReader reader = new StreamReader(dbFilename))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if(line == "")
                        continue;

                    var tokens = line.Split(';');
                    string pw;
                    if (!salted)
                    {
                        pw = tokens.Last().Trim('"');
                        
                    }
                    else
                    {
                        _salts.Add(tokens.Last().Trim('"'));
                        pw = tokens[tokens.Length - 2].Trim('"');
                    }
                    int val;
                    if (!_passwords.TryGetValue(pw, out val))
                        _passwords.Add(pw, 0);

                    _passwords[pw]++;
                }
            }

        }

        public void PrintCounts()
        {
            Console.WriteLine("Highest reused password count: {0}", _passwords.Max(kv => kv.Value));
            Console.WriteLine("Is it \"password\"? {0}", InDatabase("password"));
            Console.WriteLine("Is it \"password1\"? {0}", InDatabase("password1"));
            Console.WriteLine("Is it \"Password1\"? {0}", InDatabase("Password1"));
            Console.WriteLine("Is it \"123456\"? {0}", InDatabase("123456"));
            Console.WriteLine("Is it \"12345678\"? {0}", InDatabase("12345678"));
            Console.WriteLine("Is it \"jesus\"? {0}", InDatabase("jesus"));
            Console.WriteLine("Is it \"love\"? {0}", InDatabase("love"));
            Console.WriteLine("Is it \"war\"? {0}", InDatabase("war"));
            Console.WriteLine("Is it \"michael\"? {0}", InDatabase("michael"));
            Console.WriteLine("Is it \"xbox360\"? {0}", InDatabase("xbox360"));
            Console.WriteLine("Is it \"heroes\"? {0}", InDatabase("heroes"));
        }

        public int InDatabase(string pw)
        {
            if (_salts == null)
            {
                string hash = GetMd5Hash(pw);
                int val;
                if (_passwords.TryGetValue(hash, out val))
                    return val;
            }
            else
            {
                int count = 0;
                foreach (string salt in _salts)
                {
                    string hashsalt = _md5salt.crypt(pw, salt);
                    int val;
                    if (_passwords.TryGetValue(hashsalt, out val))
                        count += val;
                }
                return count;
            }
            return 0;
        }

        public string GetMd5Hash(string pw)
        {
            byte[] data = _md5.ComputeHash(Encoding.UTF8.GetBytes(pw));

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sb.Append(data[i].ToString("x2"));
            return sb.ToString();
        }
    }
}
