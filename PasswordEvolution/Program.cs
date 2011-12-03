using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SharpNeat.Genomes.Neat;
using SharpNeat.EvolutionAlgorithms;
using System.Threading;

namespace PasswordEvolution
{
    class Program
    {
        static PasswordEvolutionExperiment _experiment;
        static NeatEvolutionAlgorithm<NeatGenome> _ea;
        const string CONFIG_FILE = @"..\..\..\experiments\config.xml";
        const string CHAMPION_FILE = @"..\..\..\experiments\champion.xml";

        static void Main(string[] args)
        {
            _experiment = new PasswordEvolutionExperiment();

            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(CONFIG_FILE);
            _experiment.Initialize("PasswordEvolution", xmlConfig.DocumentElement);

            // Create evolution algorithm and attach update event.
            _ea = _experiment.CreateEvolutionAlgorithm();
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
    }
}
