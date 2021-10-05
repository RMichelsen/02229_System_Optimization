using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MulticoreProcessorScheduler.Models;

namespace MulticoreProcessorScheduler 
{
    class SimulatedAnnealing 
    {
        public static (double,Solution) FindOptimalSolution(List<Models.Task> tasks, List<Processor> processors) 
		{
			// start values
        	double T = GetTValue(tasks.Count());
        	double r = 0.003;
        	
            (double, Solution) bestSolution;
            
			Solution C = SolutionGenerator.GetInititalSolution(tasks, processors);
            var (E,_) = ResponseTimeAnalysis(C);
            bestSolution = (TotalLaxity(C), C);

        	// Random rnd = new Random(1);
        	Random rnd = new Random();
            int count = 1;
            while (T > 1) {
                // for (int i = 0; i < 100; i++)
                // {    
                    Solution neighbourC = SolutionGenerator.GenerateNeighbour(C);
                    var (nE, passRTA) = ResponseTimeAnalysis(neighbourC);

                    double dE = nE - E;
                    double probability = AccProbability(dE, T);

                    if (dE > 0 || probability > rnd.NextDouble()) {
                        double totalLaxity = TotalLaxity(neighbourC);
                        if (passRTA) {
                            if (bestSolution.Item1 < totalLaxity) {
                                bestSolution = (totalLaxity, neighbourC);
                            }
                            C = neighbourC;
                        }
                    }
                    E = nE;
                // }
                T = T * (1 - r);
                count++;
            }

            Console.WriteLine("Number of loops: " + count);
            return bestSolution;
        }   

        protected static double AccProbability(double dE, double T) {
            var frag = dE / T;
            return Math.Exp(frag);
        }

        public static double TotalLaxity(Solution solution) {
            return solution.AssignedTasks.Sum(at => at.Task.Deadline - at.Wcrt);
        }

        public static (double, bool) ResponseTimeAnalysis(Solution solution) {
            bool pass = true;
            
            object lockObject = new object();
            // AssignedTask assignedTask;
            // AssignedTask jthTask;

            Parallel.For(0, solution.AssignedTasks.Count, i => {
                AssignedTask assignedTask;
                lock (lockObject)
                {   
                    assignedTask = solution.AssignedTasks[i];
                }

                double I = 0.0f;
                double R = 0.0f;
                do {
                    R = I + assignedTask.Wcet;
                    if(R > assignedTask.Task.Deadline) {
                        lock (lockObject)
                        {   
                            pass = false; 
                        }
                    }
                    I = 0.0f;
                    AssignedTask jthTask;
                    for(int j = 0; j < i; j++) {
                        jthTask = solution.AssignedTasks[j];
                        if(assignedTask.Core.McpId == jthTask.Core.McpId && assignedTask.Core.Id == jthTask.Core.Id) {
                            I += Math.Ceiling(R / jthTask.Task.Period) * jthTask.Wcet;
                        }
                    }

                } while(I + assignedTask.Wcet > R);

                lock(lockObject) {
                    assignedTask.Wcrt = R;
                }
            });
            // for(int i = 0; i < solution.AssignedTasks.Count; i++) {
            //     assignedTask = solution.AssignedTasks[i];

            //     double I = 0.0f;
            //     double R = 0.0f;
            //     do {
            //         R = I + assignedTask.Wcet;
            //         if(R > assignedTask.Task.Deadline) {
            //             pass = false;
            //         }
            //         I = 0.0f;
            //         for(int j = 0; j < i; j++) {
            //             jthTask = solution.AssignedTasks[j];
            //             if(assignedTask.Core.McpId == jthTask.Core.McpId && assignedTask.Core.Id == jthTask.Core.Id) {
            //                 I += Math.Ceiling(R / jthTask.Task.Period) * jthTask.Wcet;
            //             }
            //         }

            //     } while(I + assignedTask.Wcet > R);

            //     assignedTask.Wcrt = R;
            // }

            double averageLaxity = solution.AssignedTasks.Sum(at => at.Task.Deadline - at.Wcrt) / solution.AssignedTasks.Count;
            return (averageLaxity, pass);
        }

        public static double GetTValue(int taskCount) {
            if (taskCount < 50) return 1000.0;
            if (taskCount < 200) return 10000.0;
            return 100000.0;
        }

        public static List<(double,Solution)> FindOptimalSolution_test(List<Models.Task> tasks, List<Processor> processors) 
        {
            // start values
            double T = 1000.0;
            double r = 0.003;
            
            var results = new List<(double,Solution)>();
            
            Solution C = SolutionGenerator.GetInititalSolution(tasks, processors);
            ResponseTimeAnalysis(C);
            results.Add((TotalLaxity(C), C));

            // Random rnd = new Random(1);
            Random rnd = new Random();

            while (T > 1) {
                // for (int i = 0; i < 100; i++)
                // {    
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
                // }
                T = T * (1 - r);
            }

            return results;
        }
    }
}
