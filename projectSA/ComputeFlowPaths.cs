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
    class ComputeFlowPaths
    {
        public static Dictionary<string, List<List<Edge>>> Compute(Application application, Architecture architecture) {
            var uGraph = new BidirectionalGraph<string, TaggedEdge<string, Edge>>(false);
            foreach(var vertex in architecture.Vertices) 
            {
                uGraph.AddVertex(vertex.Name);
            }
            foreach(var edge in architecture.Edges) 
            {
                uGraph.AddEdge(new TaggedEdge<string, Edge>(edge.Source, edge.Destination, edge));
                uGraph.AddEdge(new TaggedEdge<string, Edge>(edge.Destination, edge.Source, edge));
            }
            var flow_paths = new Dictionary<string, List<List<Edge>>>();
            Func<Edge<string>, double> weightFunction = e => 0.0;
            var hp = new HoffmanPavleyRankedShortestPathAlgorithm<string, TaggedEdge<string, Edge>>(uGraph, weightFunction);
            var uniquePaths = 100;
            hp.ShortestPathCount = architecture.Edges.Count * uniquePaths;
            
            // Get interesting paths
            foreach(string source in uGraph.Vertices)
            {   
                if (!source.Contains("ES")) continue;
                foreach(string target in uGraph.Vertices)
                {
                    if (!target.Contains("ES")) continue;
                    hp.Compute(source, target);
                    foreach(Flow flow in application.Flows) 
                    {
                        if (flow.Source == source && flow.Destination == target)
                        {   
                            flow_paths.Add(flow.Name, new List<List<Edge>>());
                            foreach(IEnumerable<TaggedEdge<string, Edge>> edges in hp.ComputedShortestPaths)
                            {   
                                flow_paths[flow.Name].Add(new List<Edge>(edges.Select(edge => edge.Tag).ToList()));
                            }
                        }
                    }
                    
                }
            }
            return flow_paths;
        }

        public void printFlowPaths(Dictionary<string, List<List<Edge>>> flow_paths) {
            foreach(var flow in flow_paths) 
            {   
                Console.WriteLine($"Flow: {flow.Key}");
                foreach(List<Edge> paths in flow.Value)
                {
                    Console.WriteLine("Path:");
                    foreach(Edge edge in paths) 
                    {
                        Console.WriteLine($"{edge.Source}->{edge.Destination}");
                    }
                }
            }
        }
    } 
}
