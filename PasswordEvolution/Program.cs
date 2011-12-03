using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SharpNeat.Genomes.Neat;
using SharpNeat.EvolutionAlgorithms;
using System.Threading;
using System.IO;

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
            Console.WriteLine("Gen {0}: {1}", _ea.CurrentGeneration, _ea.CurrentChampGenome.EvaluationInfo.Fitness);

            // Save the best genome to file
            var doc = NeatGenomeXmlIO.SaveComplete(new List<NeatGenome>() { _ea.CurrentChampGenome }, true);
            doc.Save(CHAMPION_FILE);
        }

        static void GenerateMarkovFilter(string filename, string corpus)
        {
            double[][] freqs = calculateFreqs(corpus);

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
                writer.WriteLine(nodes.ToString());
                writer.WriteLine(SEED_MIDDLE);
                writer.WriteLine(connections.ToString());
                writer.WriteLine(SEED_END);
            }
        }

        static double[][] calculateFreqs(string filename)
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

        const string SEED_START = "<Root>\r\n" +
  "<ActivationFunctions>\r\n" +
    "<Fn id=\"0\" name=\"MC- \" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"1\" name=\"MC-!\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"2\" name=\"MC-&quot;\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"3\" name=\"MC-#\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"4\" name=\"MC-$\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"5\" name=\"MC-%\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"6\" name=\"MC-&amp;\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"7\" name=\"MC-'\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"8\" name=\"MC-(\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"9\" name=\"MC-)\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"10\" name=\"MC-*\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"11\" name=\"MC-+\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"12\" name=\"MC-,\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"13\" name=\"MC--\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"14\" name=\"MC-.\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"15\" name=\"MC-/\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"16\" name=\"MC-0\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"17\" name=\"MC-1\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"18\" name=\"MC-2\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"19\" name=\"MC-3\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"20\" name=\"MC-4\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"21\" name=\"MC-5\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"22\" name=\"MC-6\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"23\" name=\"MC-7\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"24\" name=\"MC-8\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"25\" name=\"MC-9\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"26\" name=\"MC-:\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"27\" name=\"MC-;\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"28\" name=\"MC-&lt;\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"29\" name=\"MC-=\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"30\" name=\"MC-&gt;\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"31\" name=\"MC-?\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"32\" name=\"MC-@\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"33\" name=\"MC-A\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"34\" name=\"MC-B\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"35\" name=\"MC-C\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"36\" name=\"MC-D\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"37\" name=\"MC-E\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"38\" name=\"MC-F\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"39\" name=\"MC-G\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"40\" name=\"MC-H\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"41\" name=\"MC-I\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"42\" name=\"MC-J\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"43\" name=\"MC-K\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"44\" name=\"MC-L\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"45\" name=\"MC-M\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"46\" name=\"MC-N\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"47\" name=\"MC-O\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"48\" name=\"MC-P\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"49\" name=\"MC-Q\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"50\" name=\"MC-R\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"51\" name=\"MC-S\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"52\" name=\"MC-T\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"53\" name=\"MC-U\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"54\" name=\"MC-V\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"55\" name=\"MC-W\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"56\" name=\"MC-X\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"57\" name=\"MC-Y\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"58\" name=\"MC-Z\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"59\" name=\"MC-[\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"60\" name=\"MC-\\\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"61\" name=\"MC-]\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"62\" name=\"MC-^\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"63\" name=\"MC-_\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"64\" name=\"MC-`\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"65\" name=\"MC-a\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"66\" name=\"MC-b\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"67\" name=\"MC-c\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"68\" name=\"MC-d\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"69\" name=\"MC-e\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"70\" name=\"MC-f\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"71\" name=\"MC-g\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"72\" name=\"MC-h\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"73\" name=\"MC-i\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"74\" name=\"MC-j\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"75\" name=\"MC-k\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"76\" name=\"MC-l\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"77\" name=\"MC-m\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"78\" name=\"MC-n\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"79\" name=\"MC-o\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"80\" name=\"MC-p\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"81\" name=\"MC-q\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"82\" name=\"MC-r\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"83\" name=\"MC-s\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"84\" name=\"MC-t\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"85\" name=\"MC-u\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"86\" name=\"MC-v\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"87\" name=\"MC-w\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"88\" name=\"MC-x\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"89\" name=\"MC-y\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"90\" name=\"MC-z\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"91\" name=\"MC-{\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"92\" name=\"MC-|\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"93\" name=\"MC-}\" prob=\"0.010526315789473684\" />\r\n" +
    "<Fn id=\"94\" name=\"MC-~\" prob=\"0.010526315789473684\" />\r\n" +
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
