using System;
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

            // print solution
            var solution = SolutionGenerator.GetInititalSolution(tasks,processors);
            Console.WriteLine(solution.ToString());

            var neighbour = SolutionGenerator.GenerateNeighbour(solution);
            Console.WriteLine(neighbour.ToString());
            Console.WriteLine(solution.ToString());

            Console.WriteLine();
            Console.WriteLine();

            AssignedTask assignedTask = new AssignedTask(tasks[0], processors[0].Cores[0], 0);
            Console.WriteLine($"Old Wcet: {assignedTask.Wcet}, Core.WcetFactor: {assignedTask.Core.WcetFactor}, Task.Wcet: {assignedTask.Task.Wcet}");
            assignedTask.Core = processors[0].Cores[1];
            Console.WriteLine($"New Wcet: {assignedTask.Wcet}, Core.WcetFactor: {assignedTask.Core.WcetFactor}, Task.Wcet: {assignedTask.Task.Wcet}");

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Solutions: ");

            var tuples = SimulatedAnnealing.FindOptimalSolution(tasks, processors);
            foreach (var tuple in tuples)
            {
                Console.WriteLine(tuple.Item1);
            }

            

        }
    }
}
