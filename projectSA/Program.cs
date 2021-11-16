using System;
using System.Collections.Generic;
using projectSA;
using projectSA.Models;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.Observers;
using QuikGraph.Algorithms.Search;
using QuikGraph.Algorithms.ShortestPath;

namespace projectSA
{
    class Program
    {
        static void Main(string[] args)
        {
            Architecture architecture;
            Application application;
            XMLReader.Read(TestCase.TC1, out architecture, out application);
            Console.WriteLine("Hello World!");
            //var uGraph = new UndirectedGraph<string,Edge<string>>(false);
            var uGraph = new BidirectionalGraph<string, Edge<string>>(true);
            uGraph.AddVertex("ES1");
            uGraph.AddVertex("ES2");
            uGraph.AddVertex("SW1");
            uGraph.AddVertex("SW2");

            uGraph.AddEdge(new Edge<string>("ES1","SW1"));
            uGraph.AddEdge(new Edge<string>("ES1","SW2"));
            uGraph.AddEdge(new Edge<string>("SW1","ES2"));
            uGraph.AddEdge(new Edge<string>("SW2","ES2"));


            //var dfs = new UndirectedDepthFirstSearchAlgorithm<string, Edge<string>>(uGraph);
            Func<Edge<string>, double> weightFunction = e => 0.0;
            var fw = new FloydWarshallAllShortestPathAlgorithm<string, Edge<string>>(uGraph, weightFunction);
            // Compute
            fw.Compute();

            // Get interesting paths
            IEnumerable<Edge<string>> path;
            foreach(string source in uGraph.Vertices)
            {
                foreach(string target in uGraph.Vertices)
                {
                    if (fw.TryGetPath(source, target, out path))
                    {
                        Console.WriteLine("Path:");
                        foreach(Edge<string> edge in path)
                        {
                            Console.WriteLine(edge);
                        }
                    }
                }
            }

        //     //var dfs = new DepthFirstSearchAlgorithm<string, Edge<string>>(uGraph.ToBidirectionalGraph());

        //     var observer = new UndirectedVertexPredecessorRecorderObserver<string, Edge<string>>();

        //     using (observer.Attach(dfs)) // attach, detach to dfs events
        //         dfs.Compute("ES1");

        //     string vertexToFind = "ES2";
        //     IEnumerable<Edge<string>> edges;
        //     if (observer.TryGetPath(vertexToFind, out edges))
        //     {
        //         Console.WriteLine("To get to vertex '" + vertexToFind + "', take the following edges:");
        //         foreach (Edge<string> edge in edges)
        //             Console.WriteLine(edge.Source + " -> " + edge.Target);
                
        //     }
        //     //Console.WriteLine(graph.EdgeCount.ToString());

        //     if (observer.TryGetPath(vertexToFind, out edges))
        //     {
        //         Console.WriteLine("To get to vertex '" + vertexToFind + "', take the following edges:");
        //         foreach (Edge<string> edge in edges)
        //             Console.WriteLine(edge.Source + " -> " + edge.Target);
                
        //     }
        // }
    //}
        }
    } 
}
