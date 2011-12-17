using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PasswordEvolution
{
    public static class CharExtensions
    {
        public static bool IsNumeric(this char c)
        {
            return c >= '0' && c <= '9';
        }

        public static bool IsUpperCase(this char c)
        {
            return c >= 'A' && c <= 'Z';
        }

        public static bool IsLowerCase(this char c)
        {
            return c >= 'a' && c <= 'z';
        }

        public static bool IsAlphaNumeric(this char c)
        {
            return c.IsLowerCase() || c.IsUpperCase() || c.IsNumeric();
        }
    }
}
