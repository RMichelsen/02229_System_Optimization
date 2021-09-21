using System;
using System.Collections.Generic;
using MulticoreProcessorScheduler;
using MulticoreProcessorScheduler.Models;

namespace SimulatedAnnealing 
{
    class SimulatedAnnealing 
    {
        
        public Solution FindOptimalSolution(List<Task> tasks, List<Processor> processors) 
		{
			// start values
        	decimal T = 10000m;
        	decimal r = 0.003m;
        	
			Solution C = SolutionGenerator.GetInititalSolution(tasks, processors);
        	Random rnd = new Random();

            var results = new List<Solution>();
            while(T > 1) {
                Solution neighbourC = SolutionGenerator.GenerateNeighbour(C);
                if (AccProbability(Cost(C), Cost(neighbourC), T) > rnd.Next(0, 1)) {
                    C = neighbourC;
                    if (IsSolution(C)) {
                        results.Add(C);
                    }
                }
                T = T * (1 - r);
            }
            return results;
        }

        protected int AccProbability(var costC, var costNeighbour, int T) {
            return Math.Exp((costC-costNeighbour)/T);
        }
    }
}