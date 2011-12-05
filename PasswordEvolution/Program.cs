using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SharpNeat.Genomes.Neat;
using SharpNeat.EvolutionAlgorithms;
using System.Threading;
using System.IO;
using System.Security;

namespace PasswordEvolution
{
    class Program
    {
        static PasswordEvolutionExperiment _experiment;
        static NeatEvolutionAlgorithm<NeatGenome> _ea;
        const string CONFIG_FILE = @"..\..\..\experiments\config.xml";
        const string CHAMPION_FILE = @"..\..\..\experiments\champion.xml";
        const string SEED_FILE = @"..\..\..\experiments\seed.xml";
        const string PASSWORD_FILE = @"..\..\..\experiments\password.lst";

        static void Main(string[] args)
        {
            _experiment = new PasswordEvolutionExperiment();

            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(CONFIG_FILE);
            _experiment.Initialize("PasswordEvolution", xmlConfig.DocumentElement);

            //GenerateMarkovFilter(SEED_FILE, PASSWORD_FILE);
            var seed = _experiment.LoadPopulation(XmlReader.Create(SEED_FILE))[0];

            // Create evolution algorithm and attach update event.
            _ea = _experiment.CreateEvolutionAlgorithm(seed);
            _ea.UpdateEvent += new EventHandler(_ea_UpdateEvent);
            // Start algorithm (it will run on a background thread).
            _ea.StartContinue();

            while (true) { Thread.Sleep(1000); }
        }

        static void _ea_UpdateEvent(object sender, EventArgs e)
        {
            Console.WriteLine("Gen {0}: {1} Total: {2}", _ea.CurrentGeneration, 
                                                         _ea.CurrentChampGenome.EvaluationInfo.Fitness,
                                                         _experiment.Evaluator.FoundPasswords.Count);

            // Save the best genome to file
            var doc = NeatGenomeXmlIO.SaveComplete(new List<NeatGenome>() { _ea.CurrentChampGenome }, true);
            doc.Save(CHAMPION_FILE);
        }

        static void GenerateMarkovFilter(string filename, string corpus)
        {
            double[] fnFreqs = calculateFnFreqs(corpus);

            StringBuilder functions = new StringBuilder();
            string functionFormat = "<Fn id=\"{0}\" name=\"MC-{1}\" prob=\"{2}\" />";

            for (int i = 0; i < 95; i++)
                functions.AppendLine(string.Format(functionFormat, i, SecurityElement.Escape(((char)(i + 32)).ToString()), fnFreqs[i]));


            double[][] freqs = calculateTransitionFreqs(corpus);

            StringBuilder nodes = new StringBuilder();
            string nodeFormat = "<Node type=\"out\" id=\"{0}\" fnId=\"{1}\" />";
            
            StringBuilder connections = new StringBuilder();
            string connFormat = "<Con id=\"{0}\" src=\"{1}\" tgt=\"{2}\" wght=\"{3}\" />";

            int nextId = 97;

            for (int i = 0; i < 95; i++)
            {
                int nodeId = i+2;

                // Add the next character node
                nodes.AppendLine(string.Format(nodeFormat, nodeId, i));

                // Add a connection from the origin to the character node
                if(freqs[0][i] > 0)
                    connections.AppendLine(string.Format(connFormat, nextId++, 1, nodeId, freqs[0][i]));

                for(int j = 0; j < 95; j++)
                    if(freqs[i+1][j] > 0)
                        connections.AppendLine(string.Format(connFormat, nextId++, nodeId, j+2, freqs[i+1][j]));
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
        }

        static double[][] calculateTransitionFreqs(string filename)
        {
            double[][] freqs = new double[96][];
            for (int i = 0; i < 96; i++)
                freqs[i] = new double[95];
            using (TextReader reader = new StreamReader(filename))
            {
                string line = reader.ReadLine();
                while ((line = reader.ReadLine()) != null)
                {
                    if (line == "")
                        continue;
                    freqs[0][(int)line[0] - 32]++;
                    for (int i = 1; i < line.Length; i++)
                        freqs[(int)line[i - 1] - 32][(int)line[i] - 32]++;
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

        static double[] calculateFnFreqs(string filename)
        {
            double[] freqs = new double[95];
            for (int i = 0; i < freqs.Length; i++)
                freqs[i] = 1;

            using (TextReader reader = new StreamReader(filename))
            {
                string line = reader.ReadLine();
                while ((line = reader.ReadLine()) != null)
                {
                    if (line == "")
                        continue;
                    for (int i = 0; i < line.Length; i++)
                        freqs[(int)line[i] - 32]++;
                }
            }

            double sum = freqs.Sum();
            for (int i = 0; i < freqs.Length; i++)
                freqs[i] = freqs[i] / sum;

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
