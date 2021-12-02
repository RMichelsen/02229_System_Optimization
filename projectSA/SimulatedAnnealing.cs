using System;
using System.Collections.Generic;
using System.Linq;
using projectSA.Models;
using System.Diagnostics;

namespace projectSA //TODO: MaxE2E
{
    class SimulatedAnnealing
    {
        public static int CYCLE_LENGTH = 12;
        public static int magicInt  = 131072;
        public static double magicDouble = 0.000012;
        public static Report GenerateOptimizedSolution(int edgeCount)
		{
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int least_common_multiple = 1;          
			// start values
            int EdgeCount = edgeCount;
            int E2Esum = 0;
        	double T = 10000000.0;
        	double r = 0.002;
            bool LCC, DC;
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

                DC = DeadlineConstraint(neighbourC);
                LCC = LinkCapacityConstraint(neighbourC,cycle_count,out Bs);
                neighbourC.solution.MeanBW = ObjectiveFunction(Bs, EdgeCount);
                double dE = neighbourC.solution.MeanBW - C.solution.MeanBW;
                double probability = AccProbability(dE, T);

                if (DC & LCC) {
                    if(dE > 0 || probability > rnd.NextDouble()){
                        C = neighbourC;
                        if(C.solution.MeanBW < bestReport.solution.MeanBW){
                            bestReport = C;
                        }
                    }   
                }
                T = T * (1 - r);
                count++;
            }

            Console.WriteLine("Number of loops: " + count);

            foreach(var msg in bestReport.messages){
                E2Esum += msg.MaxE2E;
            }
            bestReport.solution.MeanE2E = E2Esum/bestReport.messages.Count();

            stopwatch.Stop();
            bestReport.solution.Runtime = stopwatch.ElapsedMilliseconds/1000.0f;

            return bestReport;
        }


    
        protected static double AccProbability(double dE, double T) {
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
                var bandwidthBytesPerCycle = (int)((edge.Bandwidth * magicInt)*magicDouble);
                foreach(var b in B[edge]){
                    if(b > bandwidthBytesPerCycle){
                        return false;
                    }
                }
            }
            return true;
        }

        public static int ObjectiveFunction(Dictionary<Edge, int[]> B,int edgeCount){
            int bSum = 0;
            int omega;

            foreach(var edge in B.Keys){
                var bandwidthBytesPerCycle = (int) Math.Ceiling((edge.Bandwidth * magicInt)*magicDouble);
                bSum += B[edge].Max()*1000/bandwidthBytesPerCycle;
            }

            omega = bSum/(edgeCount*2);
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

        public static Report solveExample(Report report, int edgeCount){
            bool LCC, DC;
            int least_common_multiple = 1;
            Dictionary<Edge, int[]> Bs;

            foreach(var message in report.messages) {
                least_common_multiple = lcm(least_common_multiple, message.flow.Period);
            }
            int cycle_count = (int) Math.Ceiling((float)least_common_multiple / (float)CYCLE_LENGTH);

            DC = DeadlineConstraint(report);
            LCC = LinkCapacityConstraint(report,cycle_count,out Bs);
            Console.WriteLine(DC.ToString());
            Console.WriteLine(LCC.ToString());

            var meanBW = ObjectiveFunction(Bs, edgeCount);

            var solution = new Solution(0.0f, 999, meanBW);
            report.solution = solution;

            return report;
        }

    }
}