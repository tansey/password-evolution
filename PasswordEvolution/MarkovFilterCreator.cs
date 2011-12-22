using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;

namespace PasswordEvolution
{
    public static class MarkovFilterCreator
    {
        /// <summary>
        /// Generates a first-order Markov filter. In this model, we have an origin node
        /// that transitions into a single layer of character nodes. After the initial
        /// transition, all character nodes transition between themselves.
        /// </summary>
        /// <param name="filename">The file in which to save the generated model.</param>
        /// <param name="corpus">The password database from which to build the model.</param>
        /// <returns>The number of output nodes for SharpNEAT.</returns>
        public static int GenerateFirstOrderMarkovFilter(string filename, string corpus)
        {
            var passwords = PasswordUtil.LoadPasswords(corpus);
            return GenerateFirstOrderMarkovFilter(filename, passwords);
        }

        public static int GenerateFirstOrderMarkovFilter(string filename, Dictionary<string, int> passwords)
        {
            Console.WriteLine("Creating First-Order Markov Filter");
            double[] fnFreqs = calculateFnFreqs(passwords);

            StringBuilder functions = new StringBuilder();
            string functionFormat = "<Fn id=\"{0}\" name=\"MC-{1}\" prob=\"{2}\" />";

            for (int i = 0; i < 95; i++)
                functions.AppendLine(string.Format(functionFormat, i, SecurityElement.Escape(((char)(i + 32)).ToString()), fnFreqs[i]));
            functions.AppendLine(string.Format(functionFormat, 95, "END", 0));

            double[][] freqs = calculateTransitionFreqs(passwords);

            StringBuilder nodes = new StringBuilder();
            string nodeFormat = "<Node type=\"out\" id=\"{0}\" fnId=\"{1}\" />";

            StringBuilder connections = new StringBuilder();
            string connFormat = "<Con id=\"{0}\" src=\"{1}\" tgt=\"{2}\" wght=\"{3}\" />";

            int nextId = 97;

            for (int i = 0; i < 95; i++)
            {
                int nodeId = i + 2;

                // Add the next character node
                nodes.AppendLine(string.Format(nodeFormat, nodeId, i));

                // Add a connection from the origin to the character node
                if (freqs[0][i] > 0)
                    connections.AppendLine(string.Format(connFormat, nextId++, 1, nodeId, freqs[0][i]));

                for (int j = 0; j < 95; j++)
                    if (freqs[i + 1][j] > 0)
                        connections.AppendLine(string.Format(connFormat, nextId++, nodeId, j + 2, freqs[i + 1][j]));
            }
            using (TextWriter writer = new StreamWriter(filename))
            {
                writer.WriteLine(SEED_START);
                writer.WriteLine(functions.ToString());
                writer.WriteLine(SEED_2);
                writer.WriteLine(nodes.ToString());
                writer.WriteLine(SEED_MIDDLE);
                writer.WriteLine(connections.ToString());
                writer.WriteLine(SEED_END);
            }

            return 95;
        }
        /// <summary>
        /// Creates a layered Markov filter. In this model, we have a layer of nodes
        /// for each character in the password. This enables us to capture more information
        /// the distribution than a simple first-order model.
        /// </summary>
        /// <param name="filename">The file in which to save the generated model.</param>
        /// <param name="corpus">The password database from which to build the model.</param>
        /// <param name="layers">The maximum number of layers (password length) to have in this model.</param>
        /// <returns>The number of output nodes for SharpNEAT.</returns>
        public static int GenerateLayeredMarkovFilter(string filename, string corpus, int layers)
        {
            var passwords = PasswordUtil.LoadPasswords(corpus);
            return GenerateLayeredMarkovFilter(filename, passwords, layers);
        }
        public static int GenerateLayeredMarkovFilter(string filename, Dictionary<string, int> passwords, int layers)
        {
            Console.WriteLine("Creating Nth-Order Markov Filter. Layers: {0}", layers);
            double[] fnFreqs = calculateFnFreqs(passwords);

            StringBuilder functions = new StringBuilder();
            string functionFormat = "<Fn id=\"{0}\" name=\"MC-{1}\" prob=\"{2}\" />";

            for (int i = 0; i < 95; i++)
                functions.AppendLine(string.Format(functionFormat, i, SecurityElement.Escape(((char)(i + 32)).ToString()), 0));//fnFreqs[i]));
            functions.AppendLine(string.Format(functionFormat, 95, "END", 0));

            double[, ,] freqs = calculateLayerFreqs(passwords, layers);

            StringBuilder nodes = new StringBuilder();
            string nodeFormat = "<Node type=\"{2}\" id=\"{0}\" fnId=\"{1}\" />";

            StringBuilder connections = new StringBuilder();
            string connFormat = "<Con id=\"{0}\" src=\"{1}\" tgt=\"{2}\" wght=\"{3}\" />";

            int nextId = 2;
            int[] prevTargets = new int[freqs.GetLength(2)];

            // The input node has an ID of 1
            for (int i = 0; i < prevTargets.Length; i++)
                prevTargets[i] = 1;
            int outputNodes = 0;

            for (int i = 0; i < freqs.GetLength(0); i++)
            {
                int[] targets = new int[freqs.GetLength(2)];

                // Check if we ever transition to each 
                // of the k nodes in the next layer.
                // If we detect a node that never gets transitioned into,
                // then we do not need to add it to the network.
                for (int j = 0; j < freqs.GetLength(1); j++)
                    for (int k = 0; k < freqs.GetLength(2); k++)
                        if (targets[k] == 0 && freqs[i, j, k] > 0)
                        {
                            targets[k] = nextId++;
                            // Add the next character node
                            nodes.AppendLine(string.Format(nodeFormat, targets[k], k, i == freqs.GetLength(0) - 1 ? "out" : "hid"));
                            if (i == freqs.GetLength(0) - 1)
                                outputNodes++;
                        }

                for (int j = 0; j < freqs.GetLength(1); j++)
                    for (int k = 0; k < freqs.GetLength(2); k++)
                        if (freqs[i, j, k] > 0)
                            connections.AppendLine(string.Format(connFormat, nextId++, prevTargets[j], targets[k], freqs[i, j, k]));

                prevTargets = targets;
            }
            using (TextWriter writer = new StreamWriter(filename))
            {
                writer.WriteLine(SEED_START);
                writer.WriteLine(functions.ToString());
                writer.WriteLine(SEED_2);
                writer.WriteLine(nodes.ToString());
                writer.WriteLine(SEED_MIDDLE);
                writer.WriteLine(connections.ToString());
                writer.WriteLine(SEED_END);
            }
            return outputNodes;
        }

        /// <summary>
        /// Creates an adaptive Markov filter where we have a concept of "halting" nodes.
        /// In this model, each letter node contains a probability of transitioning to
        /// a halting node that simply returns the string. This removes the need to
        /// hard-code how long you want your string.
        /// </summary>
        /// <param name="filename">The file in which to save the generated model.</param>
        /// <param name="corpus">The password database from which to build the model.</param>
        /// <returns>The number of output nodes for SharpNEAT.</returns>
        public static int GenerateAdaptiveMarkovFilter(string filename, string corpus)
        {
            var passwords = PasswordUtil.LoadPasswords(corpus);
            return GenerateAdaptiveMarkovFilter(filename, passwords);
        }
        public static int GenerateAdaptiveMarkovFilter(string filename, Dictionary<string, int> passwords)
        {
            Console.WriteLine("Creating True Markov Filter");
            double[] fnFreqs = calculateFnFreqs(passwords);

            StringBuilder functions = new StringBuilder();
            string functionFormat = "<Fn id=\"{0}\" name=\"MC-{1}\" prob=\"{2}\" />";

            for (int i = 0; i < 95; i++)
                functions.AppendLine(string.Format(functionFormat, i, SecurityElement.Escape(((char)(i + 32)).ToString()), fnFreqs[i]));
            functions.AppendLine(string.Format(functionFormat, 95, "END", 0));


            double[, ,] freqs = calculateAdaptiveFreqs(passwords);

            StringBuilder nodes = new StringBuilder();
            string nodeFormat = "<Node type=\"{2}\" id=\"{0}\" fnId=\"{1}\" />";

            StringBuilder connections = new StringBuilder();
            string connFormat = "<Con id=\"{0}\" src=\"{1}\" tgt=\"{2}\" wght=\"{3}\" />";
            nodes.AppendLine(string.Format(nodeFormat, 2, 95, "out"));

            int nextId = 3;
            int[] prevTargets = new int[freqs.GetLength(2)];

            // The input node has an ID of 1
            for (int i = 0; i < prevTargets.Length; i++)
                prevTargets[i] = 1;

            int outputNodeIdx = freqs.GetLength(2) - 1;
            for (int i = 0; i < freqs.GetLength(0); i++)
            {
                int[] targets = new int[outputNodeIdx];

                // Check if we ever transition to each 
                // of the k nodes in the next layer.
                // If we detect a node that never gets transitioned into,
                // then we do not need to add it to the network.
                for (int j = 0; j < freqs.GetLength(1); j++)
                    for (int k = 0; k < outputNodeIdx; k++)
                        if (targets[k] == 0 && freqs[i, j, k] > 0)
                        {
                            targets[k] = nextId++;
                            // Add the next character node
                            nodes.AppendLine(string.Format(nodeFormat, targets[k], k, "hid"));
                        }

                for (int j = 0; j < freqs.GetLength(1); j++)
                    for (int k = 0; k <= outputNodeIdx; k++)
                        if (freqs[i, j, k] > 0)
                            connections.AppendLine(string.Format(connFormat, nextId++, prevTargets[j], k == outputNodeIdx ? 2 : targets[k], freqs[i, j, k]));
                prevTargets = targets;
            }
            using (TextWriter writer = new StreamWriter(filename))
            {
                writer.WriteLine(SEED_START);
                writer.WriteLine(functions.ToString());
                writer.WriteLine(SEED_2);
                writer.WriteLine(nodes.ToString());
                writer.WriteLine(SEED_MIDDLE);
                writer.WriteLine(connections.ToString());
                writer.WriteLine(SEED_END);
            }
            return 1;
        }


        /// <summary>
        /// Calculates the zeroth-order markov chain. That is, the probability of each
        /// character given the previous character.
        /// </summary>
        static double[][] calculateTransitionFreqs(Dictionary<string, int> passwords)
        {
            double[][] freqs = new double[96][];
            for (int i = 0; i < 96; i++)
                freqs[i] = new double[95];
            foreach (var wordcount in passwords)
            {
                if (wordcount.Key.Length == 0)
                    continue;

                int key = (int)wordcount.Key[0] - 32;
                if (key >= 0 && key < 95)
                    freqs[0][key]++;
                for (int i = 1; i < wordcount.Key.Length; i++)
                {
                    int cur = (int)wordcount.Key[i - 1] - 32 + 1;
                    int next = (int)wordcount.Key[i] - 32;
                    if (cur >= 1 && cur < 96 && next >= 0 && next < 95)
                        freqs[cur][next] += wordcount.Value;
                }
            }

            // Normalize
            for (int i = 0; i < 96; i++)
            {
                double sum = freqs[i].Sum();
                if (sum == 0)
                    continue;
                for (int j = 0; j < 95; j++)
                    freqs[i][j] = freqs[i][j] / sum;
            }

            return freqs;
        }

        /// <summary>
        /// Calculates the zeroth-order markov chain. That is, the probability of each
        /// character in the file.
        /// </summary>
        static double[] calculateFnFreqs(Dictionary<string, int> passwords)
        {
            double[] freqs = new double[95];
            for (int i = 0; i < freqs.Length; i++)
                freqs[i] = 1;

            foreach (var wc in passwords)
                for (int i = 0; i < wc.Key.Length; i++)
                {
                    int key = (int)wc.Key[i] - 32;
                    if (key >= 0 && key < 95)
                        freqs[key] += wc.Value;
                }

            double sum = freqs.Sum();
            for (int i = 0; i < freqs.Length; i++)
                freqs[i] = freqs[i] / sum;

            return freqs;
        }

        static double[, ,] calculateLayerFreqs(Dictionary<string, int> passwords, int layers)
        {
            double[, ,] freqs = new double[layers, 95, 95];

            foreach (var wordcount in passwords)
            {
                int prev = 0;
                for (int i = 0; i < wordcount.Key.Length; i++)
                {
                    int cur = (int)wordcount.Key[i] - 32;
                    if (cur < 95 && cur >= 0 && prev < 95 && prev >= 0)
                    {
                        int layer = i < layers ? i : layers - 1;
                        freqs[layer, prev, cur] += wordcount.Value;
                    }
                    prev = cur;
                }
            }

            // For every layer in this network
            for (int i = 0; i < freqs.GetLength(0); i++)
            {
                // For every node in this layer
                for (int j = 0; j < freqs.GetLength(1); j++)
                {
                    double sum = 0;
                    for (int k = 0; k < freqs.GetLength(2); k++)
                        sum += freqs[i, j, k];

                    // If we never transitioned from j to k, just skip it
                    if (sum == 0)
                        continue;

                    // Set the probabilities of transition from j to k
                    for (int k = 0; k < freqs.GetLength(2); k++)
                        freqs[i, j, k] /= sum;
                }
            }
            return freqs;
        }

        static double[, ,] calculateAdaptiveFreqs(Dictionary<string, int> passwords)
        {
            // We need to figure out the largest possible password in the database
            int layers = passwords.Keys.Max(s => s.Length) + 1;
            Console.WriteLine("Layers: {0}", layers);

            double[, ,] freqs = new double[layers, 95, 96];

            foreach (var wordcount in passwords)
            {
                int prev = 0;
                for (int i = 0; i < wordcount.Key.Length; i++)
                {
                    int cur = (int)wordcount.Key[i] - 32;
                    if (cur < 95 && cur >= 0 && prev < 95 && prev >= 0)
                        freqs[i, prev, cur] += wordcount.Value;
                    prev = cur;
                }
                // Only real difference between calculateNthOrderFreqs is that we 
                // now track the probability of a character being the end of a word.
                if (prev < 95 && prev >= 0)
                    freqs[wordcount.Key.Length, prev, 95] += wordcount.Value;
            }

            // For every layer in this network
            for (int i = 0; i < freqs.GetLength(0); i++)
            {
                // For every node in this layer
                for (int j = 0; j < freqs.GetLength(1); j++)
                {
                    double sum = 0;
                    for (int k = 0; k < freqs.GetLength(2); k++)
                        sum += freqs[i, j, k];

                    // If we never transitioned from j to k, just skip it
                    if (sum == 0)
                        continue;

                    // Set the probabilities of transition from j to k
                    for (int k = 0; k < freqs.GetLength(2); k++)
                        freqs[i, j, k] /= sum;
                }
            }
            return freqs;
        }


        const string SEED_START = "<Root>\r\n" +
  "<ActivationFunctions>\r\n";
        const string SEED_2 =
  "</ActivationFunctions>\r\n" +
  "<Networks>\r\n" +
    "<Network id=\"0\" birthGen=\"0\" fitness=\"0\">\r\n" +
      "<Nodes>\r\n" +
        "<Node type=\"bias\" id=\"0\" fnId=\"0\" />\r\n" +
        "<Node type=\"in\" id=\"1\" fnId=\"0\" />\r\n";
        const string SEED_MIDDLE =
            "</Nodes>\r\n" +
          "<Connections>\r\n";
        const string SEED_END =
          "</Connections>\r\n" +
        "</Network>\r\n" +
      "</Networks>\r\n" +
    "</Root>\r\n";
    }


}
