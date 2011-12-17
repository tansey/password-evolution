using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PasswordEvolution
{
    public static class StringExtensions
    {
        public static bool ContainsNumber(this string s)
        {
            for (int i = 0; i < s.Length; i++)
                if (s[i].IsNumeric())
                    return true;
            return false;
        }
    }
}
