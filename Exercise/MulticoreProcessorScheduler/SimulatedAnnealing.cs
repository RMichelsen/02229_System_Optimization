using System;
using System.Collections.Generic;
using System.Linq;
using MulticoreProcessorScheduler;
using MulticoreProcessorScheduler.Models;

namespace SimulatedAnnealing 
{
    class SimulatedAnnealing 
    {
        
        public List<(decimal,Solution)> FindOptimalSolution(List<Task> tasks, List<Processor> processors) 
		{
			// start values
        	double T = 10000.0;
        	double r = 0.003;
        	
            var results = new List<(decimal,Solution)>();
            
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
                    if (true /*IsSolution(C)*/) {
                        break;
                    }
                }
                T = T * (1 - r);
            }

            return C;
        }

        protected double AccProbability(double costC, double costNeighbour, double T) {
            return Math.Exp((costC-costNeighbour)/T);
        }

        protected double Cost(Solution solution) {
            for(int i = 0; i < solution.AssignedTasks.Count; i++) {
                Task task = solution.AssignedTasks[i].Task;

                double I = 0.0f;
                double R = 0.0f;
                do {
                    R = I + task.Wcet;
                    if(R > task.Deadline) {
                        return 100000.0f;
                    }
                    for(int j = 0; j < i - 1; j++) {
                        I += Math.Ceiling(R / task.Period) * task.Wcet;
                    }

                } while(I + task.Wcet > R);

                solution.AssignedTasks[i].Wcrt = R;
            }

            double cost = 0.0;
            foreach(var task in solution.AssignedTasks) {
                cost += task.Wcrt;
            }
            return cost;
        }
    }
}
