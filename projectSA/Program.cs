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
            
            XMLReader.Read(TestCase.TC1, out architecture, out application);
            SolutionGenerator.flow_paths = ComputeFlowPaths.Compute(application, architecture);

            var report = SolutionGenerator.GetInititalSolution();
            report.toXML("original");
            
            var SA = SimulatedAnnealing.GenerateOptimizedSolution();    
            SA.toXML("copy");
        }
    } 
}
