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
            Architecture architecture;
            Application application;
            Report SA;

            XMLReader.Read(TestCase.TC0, out architecture, out application);
            var (flows,flowpaths) = ComputeFlowPaths.Compute(application, architecture);
            var solutionGenerator = new SolutionGenerator(flows,flowpaths);
            do{
            SA = SimulatedAnnealing.GenerateOptimizedSolution(solutionGenerator, architecture.Edges.Count());
            Console.WriteLine(SA.solution.MeanBW);            
            }while(SA.solution.MeanBW == 8888);
            SA.toXML("example");
                    
            // var example = SolutionGenerator.GenerateExampleReport();
            // example.toXML("example");
            // var solvedExample = SimulatedAnnealing.solveExample(example,5);
            // solvedExample.toXML("solvedExample");

            //var SA = SimulatedAnnealing.GenerateOptimizedSolution(5);    //TODO: don't hardcode edgecount
            //SA.toXML("copy");
        }
    } 
}
