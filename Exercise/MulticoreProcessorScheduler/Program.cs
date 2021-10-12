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
        private static ImportFileSize defaultSize = ImportFileSize.Small;
        static void Main(string[] args)
        {
            ImportFileSize fileSize = defaultSize;
            if (args.Count() > 0) {
                fileSize = GetSizeFromArgs(args);
            }
            XmlReader.Read(fileSize, out var tasks, out var processors);
            
            Console.WriteLine(fileSize);
            Console.WriteLine("Task count: " + tasks.Count());
            // foreach (var processor in processors) {
            //     Console.WriteLine(processor.ToString());
            // }
            // foreach (var task in tasks) {
            //     Console.WriteLine(task.ToString());
            // }

            Console.WriteLine();
            Console.WriteLine();
            
            // Stopwatch s1 = new Stopwatch();
            // s1.Start(); 
            // (double, Solution) bestSolution;
            // bestSolution = SimulatedAnnealing.FindOptimalSolution(tasks, processors);
            // Console.WriteLine("Best Solution: ");
            // Console.WriteLine("\tTotal laxity: " + bestSolution.Item1);
            // Console.WriteLine();
            // s1.Stop();
            // Console.WriteLine("Time: " + s1.Elapsed.ToString());
            // s1.Reset();
            
            // Console.WriteLine("Best Solutions: ");
            // Console.WriteLine("\tTotal laxity: ");
            
            // Parallel.For(0,9, i => {
            //     bestSolution = SimulatedAnnealing.FindOptimalSolution(tasks, processors);
            //     Console.WriteLine("\t\t" + bestSolution.Item1);
            // });

            // // print solution
            // var solution = SolutionGenerator.GetInititalSolution(tasks,processors);
            // Console.WriteLine(solution.ToString());

            // var neighbour = SolutionGenerator.GenerateNeighbourMove(solution);
            // Console.WriteLine(neighbour.ToString());
            // Console.WriteLine(solution.ToString());

            // Console.WriteLine();
            // Console.WriteLine();

            // AssignedTask assignedTask = new AssignedTask(tasks[0], processors[0].Cores[0], 0);
            // Console.WriteLine($"Old Wcet: {assignedTask.Wcet}, Core.WcetFactor: {assignedTask.Core.WcetFactor}, Task.Wcet: {assignedTask.Task.Wcet}");
            // assignedTask.Core = processors[0].Cores[1];
            // Console.WriteLine($"New Wcet: {assignedTask.Wcet}, Core.WcetFactor: {assignedTask.Core.WcetFactor}, Task.Wcet: {assignedTask.Task.Wcet}");

            // Console.WriteLine();
            // Console.WriteLine();
            // Console.WriteLine();
            // Console.WriteLine("Solutions: ");


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

            /*
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
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(exampleSolution.ToString());
            Console.WriteLine("Total Laxity: " + SimulatedAnnealing.TotalLaxity(exampleSolution));
            */
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
            Console.WriteLine();
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
            // indent = indent.Substring(0, indent.Length-1);
            // Console.WriteLine(indent + "Middle 20: ");
            // indent += "\t";
            // tuples.Skip(tuples.Count() / 2).Take(20).ToList().ForEach(t => Console.WriteLine(indent + t.Item1));
            indent = indent.Substring(0, indent.Length-1);
            Console.WriteLine(indent + "Last 20: ");
            var last20 = tuples.Skip(tuples.Count() - 20).Take(20).ToList();
            indent += "\t";
            tuples.Skip(tuples.Count() - 20).Take(20).ToList().ForEach(t => Console.WriteLine(indent + t.Item1));
        }
    }
}
