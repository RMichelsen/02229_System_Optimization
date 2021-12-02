using System.Collections.Generic;
using QuikGraph;

namespace projectSA.Models
{
    class Vertex
    {
        public string Name {get;}

        public Vertex(string name)
        {
            Name = name;
        }
    }

    class Edge
    {
        public string Id{get;}
        public string Source {get;}
        public string Destination {get;}
        public int Bandwidth {get;}
        public int PropagationDelay {get;}

        public Edge(string id, string source, string destination,
            int bandwidth, int propagationDelay)
        {
            Id = id;
            Source = source;
            Destination = destination;
            Bandwidth = bandwidth;
            PropagationDelay = propagationDelay;
        }
    }

    class Architecture
    {
        public List<Vertex> Vertices;
        public List<Edge> Edges;

        public Architecture(List<Vertex> vertices, List<Edge> edges)
        {
            Vertices = vertices;
            Edges = edges;
        }
    }

    
}