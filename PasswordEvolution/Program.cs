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
using SharpNeat.Utility;
using SharpNeat.Core;
using SharpNeatMarkovModels;
using System.Threading.Tasks;

namespace PasswordEvolution
{
    class Program
    {
        static PasswordEvolutionExperiment _experiment;
        static NeatEvolutionAlgorithm<NeatGenome> _ea;
        const string CHAMPION_FILE = @"..\..\..\experiments\champion.xml";
        const string CONFIG_FILE = @"..\..\..\experiments\config.xml";
        const string SEED_FILE = @"..\..\..\experiments\seed.xml";
        const string PASSWORD_MODEL_FILE = @"..\..\..\passwords\phpbb-withcount.txt";
        const string PASSWORD_FILE = @"..\..\..\passwords\phpbb-withcount.txt";
        const string RESULTS_FILE = @"..\..\..\experiments\phpbb_myspace-filtered_results.csv";
        const string TEST_MARKOV_CONFIG_FILE = @"..\..\..\experiments\test_relaxing.xml";
        const string TEST_MARKOV_SEED = @"..\..\..\experiments\test_markov_seed.xml";
        const string TEST_MARKOV_PASSWORDS = @"..\..\..\passwords\test_markov.txt";
        const string ENGLISH_WORDS = @"..\..\..\passwords\english.txt";// only 5 of 350k words have numbers
        const string MORPHED_ENGLISH_WORDS = @"..\..\..\passwords\morphed_english.txt";//10% of all words have numbers
        const string FORCED_MORPHED_ENGLISH_WORDS = @"..\..\..\passwords\forced_morphed_english.txt";//all words required to have at least one number
        const string MORPHED_SEED_FILE = @"..\..\..\experiments\morphed_seed.xml";
        const string MORPHED_CONFIG_FILE = @"..\..\..\experiments\morphed.config.xml";
        const string MORPHED_RESULTS_FILE = @"..\..\..\experiments\morphed_results.csv";
        const string HASHED_CONFIG_FILE = @"..\..\..\experiments\hashed.config.xml";
        const string HASHED_PASSWORDS_FILE = @"..\..\..\passwords\battlefield_heroes.txt";
        const string HASHED_RESULTS_FILE = @"..\..\..\experiments\hashed_results.csv";
        const string FILTERED_MYSPACE_PASSWORDS = @"..\..\..\passwords\myspace-filtered-withcount.txt";
        const int VALIDATION_GUESSES = 300000000;
        const int MAX_GENERATIONS = 200;
        static void Main(string[] args)
        {
            //morphEnglish(ENGLISH_WORDS, MORPHED_ENGLISH_WORDS);
            //morphEnglish(ENGLISH_WORDS, FORCED_MORPHED_ENGLISH_WORDS, minLength: 8);
            //RunExperiment(MORPHED_ENGLISH_WORDS, MORPHED_SEED_FILE, MORPHED_CONFIG_FILE, MORPHED_RESULTS_FILE, false);
            //RunExperiment(PASSWORD_MODEL_FILE, SEED_FILE, CONFIG_FILE, RESULTS_FILE, validateSeed: false);
            //RunExperiment(PASSWORD_MODEL_FILE, SEED_FILE, HASHED_CONFIG_FILE, HASHED_RESULTS_FILE, true);
            //PrintStats(@"..\..\..\passwords\morphed_english.txt");
            //PrintStats(@"..\..\..\passwords\forced_morphed_english.txt");
            //RunExperiment(PASSWORD_MODEL_FILE, SEED_FILE, CONFIG_FILE, RESULTS_FILE, validateSeed: false);
            //PrepareMarkovModelRuns();
            //Parallel.For(0, _datasetFilenames.Length, i => RunAllMarkovModelPairs(i));
        }

        

        private static void RunExperiment(string passwordModelFile, string seedFile, string configFile, string resultsFile, bool validateSeed = false)
        {
            Console.Write("Building Markov model...");
            var passwords = PasswordEvolutionExperiment.LoadPasswords(passwordModelFile, 8);
            int outputs = GenerateFirstOrderMarkovFilter(seedFile, passwords);
            Console.WriteLine("Done! Outputs: {0}", outputs);

            _experiment = new PasswordEvolutionExperiment();
            _experiment.OutputCount = outputs;
            
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(configFile);
            _experiment.Initialize("PasswordEvolution", xmlConfig.DocumentElement);
            PasswordCrackingEvaluator.Passwords = _experiment.Passwords;

            Console.WriteLine("Loading seed...");
            var seed = _experiment.LoadPopulation(XmlReader.Create(seedFile))[0];

            if (validateSeed)
            {
                Console.WriteLine("Validating seed model...");
                var seedModel = _experiment.CreateGenomeDecoder().Decode(seed);
                ValidateModel(seedModel, _experiment.Passwords, VALIDATION_GUESSES, _experiment.Hashed);
            }

            Console.WriteLine("Creating population...");
            // Create evolution algorithm and attach update event.
            _ea = _experiment.CreateEvolutionAlgorithm(seed);
            _ea.UpdateEvent += new EventHandler(_ea_UpdateEvent);

            // Setup results file
            using (TextWriter writer = new StreamWriter(resultsFile))
                writer.WriteLine("Generation,Champion Accounts,Champion Uniques,Average Accounts,Average Uniques,Total Accounts,Total Uniques");
            _generationalResultsFile = resultsFile;

            // Start algorithm (it will run on a background thread).
            Console.WriteLine("Starting evolution. Pop size: {0} Guesses: {1}", _experiment.DefaultPopulationSize, _experiment.GuessesPerIndividual);
            _ea.StartContinue();

            while (_ea.RunState == RunState.Running) { Thread.Sleep(1000); }

            // Validate the resulting model
            var decoder = _experiment.CreateGenomeDecoder();
            var champ = decoder.Decode(_ea.CurrentChampGenome);
            ValidateModel(champ, _experiment.Passwords, VALIDATION_GUESSES, _experiment.Hashed);
        }

        static void ValidateModel(MarkovChain model, Dictionary<string, int> passwords, int guesses, bool hashed )
        {
            Console.Write("Validating on {0} guesses... ", guesses);
            PasswordCrackingEvaluator eval = new PasswordCrackingEvaluator(guesses, hashed);
            var results = eval.Validate(model);
            Console.WriteLine("Accounts: {0} Uniques: {1}", results._fitness, results._alternativeFitness);
        }

        static int _gens = 0;
        static string _generationalResultsFile;
        static void _ea_UpdateEvent(object sender, EventArgs e)
        {
            using (TextWriter writer = new StreamWriter(_generationalResultsFile, true))
            {
                Console.WriteLine("Gen {0}: {1} ({2}) Total: {3}", _ea.CurrentGeneration,
                                                         _ea.CurrentChampGenome.EvaluationInfo.Fitness,
                                                         _ea.CurrentChampGenome.EvaluationInfo.AlternativeFitness,
                                                         _experiment.Evaluator.FoundPasswords.Count);
                lock (_experiment.Evaluator.FoundPasswords)
                {
                    Console.WriteLine("{0},{1},{2},{3},{4},{5},{6}", _ea.CurrentGeneration,
                                                                _ea.CurrentChampGenome.EvaluationInfo.Fitness,
                                                                _ea.CurrentChampGenome.EvaluationInfo.AlternativeFitness,
                                                                _ea.GenomeList.Average(g => g.EvaluationInfo.Fitness),
                                                                _ea.GenomeList.Average(g => g.EvaluationInfo.AlternativeFitness),
                                                                _experiment.Evaluator.FoundPasswords.Sum(s => PasswordCrackingEvaluator.Passwords[s]),
                                                                _experiment.Evaluator.FoundPasswords.Count);
                    writer.WriteLine("{0},{1},{2},{3},{4},{5},{6}", _ea.CurrentGeneration,
                                                                 _ea.CurrentChampGenome.EvaluationInfo.Fitness,
                                                                 _ea.CurrentChampGenome.EvaluationInfo.AlternativeFitness,
                                                                 _ea.GenomeList.Average(g => g.EvaluationInfo.Fitness),
                                                                 _ea.GenomeList.Average(g => g.EvaluationInfo.AlternativeFitness),
                                                                 _experiment.Evaluator.FoundPasswords.Sum(s => PasswordCrackingEvaluator.Passwords[s]),
                                                                 _experiment.Evaluator.FoundPasswords.Count);
                }
                Console.WriteLine("Done.");
            }
            // Save the best genome to file
            var doc = NeatGenomeXmlIO.SaveComplete(new List<NeatGenome>() { _ea.CurrentChampGenome }, true);
            doc.Save(CHAMPION_FILE);

            if (_gens >= MAX_GENERATIONS)
                _ea.Stop();

            _gens++;
        }

        static Dictionary<string, int>[] _passwords;
        static PasswordDataset[] _datasetFilenames;
        static object _writerLock = new object();
        static bool[] _finished;
        static bool AllFinished()
        {
            lock (_finished)
                foreach (bool b in _finished)
                    if (!b)
                        return false;
            return true;
        }
        static void PrepareMarkovModelRuns()
        {
            const string PASSWORD_OFFSET = @"..\..\..\passwords\";
            _datasetFilenames = new PasswordDataset[]
            {
                //new PasswordDataset(){ Filename = "faithwriters-withcount.txt", Name = "faithwriters" },
                //new PasswordDataset(){ Filename = "myspace-filtered-withcount.txt", Name = "myspace" },
                //new PasswordDataset(){ Filename = "phpbb-withcount.txt", Name = "phpbb" },
                //new PasswordDataset(){ Filename = "rockyou-withcount.txt", Name = "rockyou" },
                //new PasswordDataset(){ Filename = "singles.org-withcount.txt", Name = "singles.org" },
                new PasswordDataset() { Filename = "morphed_english.txt", Name = "training" },
                new PasswordDataset() { Filename = "forced_morphed_english.txt", Name = "testing" }
            };

            Console.WriteLine("Loading all {0} password datasets...", _datasetFilenames.Length);
            _passwords = new Dictionary<string, int>[_datasetFilenames.Length];
            for (int i = 0; i < _passwords.Length; i++)
            {
                Console.WriteLine(_datasetFilenames[i].Name);
                _passwords[i] = PasswordEvolutionExperiment.LoadPasswords(PASSWORD_OFFSET + _datasetFilenames[i].Filename);
            }
            Console.WriteLine("Done.");

            _finished = new bool[_datasetFilenames.Length];

            _experiment = new PasswordEvolutionExperiment();

            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(CONFIG_FILE);
            _experiment.Passwords = _passwords[0];
            _experiment.Initialize("PasswordEvolution", xmlConfig.DocumentElement);

            using (TextWriter writer = new StreamWriter(@"..\..\..\experiments\summary_results.csv"))
                writer.WriteLine("TrainingSet,TestingSet,Accounts Cracked,Passwords Cracked,% Accounts,% Passwords");
        }
        static void RunAllMarkovModelPairs(object special)
        {
            const string EXPERIMENT_OFFSET = @"..\..\..\experiments\intermediate\";
            string[] models = new string[]
            {
                "first-order",
                "8-layer"
            };
            


            // For every dataset, create a model
            for (int i = 0; i < _datasetFilenames.Length; i++)
            {
                if (i != (int)special)
                    continue;
                for (int m = 0; m < 2; m++)
                {
                    int outputs;
                    string seedFile = EXPERIMENT_OFFSET + "seed-" + models[m] + "-" + _datasetFilenames[i].Name + ".xml";
                    Console.Write("Building {0} Markov model...", models[m]);
                    if (m == 0)
                        outputs = GenerateFirstOrderMarkovFilter(seedFile, _passwords[i]);
                    else
                        outputs = GenerateNthOrderMarkovFilter(seedFile, _passwords[i], 8);

                    Console.WriteLine("Done! Outputs: {0}", outputs);
                    _experiment.OutputCount = outputs;

                    Console.WriteLine("Loading seed...");
                    var seed = _experiment.LoadPopulation(XmlReader.Create(seedFile))[0];

                    Console.WriteLine("Creating model...");
                    var model = _experiment.CreateGenomeDecoder().Decode(seed);

                    // For every dataset, test the model
                    for (int j = 0; j < _datasetFilenames.Length; j++)
                    {
                        //PasswordCrackingEvaluator.Passwords = passwords[j];
                        Console.Write("Validating {0} {1} model on {2} with {3} guesses... ", models[m], _datasetFilenames[i].Name, _datasetFilenames[j].Name, VALIDATION_GUESSES);
                        PasswordCrackingEvaluator eval = new PasswordCrackingEvaluator(VALIDATION_GUESSES, false);
                        eval.OneTimePasswordDeal = _passwords[j];
                        var results = eval.Validate(model, EXPERIMENT_OFFSET + models[m] + "-" + _datasetFilenames[i].Name + "-" + _datasetFilenames[j].Name + ".csv", 10000);
                        Console.WriteLine("Accounts: {0} Uniques: {1}", results._fitness, results._alternativeFitness);

                        lock(_writerLock)
                            using (TextWriter writer = new StreamWriter(@"..\..\..\experiments\summary_results.csv", true))
                                writer.WriteLine("{0},{1},{2},{3},{4}%,{5}%", 
                                    _datasetFilenames[i].Name, 
                                    _datasetFilenames[j].Name, 
                                    results._fitness, 
                                    results._alternativeFitness, 
                                    results._fitness / (double)eval.OneTimePasswordDeal.Sum(kv => kv.Value) * 100, 
                                    results._alternativeFitness / (double)eval.OneTimePasswordDeal.Count * 100); 
                    }
                }
            }
            lock(_finished)
                _finished[(int)special] = true;
        }

        static void PrintStats(string filename)
        {
            PrintStats(PasswordEvolutionExperiment.LoadPasswords(filename));
        }

        static void PrintStats(Dictionary<string, int> passwords)
        {
            int total = passwords.Count;
            int lowercase = 0;
            int numerical = 0;
            int nonAlphaNumeric = 0;
            int uppercase = 0;
            int[] lengths = new int[20];

            foreach (var kv in passwords)
            {
                bool hasNum = false, hasNonAN = false, hasUpper = false, hasLower = false;
                string key = kv.Key;

                lengths[key.Length > 20 ? 19 : key.Length - 1]++;

                for (int i = 0; i < key.Length; i++)
                {
                    char c = key[i];
                    if (c.IsLowerCase())
                    {
                        if (!hasLower)
                        {
                            hasLower = true;
                            lowercase++;
                        }
                    }
                    else if (c.IsNumeric())
                    {
                        if (!hasNum)
                        {
                            hasNum = true;
                            numerical++;
                        }
                    }
                    else if (c.IsUpperCase())
                    {
                        if (!hasUpper)
                        {
                            hasUpper = true;
                            uppercase++;
                        }
                    }
                    else
                    {
                        if (!hasNonAN)
                        {
                            hasNonAN = true;
                            nonAlphaNumeric++;
                        }
                    }
                }
            }

            Console.WriteLine("Total Accounts: {0}", passwords.Sum(kp => kp.Value));
            Console.WriteLine("Total Unique Passwords: {0}", total);
            Console.WriteLine("- Contain lowercase: {0} ({1:N2}%)", lowercase, lowercase / (double)total * 100);
            Console.WriteLine("- Contain uppercase: {0} ({1:N2}%)", uppercase, uppercase / (double)total * 100);
            Console.WriteLine("- Contain number:    {0} ({1:N2}%)", numerical, numerical / (double)total * 100);
            Console.WriteLine("- Contain other:     {0} ({1:N2}%)", nonAlphaNumeric, nonAlphaNumeric / (double)total * 100);
            Console.WriteLine("Length distributions:");
            for (int i = 0; i < lengths.Length; i++)
                if (lengths[i] != 0)
                    Console.WriteLine("{2}{0}: {1}", i + 1, lengths[i], i == lengths.Length - 1 ? ">= " : "");
        }

        static int GenerateFirstOrderMarkovFilter(string filename, string corpus)
        {
            var passwords = PasswordEvolutionExperiment.LoadPasswords(corpus);
            return GenerateFirstOrderMarkovFilter(filename, passwords);
        }
            
        static int GenerateFirstOrderMarkovFilter(string filename, Dictionary<string, int> passwords)
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

            return 95;
        }

        static int GenerateNthOrderMarkovFilter(string filename, string corpus, int layers)
        {
            var passwords = PasswordEvolutionExperiment.LoadPasswords(corpus);
            return GenerateNthOrderMarkovFilter(filename, passwords, layers);
        }
        static int GenerateNthOrderMarkovFilter(string filename, Dictionary<string, int> passwords, int layers)
        {
            Console.WriteLine("Creating Nth-Order Markov Filter. Layers: {0}", layers);
            double[] fnFreqs = calculateFnFreqs(passwords);

            StringBuilder functions = new StringBuilder();
            string functionFormat = "<Fn id=\"{0}\" name=\"MC-{1}\" prob=\"{2}\" />";

            for (int i = 0; i < 95; i++)
                functions.AppendLine(string.Format(functionFormat, i, SecurityElement.Escape(((char)(i + 32)).ToString()), 0));//fnFreqs[i]));
            functions.AppendLine(string.Format(functionFormat, 95, "END", 0));

            double[, ,] freqs = calculateNthOrderFreqs(passwords, layers);

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
                            if(i == freqs.GetLength(0) - 1)
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

        static int GenerateTrueMarkovFilter(string filename, string corpus, int layers)
        {
            var passwords = PasswordEvolutionExperiment.LoadPasswords(corpus);
            return GenerateTrueMarkovFilter(filename, passwords);
        }
        static int GenerateTrueMarkovFilter(string filename, Dictionary<string, int> passwords)
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
            foreach(var wordcount in passwords)
            {
                if (wordcount.Key.Length == 0)
                    continue;

                int key = (int)wordcount.Key[0] - 32;
                if(key >= 0 && key < 95)
                    freqs[0][key]++;
                for (int i = 1; i < wordcount.Key.Length; i++)
                {
                    int cur = (int)wordcount.Key[i - 1] - 32 + 1;
                    int next = (int)wordcount.Key[i] - 32;
                    if(cur >= 1 && cur < 96 && next >= 0 && next < 95)
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

            foreach(var wc in passwords)
                for (int i = 0; i < wc.Key.Length; i++)
                {
                    int key = (int)wc.Key[i] - 32;
                    if(key >= 0 && key < 95)
                        freqs[key] += wc.Value;
                }
            
            double sum = freqs.Sum();
            for (int i = 0; i < freqs.Length; i++)
                freqs[i] = freqs[i] / sum;

            return freqs;
        }

        static double[,,] calculateNthOrderFreqs(Dictionary<string,int> passwords, int layers)
        {
            double[,,] freqs = new double[layers,95,95];

            foreach(var wordcount in passwords)
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
            int layers = passwords.Keys.Max(s=> s.Length) + 1;
            Console.WriteLine("Layers: {0}", layers);
            
            double[,,] freqs = new double[layers, 95, 96];

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

        static void morphEnglish(string inFile, string outFile, bool counts = false, int minLength = 0)
        {
            var english = PasswordEvolutionExperiment.LoadPasswords(inFile);
            var morphed = morphEnglish(english, minLength);
            using (TextWriter writer = new StreamWriter(outFile))
                foreach (var kv in morphed)
                    if (counts)
                        writer.WriteLine("{0} {1}", kv.Value, kv.Key);
                    else
                        writer.WriteLine(kv.Key);
        }

        static Dictionary<string, int> morphEnglish(Dictionary<string, int> english, int minLength = 0)
        {
            Dictionary<string, int> results = new Dictionary<string, int>();

            FastRandom random = new FastRandom();
            double[] digitProbs = new double[]
            {
                0.14, //0
                0.3,//1
                0.07,//2
                0.07,//3
                0.07, //4
                0.07, //5
                0.07, //6
                0.07, //7
                0.07, //8
                0.07 //9
            };
            double[] posProbs = new double[]
            {
                //0.9,
                //0.025,
                //0.06,
                //0.015
                0,//do nothing
                0.25,//prepend
                0.6,//append
                0.15//random
            };
            Console.WriteLine("Probs sum: {0}", digitProbs.Sum());
            RouletteWheelLayout digitLayout = new RouletteWheelLayout(digitProbs);
            RouletteWheelLayout posLayout = new RouletteWheelLayout(posProbs);
            int alreadyNumbered = 0;
            foreach (string s in english.Keys)
            {
                bool numbered = false;
                for(int i = 0; i < s.Length; i++)
                    if (s[i] >= '0' && s[i] <= '9')
                    {
                        alreadyNumbered++;
                        numbered = true;
                        break;
                    }
                string morphedPassword = s;
                while(!numbered || morphedPassword.Length < minLength)
                {
                    int toAdd = RouletteWheel.SingleThrow(digitLayout, random);
                    int pos = RouletteWheel.SingleThrow(posLayout, random);

                    if (pos == 0)
                        break;
                    else if (pos == 1)
                        morphedPassword = toAdd + morphedPassword;
                    else if (pos == 2)
                        morphedPassword = morphedPassword + toAdd;
                    else
                    {
                        pos = random.Next(morphedPassword.Length);
                        morphedPassword = morphedPassword.Substring(0, pos) + toAdd + morphedPassword.Substring(pos, morphedPassword.Length - pos);
                    }
                    numbered = true;
                }
                int val;
                if (!results.TryGetValue(morphedPassword, out val))
                    results.Add(morphedPassword, 1);
            }
            Console.WriteLine("Had numbers already: {0}", alreadyNumbered);
            return results;
        }

        private static void ProcessHackForumsFile()
        {
            using (TextReader reader = new StreamReader(@"..\..\..\passwords\hackforums.sql"))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("INSERT INTO"))
                        break;
                }
                using (TextWriter writer = new StreamWriter(@"..\..\..\passwords\hackforums.txt"))
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line == "")
                            continue;
                        int quotes = 0;
                        int start = 0;
                        int end = 0;
                        for (int i = 0; i < line.Length; i++)
                            if (line[i] == '\'')
                            {
                                quotes++;
                                if (quotes == 3)
                                    start = i + 1;
                                if (quotes == 4)
                                {
                                    end = i;
                                    break;
                                }
                            }
                        string password = line.Substring(start, end - start);

                        start = end + 4;
                        end = line.IndexOf("\'", start + 1);
                        string salt = line.Substring(start, end - start);
                        writer.WriteLine("\"\";\"{0}\";\"{1}\"", password, salt);
                    }
            }
        }

        private static void ProcessBattlefieldHeroesFile()
        {
            Dictionary<string, int> passwords = new Dictionary<string, int>();
            using (TextReader reader = new StreamReader(@"..\..\..\passwords\battlefield_heroes.csv"))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line == "")
                        continue;

                    var tokens = line.Split(';');
                    string pw = tokens.Last().Trim('"');
                    int val;
                    if (!passwords.TryGetValue(pw, out val))
                        passwords.Add(pw, 0);

                    passwords[pw]++;
                }
            }
            using (TextWriter writer = new StreamWriter(@"..\..\..\passwords\battlefield_heroes.txt"))
                foreach (var kv in passwords.OrderByDescending(s => s.Value))
                    writer.WriteLine(kv.Value + " " + kv.Key);
        }

        private static void ProcessMySpaceFile()
        {
            using (TextReader reader = new StreamReader(@"..\..\..\passwords\myspace-unfiltered-withcount.txt"))
            {
                using(TextWriter writer = new StreamWriter(@"..\..\..\passwords\myspace-filtered-withcount.txt"))
                {
                    string line = null;
                    while((line = reader.ReadLine()) != null)
                    {
                        if(line == "")
                            continue;
                        
                        line = line.TrimStart();
                        string[] tokens = line.Split();
                        string pw = tokens.Length == 1 ? line : tokens.Skip(1).Concatenate(" ");
                        
                        if(!pw.ContainsNumber() || pw.Length > 20 || pw.Length < 6)
                            continue;

                        writer.WriteLine(line);
                    }
                }
            }
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
