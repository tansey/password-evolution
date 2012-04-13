using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeatMarkovModels;
using System.Threading.Tasks;
using System;
using System.IO;
using PasswordEvolution;


namespace CondorApp
{
    public class CondorEvaluator : IGenomeListEvaluator<NeatGenome>
    {
        readonly string BASE_DIR;
        readonly string CONDOR_FILE;
        readonly string CONFIG_FILE;
        readonly string PASSWORDS_FILE;
        const string EXECUTABLE_PATH = "/u/tansey/password-evolution/executable/ModelEvaluator.exe";
        const string GENOMES_DIR = "genomes/";
        const string RESULTS_DIR = "results/";
        const string FINISHED_FLAGS_DIR = "finished_flags/";
        const string CHAMPIONS_DIR = "champions/";
        const string OUTPUT_DIR = "output/";
        const string CONDOR_LOGS_DIR = "condor_logs/";
        const string ERROR_DIR = "error/";
        readonly string GENOMES_FORMAT;
        readonly string RESULTS_FORMAT;
        readonly string FINISHED_FLAG_FORMAT;
        readonly string CHAMPIONS_FORMAT;
        readonly string OUTPUT_FORMAT;
        readonly string CONDOR_LOG_FORMAT;
        readonly string ERROR_FORMAT;
        readonly int? PASSWORD_LENGTH;
        readonly string USER_GROUP;

        ulong _evaluationCount;
        int _generations;
        Dictionary<string, double> _passwords;

        /// <summary>
        /// Construct a new distributed evaluator that leverages the UTCS Condor cluster.
        /// Note that experimentDir must already exist, but it can be empty.
        /// If the passwords dictionary is specified, then it will be written to a temporary
        /// file every generation and used for evaluation. If it's null, then the password
        /// file in the config file will be used.
        /// </summary>
        public CondorEvaluator(string experimentDir, string configFile, 
                                CondorGroup userGroup,
                                Dictionary<string,double> passwords = null,
                                int? passwordLength = null)
        {
            // convert to unix pathnames
            experimentDir = experimentDir.Replace('\\', '/');
            string originalConfig = configFile.Replace('\\', '/');
            
            // append directory
            if(!experimentDir.EndsWith("/"))
                experimentDir += "/";

            
            switch (userGroup)
            {
                case CondorGroup.Grad: USER_GROUP = "GRAD";
                    break;
                case CondorGroup.Undergrad: USER_GROUP = "UNDER";
                    break;
                default: throw new Exception("Unknown group");
            }

            // set the directories for the experiments, creating if necessary
            BASE_DIR = experimentDir;
            CONFIG_FILE = experimentDir + "config.xml";
            CONDOR_FILE = experimentDir + "jobs";
            GENOMES_FORMAT = createFileFormat(GENOMES_DIR, "genome_{0}.xml");
            RESULTS_FORMAT = createFileFormat(RESULTS_DIR, "results_{0}.txt");
            FINISHED_FLAG_FORMAT = createFileFormat(FINISHED_FLAGS_DIR, "finished_{0}");
            CHAMPIONS_FORMAT = createFileFormat(CHAMPIONS_DIR, "champion_{0}.xml");
            OUTPUT_FORMAT = createFileFormat(OUTPUT_DIR, "output_{0}.out");
            CONDOR_LOG_FORMAT = createFileFormat(CONDOR_LOGS_DIR, "job_{0}.log");
            ERROR_FORMAT = createFileFormat(ERROR_DIR, "error_{0}.log");

            // Copy the config file to the experiment directory
            File.Copy(originalConfig, CONFIG_FILE, true);

            if (passwords != null)
            {
                _passwords = passwords;
                PASSWORDS_FILE = experimentDir + "passwords.txt";
            }
            PASSWORD_LENGTH = passwordLength;
        }

        private string createFileFormat(string subdir, string format)
        {
            if (!Directory.Exists(BASE_DIR + subdir))
                Directory.CreateDirectory(BASE_DIR + subdir);

            return BASE_DIR + subdir + format;
        }

        public void Evaluate(IList<NeatGenome> genomeList)
        {
            // Clear any old flags from previous generations
            foreach (var flag in Directory.GetFiles(FINISHED_FLAGS_DIR))
                File.Delete(flag);

            // Write the genomes to file
            writeGenomes(genomeList);

            // If we're using a dynamic password value, 
            // write the new pw database to file
            if (_passwords != null)
                writePasswords();

            // Create the condor jobs
            createCondorJobs(genomeList);

            // Submit the condor jobs using condor_submit
            submitCondorJobs();
            
            // Wait for all the evaluations to finish
            waitForEvaluations(genomeList.Count);

            // Read in the results
            loadResults(genomeList);
            
            // Log the champion manually here
            saveChampion(genomeList);

            _evaluationCount += (ulong)genomeList.Count;
            _generations++;
        }

        private void writePasswords()
        {
            using (TextWriter writer = new StreamWriter(PASSWORDS_FILE))
                foreach (var kv in _passwords)
                    writer.WriteLine("{0} {1}", kv.Value, kv.Key);
        }

        private void submitCondorJobs()
        {
            System.Diagnostics.ProcessStartInfo procStartInfo =
                new System.Diagnostics.ProcessStartInfo("condor_submit", CONDOR_FILE);

            // The following commands are needed to redirect the standard output.
            // This means that it will be redirected to the Process.StandardOutput StreamReader.
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            // Do not create the black window.
            procStartInfo.CreateNoWindow = true;
            // Now we create a process, assign its ProcessStartInfo and start it
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();
            // Get the output into a string
            string result = proc.StandardOutput.ReadToEnd();
            // Display the command output.
            Console.WriteLine(result);
        }

        private void createCondorJobs(IList<NeatGenome> genomeList)
        {
            using (TextWriter writer = new StreamWriter(CONDOR_FILE))
            {
                writer.WriteLine("universe = vanilla");
                writer.WriteLine("Initialdir = " + BASE_DIR);
                writer.WriteLine("Executable=/lusr/opt/mono-2.10.8/bin/mono");
                writer.WriteLine(string.Format("+Group   = \"{0}\"", USER_GROUP));
                writer.WriteLine("+Project = \"AI_ROBOTICS\"");
                writer.WriteLine("+ProjectDescription = \"Password evolution experiments\"");

                for (int i = 0; i < genomeList.Count; i++)
                {
                    writer.WriteLine("Log = " + string.Format(CONDOR_LOG_FORMAT, i));
                    writer.WriteLine("Arguments = " + EXECUTABLE_PATH + " "
                        + string.Format("{0} {1} {2} {3} {4} {5}",
                        i, string.Format(GENOMES_FORMAT, i),
                        string.Format(RESULTS_FORMAT, i),
                        string.Format(FINISHED_FLAG_FORMAT, i),
                        CONFIG_FILE,
                        PASSWORDS_FILE == null ? "" : PASSWORDS_FILE,
                        PASSWORD_LENGTH.HasValue ? PASSWORD_LENGTH.Value.ToString() : ""
                        ));
                    writer.WriteLine("Output = " + string.Format(OUTPUT_FORMAT, i));
                    writer.WriteLine("Error = " + string.Format(ERROR_FORMAT, i));
                    writer.WriteLine("Queue 1");
                }
            }
        }

        private void writeGenomes(IList<NeatGenome> genomeList)
        {
            for (int i = 0; i < genomeList.Count; i++)
            {
                var genome = genomeList[i];
                var doc = NeatGenomeXmlIO.SaveComplete(new List<NeatGenome>() { genome }, true);
                doc.Save(string.Format(GENOMES_FORMAT, i));
            }
        }

        private void saveChampion(IList<NeatGenome> genomeList)
        {
            var champ = genomeList.ArgMax(g => g.EvaluationInfo.Fitness);
            var champDoc = NeatGenomeXmlIO.SaveComplete(new List<NeatGenome>() { champ }, true);
            champDoc.Save(string.Format(CHAMPIONS_FORMAT, _generations));
        }

        private void loadResults(IList<NeatGenome> genomeList)
        {
            for (int a = 0; a < genomeList.Count; a++)
            {
                NeatGenome genome = genomeList[a];
                using (TextReader reader = new StreamReader(string.Format(RESULTS_FORMAT, a)))
                {
                    string line = reader.ReadLine();
                    string[] values = line.Split(' ');
                    genome.EvaluationInfo.SetFitness(double.Parse(values[0]));
                    genome.EvaluationInfo.AlternativeFitness = double.Parse(values[1]);
                }
            }
        }

        private static void waitForEvaluations(int totalNumberGenomes)
        {
            int numberGenomes = 0;
            do
            {
                string[] flags = Directory.GetFiles(FINISHED_FLAGS_DIR);
                numberGenomes = flags.Length;

                // Don't hog the CPU while we're waiting for the evaluation to finish
                System.Threading.Thread.Sleep(1000);

            } while (numberGenomes != totalNumberGenomes);
        }

        public ulong EvaluationCount
        {
            get { return _evaluationCount; }
        }

        public void Reset()
        {
            
        }

        public bool StopConditionSatisfied
        {
            get { return false; }
        }
    }
}
