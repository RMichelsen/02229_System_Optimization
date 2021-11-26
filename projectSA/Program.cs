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

namespace projectSA
{
    class Program
    {
        static void Main(string[] args)
        {   
            Architecture architecture;
            Application application;
            XMLReader.Read(TestCase.TC4, out architecture, out application);
            var uGraph = new BidirectionalGraph<string, Edge<string>>(true);
            foreach(var vertex in architecture.Vertices) {
                uGraph.AddVertex(vertex.Name);
            }
            foreach(var edge in architecture.Edges) {
                uGraph.AddEdge(new Edge<string>(edge.Source, edge.Destination));
            }
            //var dfs = new UndirectedDepthFirstSearchAlgorithm<string, Edge<string>>(uGraph);
            Func<Edge<string>, double> weightFunction = e => 1.0;
            var hp = new HoffmanPavleyRankedShortestPathAlgorithm<string, Edge<string>>(uGraph, weightFunction);
            
            // Get interesting paths
             foreach(string source in uGraph.Vertices)
            {   
                if (!source.Contains("ES")) continue;
                foreach(string target in uGraph.Vertices)
                {
                    if (!target.Contains("ES")) continue;
                    hp.Compute(source, target);
                    foreach(IEnumerable<Edge<string>> edges in hp.ComputedShortestPaths)
                    {   
                        Console.WriteLine("Path:");
                        foreach(Edge<string> edge in edges) {
                            Console.WriteLine(edge);
                        }
                    }
                }
            }
        }
    } 
}
