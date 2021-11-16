using System.Collections.Generic;

namespace projectSA.Models
{
    class Vertex
    {
        public string Name;

        public Vertex(string name)
        {
            Name = name;
        }
    }

    class Edge
    {
        public string Id;
        public string Source;
        public string Destination;
        public int Bandwidth;
        public int PropagationDelay;

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