using System;

namespace SimulatedAnnealing 
{
    class SimulatedAnnealing 
    {
        public int T { get; } = 10000;
        public int r { get; } = 0.003;
        public Solution C = InitialSolution();
        private Random rnd = new Random();
        
        public Solution FindOptimalSolution() {
            var results = new List<Solution>();
            while(T > 1) {
                Solution neighbourC = GenerateNeighbour(C);
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