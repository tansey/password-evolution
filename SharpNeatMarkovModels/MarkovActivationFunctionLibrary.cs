using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Network;

namespace SharpNeatMarkovModels
{
    public static class MarkovActivationFunctionLibrary
    {
        public static IActivationFunctionLibrary CreateLibraryMc(params string[] nodes)
        {
            List<ActivationFunctionInfo> fnList = new List<ActivationFunctionInfo>(2);
            for (int i = 0; i < nodes.Length; i++)
            {
                var fn = new MarkovActivationFunction(nodes[i]);

                // TODO: Add ability to weight different nodes based on occurrence frequencies
                fnList.Add(new ActivationFunctionInfo(i, 1.0 / (double)nodes.Length, fn));

                // Add the functionality to read/write XML files
                NetworkXmlIO.AddActivationFunction(fn.FunctionId, fn);
            }
            return new DefaultActivationFunctionLibrary(fnList);
        }
    }
}
