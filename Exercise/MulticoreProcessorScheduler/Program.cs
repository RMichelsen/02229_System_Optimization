using System;

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
        }
    }
}
