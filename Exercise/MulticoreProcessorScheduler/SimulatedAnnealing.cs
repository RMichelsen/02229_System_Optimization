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
        	double T = 1000000000000000.0;
        	double r = 0.0001;
        	
            var results = new List<(double,Solution)>();
            
			Solution C = SolutionGenerator.GetInititalSolution(tasks, processors);
            ResponseTimeAnalysis(C);
            results.Add((TotalLaxity(C), C));

        	Random rnd = new Random();

            while (T > 1) {
                Solution neighbourC = SolutionGenerator.GenerateNeighbour(C);
                var (E, _) = ResponseTimeAnalysis(C);
                var (nE, passRTA) = ResponseTimeAnalysis(neighbourC);

                double dE = nE - E;
                double probability = AccProbability(dE, T);

                if (dE > 0 || probability > rnd.NextDouble()) {
                    if (passRTA) {
                        results.Add((TotalLaxity(neighbourC), neighbourC));
                    }
                    C = neighbourC;
                }
                T = T * (1 - r);
            }

            return results;
        }

        protected static double AccProbability(double dE, double T) {
            return Math.Exp(dE / T);
        }

        protected static double TotalLaxity(Solution solution) {
            return solution.AssignedTasks.Sum(at => at.Task.Deadline - at.Wcrt);
        }

        protected static (double, bool) ResponseTimeAnalysis(Solution solution) {
            bool pass = true;

            AssignedTask assignedTask;
            AssignedTask jthTask;

            for(int i = 0; i < solution.AssignedTasks.Count; i++) {
                assignedTask = solution.AssignedTasks[i];

                double I = 0.0f;
                double R = 0.0f;
                do {
                    R = I + assignedTask.Wcet;
                    if(R > assignedTask.Task.Deadline) {
                        pass = false;
                    }
                    I = 0.0f;
                    for(int j = 0; j < i; j++) {
                        jthTask = solution.AssignedTasks[j];
                        if(assignedTask.Core.Id == jthTask.Core.Id) {
                            I += Math.Ceiling(R / jthTask.Task.Period) * jthTask.Wcet;
                        }
                    }

                } while(I + assignedTask.Wcet > R);

                assignedTask.Wcrt = R;
            }

            double averageLaxity = solution.AssignedTasks.Sum(at => at.Task.Deadline - at.Wcrt) / solution.AssignedTasks.Count;
            return (averageLaxity, pass);
        }
    }
}
