using System;
using System.Collections.Generic;
using System.Linq;
using MulticoreProcessorScheduler.Models;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace MulticoreProcessorScheduler
{
    class Program
    {
        private static ImportFileSize defaultSize = ImportFileSize.Medium;
        static void Main(string[] args)
        {
            ImportFileSize fileSize = defaultSize;
            if (args.Count() > 0) {
                fileSize = GetSizeFromArgs(args);
            }
            XmlReader.Read(fileSize, out var tasks, out var processors);
            
            Console.WriteLine(fileSize);
            Console.WriteLine("Task count: " + tasks.Count());

            FindAndPresentResult(tasks, processors, fileSize);

            // CalcTotalLaxityOfSolution(tasks, processors);

            // GetAllSolutionListInCSV(tasks, processors);
            Console.WriteLine();
        }

        public static void FindAndPresentResult(List<Models.Task> tasks, List<Processor> processors, ImportFileSize fileSize) {
            Console.WriteLine();
            PrintLine();
            
            Console.WriteLine("Statistics:\n");
            Stopwatch s1 = new Stopwatch();
            s1.Start(); 
            (double, Solution) bestSolution;
            bestSolution = SimulatedAnnealing.FindOptimalSolution(tasks, processors);
            Console.WriteLine("Best Solution: ");
            Console.WriteLine("\tTotal laxity: " + bestSolution.Item1);
            Console.WriteLine();
            s1.Stop();
            Console.WriteLine("Time: " + s1.Elapsed.ToString());
            s1.Reset();
            PrintLine();

            string filepath = bestSolution.Item2.toXML(bestSolution.Item1, fileSize.ToString());
            Console.WriteLine("Solution has been saved at: " + filepath);

            // PrintLine();
            // var lines = File.ReadAllLines(filepath).ToList();
            // lines.ForEach(l => Console.WriteLine(l));
            // PrintLine();

        }

        public static void CalcTotalLaxityOfSolution(List<Models.Task> tasks, List<Processor> processors) 
        {
            PrintLine();
            var exampleSolution = new Solution() {
                AssignedTasks = new List<AssignedTask> {
                    new AssignedTask(tasks[7], processors[0].Cores[0], 0),
                    new AssignedTask(tasks[0], processors[0].Cores[1], 0),
                    new AssignedTask(tasks[8], processors[0].Cores[2], 0),
                    new AssignedTask(tasks[1], processors[0].Cores[3], 0),
                    new AssignedTask(tasks[5], processors[1].Cores[0], 0),
                    new AssignedTask(tasks[6], processors[1].Cores[1], 0),
                    new AssignedTask(tasks[2], processors[1].Cores[2], 0),
                    new AssignedTask(tasks[3], processors[1].Cores[2], 0),
                    new AssignedTask(tasks[4], processors[1].Cores[3], 0),
                }
            };
            exampleSolution.AssignedTasks = exampleSolution.AssignedTasks
                .OrderBy(at => at.Task.Deadline)
                .ThenByDescending(at => at.Wcet)
                .ToList();


            SimulatedAnnealing.ResponseTimeAnalysis(exampleSolution);
            Console.WriteLine("Manually typed solution:");
            Console.WriteLine(exampleSolution.ToString());
            Console.WriteLine("Total Laxity: " + SimulatedAnnealing.TotalLaxity(exampleSolution));
            PrintLine();
            
        }

        public static void GetAllSolutionListInCSV(List<Models.Task> tasks, List<Processor> processors) 
        {
            PrintLine();
            string path = @"csv_files/test.csv";         

            var lines = new List<string>();
            // print list of solutions
            var tuples = SimulatedAnnealing.FindOptimalSolution_test(tasks, processors);
            foreach (var tuple in tuples)
            {
                // Console.WriteLine(tuple.Item1);
                lines.Add(tuple.Item1.ToString());
            }
            File.WriteAllLines(path, lines);

            printTuples(tuples);
            PrintLine();
        }

        private static ImportFileSize GetSizeFromArgs(string[] args)
		{
            if (args.Count() == 0) return defaultSize;
            switch (args[0]) {
                case "small":
                case "Small":
                case "-small":
                case "--small":
                case "-Small":
                case "--Small":
                    return ImportFileSize.Small;
                case "medium":
                case "Medium":
                case "-medium":
                case "--medium":
                case "-Medium":
                case "--Medium":
                    return ImportFileSize.Medium;
                case "large":
                case "Large":
                case "-large":
                case "--large":
                case "-Large":
                case "--Large":
                    return ImportFileSize.Large;                
                default: 
                    return defaultSize;
            }
		}

		public static void printTuples(List<(double, Solution)> tuples)
        {
            string indent = "";
            Console.WriteLine("Solutions (" + tuples.Count() + ") status: ");
            indent += "\t";
            Console.WriteLine(indent + "First 5: ");
            indent += "\t";
            tuples.Take(5).ToList().ForEach(t => Console.WriteLine(indent + t.Item1));
            indent = indent.Substring(0, indent.Length-1);
            Console.WriteLine(indent + "Last 5: ");
            indent += "\t";
            tuples.Skip(tuples.Count()-5).ToList().ForEach(t => Console.WriteLine(indent + t.Item1));
            indent = indent.Substring(0, indent.Length-1);
            Console.WriteLine(indent + "Best 5: ");
            indent += "\t";
            tuples.OrderBy(t => t.Item1).Skip(tuples.Count() - 5).ToList().ForEach(t => Console.WriteLine(indent + t.Item1));
        }

        public static void PrintLine() {
            Console.WriteLine("============================================");
        }
    }
}
