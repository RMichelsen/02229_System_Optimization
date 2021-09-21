using System;
using System.Collections.Generic;
using MulticoreProcessorScheduler;
using MulticoreProcessorScheduler.Models;

namespace SimulatedAnnealing 
{
    class SimulatedAnnealing 
    {
        
        public List<(decimal,Solution)> FindOptimalSolution(List<Task> tasks, List<Processor> processors) 
		{
			// start values
        	decimal T = 10000m;
        	decimal r = 0.003m;
        	
            var results = new List<(decimal,Solution)>();
            
			Solution C = SolutionGenerator.GetInititalSolution(tasks, processors);
            decimal costC = Cost(C);
            results.Add((costC, C));

            decimal costNeighbour;
        	Random rnd = new Random();

            while(T > 1) {
                Solution neighbourC = SolutionGenerator.GenerateNeighbour(C);
                costC = Cost(C);
                costNeighbour = Cost(neighbourC);
                if (AccProbability(costC,costNeighbour, T) > rnd.Next(0, 1)) {
                    C = neighbourC;
                    if (IsSolution(C)) {
                        results.Add((costNeighbour, neighbourC));
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