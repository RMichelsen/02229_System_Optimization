using System;
using System.Collections.Generic;
using System.Linq;
using MulticoreProcessorScheduler;
using MulticoreProcessorScheduler.Models;

namespace MulticoreProcessorScheduler 
{
    class SimulatedAnnealing 
    {
        public static List<(double,Solution)> FindOptimalSolution(List<Task> tasks, List<Processor> processors) 
		{
			// start values
        	double T = 10000.0;
        	double r = 0.003;
        	
            var results = new List<(double,Solution)>();
            
			Solution C = SolutionGenerator.GetInititalSolution(tasks, processors);
            double costC = Cost(C);
            results.Add((costC, C));

            double costNeighbour;
        	Random rnd = new Random();

            while(T > 1) {
                Solution neighbourC = SolutionGenerator.GenerateNeighbour(C);
                costC = Cost(C);
                costNeighbour = Cost(neighbourC);
                if (AccProbability(costC,costNeighbour, T) > rnd.Next(0, 1)) {
                    C = neighbourC;
                    if (costNeighbour < 1000000000f) {
                        results.Add((costNeighbour, C));
                    }
                }
                T = T * (1 - r);
            }

            return results;
        }

        protected static double AccProbability(double costC, double costNeighbour, double T) {
            return Math.Exp((costNeighbour - costC) / T);
        }

        protected static double Cost(Solution solution) {
            AssignedTask assignedTask;
            AssignedTask jthTask;
            for(int i = 0; i < solution.AssignedTasks.Count; i++) {
                assignedTask = solution.AssignedTasks[i];

                double I = 0.0f;
                double R = 0.0f;
                do {
                    R = I + assignedTask.Wcet;
                    if(R > assignedTask.Task.Deadline) {
                        return 1000000000f;
                    }
                    I = 0.0f;
                    for(int j = 0; j < i; j++) {
                        jthTask = solution.AssignedTasks[j];
                        if(assignedTask.Core.Id == jthTask.Core.Id) {
                            I += Math.Ceiling(R / jthTask.Task.Period) * jthTask.Wcet;
                        }
                    }

                } while(I + assignedTask.Wcet > R);

                solution.AssignedTasks[i].Wcrt = R;
            }

            return (double) solution.AssignedTasks.Sum(at => at.Wcrt);
        }
    }
}
