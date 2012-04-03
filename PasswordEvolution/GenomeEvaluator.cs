using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SharpNeat.Genomes.Neat;
using System.Xml;
using SharpNeatMarkovModels;
using SharpNeat.Core;

namespace PasswordEvolution
{
    public static class GenomeEvaluator
    {
        //static IGenomeDecoder<NeatGenome, MarkovChain> _genomeDecoder;
        //static PasswordCrackingEvaluator _passwordCrackingEvaluator;



        public static void Evaluate(IGenomeDecoder<NeatGenome, MarkovChain> genomeDecoder, PasswordCrackingEvaluator passwordCrackingEvaluator, PasswordEvolutionExperiment experiment)
        {

            string[] genomeFiles = Directory.GetFiles(@"..\..\..\experiments\genomes\", "*.xml");

            XmlDocument doc = new XmlDocument();
            int genomeNumber;


            foreach (string genomeFile in genomeFiles)
            {
                // Read in genome
                doc.Load(genomeFile);

                //NeatGenome genome = NeatGenomeXmlIO.LoadGenome(doc, false);
                //NeatGenomeFactory genomeFactory = (NeatGenomeFactory)CreateGenomeFactory();

                NeatGenome genome = experiment.LoadPopulation(XmlReader.Create(genomeFile))[0];
                MarkovChain phenome = experiment.CreateGenomeDecoder().Decode(genome);//genomeDecoder.Decode(genome);

                string[] filePath = genomeFile.Split('\\');
                string[] fileName = (filePath[filePath.Length - 1]).Split('-');


                String fileNumber = (fileName[1]).Split('.')[0];

                genomeNumber = Convert.ToInt32(fileNumber);


                //FileStream fs = File.Open(@"..\..\..\experiments\genomes\genome-results\genome-"+genomeNumber+"-results.txt", FileMode.CreateNew, FileAccess.Write);
                TextWriter tw = new StreamWriter(@"..\..\..\experiments\genomes\genome-results\genome-" + genomeNumber + "-results.txt");

                // Evaluate
                if (null == phenome)
                {   // Non-viable genome.
                    tw.WriteLine("0.0 0.0");
                }
                else
                {
                    FitnessInfo fitnessInfo = passwordCrackingEvaluator.Evaluate(phenome);
                    double val = fitnessInfo._fitness;
                    double val2 = fitnessInfo._alternativeFitness;
                    tw.WriteLine(fitnessInfo._fitness + " " + fitnessInfo._alternativeFitness);
                }
                tw.Close();
                File.Create(@"..\..\..\experiments\genomes\genome-finished\genome-" + genomeNumber + "-finished.txt");
            }

            // Write results?? -> genome_#_results
            // Write finished flag -> genome_#_finished

        }


    }
}
