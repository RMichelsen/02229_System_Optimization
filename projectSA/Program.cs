using System;
using System.Collections.Generic;
using projectSA;
using projectSA.Models;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.Observers;
using QuikGraph.Algorithms.Search;
using QuikGraph.Algorithms.ShortestPath;
using QuikGraph.Algorithms.RankedShortestPath;
using System.Linq;

namespace projectSA
{
    class Program
    {
        static void Main(string[] args)
        {   
            //runTestCase(TestCase.TC0);
            runAllTestCases();
           
        }

        public static void runAllTestCases(){
            Architecture architecture;
            Application application;
            HashSet<Report> schedulables;
            bool solutionFound = false;
            Report SA;

            foreach(TestCase t in Enum.GetValues(typeof(TestCase))){
                XMLReader.Read(t, out architecture, out application);
                var (flows,flowpaths) = ComputeFlowPaths.Compute(application, architecture);
                var solutionGenerator = new SolutionGenerator(flows,flowpaths);
                schedulables = SimulatedAnnealing.GenerateSchedulables(solutionGenerator, architecture.Edges.Count());
                (solutionFound, SA) = SimulatedAnnealing.findBestReport(schedulables);
                if(solutionFound){
                    Console.WriteLine(t.ToString()+": "+schedulables.Count()+" Solution(s) found; Runtime: "+SA.solution.Runtime.ToString()+"s; Best MeanBW: "+SA.solution.MeanBW.ToString());
                    SA.toXML(t.ToString()+"V3");
                } else{
                    Console.WriteLine(t.ToString()+": Solution not found");
                }
            }
        }

        public static void runTestCase(TestCase t){
            Architecture architecture;
            Application application;
            HashSet<Report> schedulables;
            bool solutionFound = false;
            Report SA;

            XMLReader.Read(t, out architecture, out application);
            var (flows,flowpaths) = ComputeFlowPaths.Compute(application, architecture);
            var solutionGenerator = new SolutionGenerator(flows,flowpaths);
            schedulables = SimulatedAnnealing.GenerateSchedulables(solutionGenerator, architecture.Edges.Count());
            (solutionFound, SA) = SimulatedAnnealing.findBestReport(schedulables);
            if(solutionFound){
                Console.WriteLine(t.ToString()+": "+schedulables.Count()+" Solution(s) found; Runtime: "+SA.solution.Runtime.ToString()+"s; Best MeanBW: "+SA.solution.MeanBW.ToString());
                SA.toXML(t.ToString()+"V2");
            } else{
                Console.WriteLine(t.ToString()+": Solution not found");
            }
        }

    } 
}
