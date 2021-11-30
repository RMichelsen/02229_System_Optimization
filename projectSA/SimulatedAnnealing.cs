using System;
using System.Collections.Generic;
using System.Linq;
using projectSA.Models;

namespace projectSA //TODO: MaxE2E
{
    class SimulatedAnnealing
    {
        public static int CYCLE_LENGTH = 12;
        public static Report GenerateOptimizedSolution()
		{
            int least_common_multiple = 1;          
			// start values
        	double T = 100000.0;
        	double r = 0.003;
            bool LCC, DC;
            int meanBW = 9999, meanE2E = 9999;
            Dictionary<Edge, int[]> Bs;
    		var C = SolutionGenerator.GetInititalSolution();
            //var (E,_) = ResponseTimeAnalysis(C);
            Report bestReport = C;

            foreach(var message in C.messages) {
                least_common_multiple = lcm(least_common_multiple, message.flow.Period);
            }

            int cycle_count = (int) Math.Ceiling((float)least_common_multiple / (float)CYCLE_LENGTH);

        	Random rnd = new Random();


            int count = 1;
            while (T > 0.1) {
                var neighbourC = SolutionGenerator.GenerateNeighbour(C);
                //var (nE, passRTA) = ResponseTimeAnalysis(neighbourC);


                // TODO: add probability of accepting a worse solution


                //double dE = nE - E;
                //double probability = AccProbability(dE, T);
                DC = DeadlineConstraint(neighbourC);
                LCC = LinkCapacityConstraint(neighbourC,cycle_count,out Bs);
                if (DC & LCC) {
                    Console.WriteLine("In");
                     //if (dE > 0 || probability > rnd.NextDouble()) {
                        //double totalLaxity = TotalLaxity(neighbourC);
                        //if (bestSolution.Item1 < totalLaxity) {
                           // bestSolution = (totalLaxity, neighbourC);
                        //}
                    meanBW = ObjectiveFunction(Bs);
                    if(meanBW < bestReport.solution.MeanBW){
                        C = neighbourC;
                        var solution = new Solution(0.2f, meanE2E, meanBW);
                        C.solution = solution;
                        bestReport = C;
                    }   
                    
                }
                    //E = nE;
                T = T * (1 - r);
                count++;
            }

            Console.WriteLine("Number of loops: " + count);

            return bestReport;
        }   
        

        public static double AccProbability(double dE, double T) {
            var frag = dE / T;
            return Math.Exp(frag);
        }

        public static bool DeadlineConstraint(Report report){

            foreach(var message in report.messages){
                var E2E = 0;
                foreach(var link in message.Links){
                    var InducedDelay = (int) Math.Ceiling((decimal)link.PropagationDelay/(decimal)CYCLE_LENGTH);
                    E2E += InducedDelay+link.Qnumber;
                }
                if(E2E > message.flow.Deadline){
                    return false;
                }
                message.MaxE2E = E2E;
            }
            return true;
        }

        public static bool LinkCapacityConstraint(Report report, int cycle_count, out Dictionary<Edge, int[]> B){
            //Initialize B array
            B = new Dictionary<Edge, int[]>();
            foreach(var message in report.messages){
                foreach(var link in message.Links){
                    if(!B.ContainsKey(link.Edge)){
                        B.Add(link.Edge,new int[cycle_count]);
                    }
                }
            }

            foreach(var message in report.messages){
                var alpha = 0;
                foreach(var link in message.Links){
                    
                    for(int i = 0; i < cycle_count; i++){
                        B[link.Edge][i] += ArrivalFunction(i,alpha,message.flow.Period,message.flow.Size);
                    }
                    
                    var InducedDelay = (int) Math.Ceiling((decimal)link.PropagationDelay/(decimal)CYCLE_LENGTH);
                    alpha += InducedDelay+link.Qnumber;
                    
                }
            }

            foreach(var edge in B.Keys){
                foreach(var b in B[edge]){
                    if(b > edge.Bandwidth){
                        return false;
                    }
                }
            }
            
            return true;

        }

        public static int ObjectiveFunction(Dictionary<Edge, int[]> B){
            int bSum = 0;
            int omega;

            foreach(var edge in B.Keys){
                bSum += ((B[edge].Max())/(edge.Bandwidth))*1000;
            }

            omega = bSum/B.Keys.Count();
            return omega;
        }

        public static int ArrivalFunction(int cycle, int alpha, int flowPeriod, int flowSize){
            int c = cycle-alpha;
            if(c < 0){
                return 0;
            }

            int A = c*CYCLE_LENGTH;

            if(A % flowPeriod != 0){
                return 0;
            }

            return flowSize;

        }

        static int gcf(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        static int lcm(int a, int b)
        {
            return (a / gcf(a, b)) * b;
        }

    }
}