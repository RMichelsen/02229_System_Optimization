﻿using System;
using System.Collections.Generic;
using System.Linq;
using MulticoreProcessorScheduler.Models;

namespace MulticoreProcessorScheduler
{
    class Program
    {
        static void Main(string[] args)
        {

            XmlReader.Read(ImportFileSize.Small, out var tasks, out var processors);

            foreach (var processor in processors) {
                Console.WriteLine(processor.ToString());
            }
            foreach (var task in tasks) {
                Console.WriteLine(task.ToString());
            }

            Console.WriteLine();
            Console.WriteLine();

            var bestSolution = SimulatedAnnealing.FindOptimalSolution(tasks, processors);
            Console.WriteLine("Best Solution: ");
            Console.WriteLine("\tTotal laxity: " + bestSolution.Item1);
            Console.WriteLine();
            Console.WriteLine(bestSolution.Item2.ToString());

            Console.WriteLine("Best Solutions: ");
            Console.WriteLine("\tTotal laxity: ");
            for (int i = 0; i < 10; i++)
            {
                bestSolution = SimulatedAnnealing.FindOptimalSolution(tasks, processors);
                Console.WriteLine("\t\t" + bestSolution.Item1);
            }
            
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


            // print list of solutions
            // var tuples = SimulatedAnnealing.FindOptimalSolution(tasks, processors);
            // foreach (var tuple in tuples)
            // {
            //     // Console.WriteLine(tuple.Item1);
            // }

            // printTuples(tuples);

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
            // indent = indent.Substring(0, indent.Length-1);
            // Console.WriteLine(indent + "Last 20: ");
            // var last20 = tuples.Skip(tuples.Count() - 20).Take(20).ToList();
            // indent += "\t";
            // tuples.Skip(tuples.Count() - 20).Take(20).ToList().ForEach(t => Console.WriteLine(indent + t.Item1));
        }
    }
}
