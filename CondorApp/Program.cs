using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CondorApp
{
    class Program
    {
        static void Main(string[] args)
        {
            int curArg = 0;
            //string experimentDir = args[curArg++];
            //string configFile = args[curArg++];

            CondorParameters cp = CondorParameters.GetParameters(args);
           
        }
    }
}
