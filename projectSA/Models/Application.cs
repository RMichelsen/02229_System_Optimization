using System.Collections.Generic;

namespace projectSA.Models
{
    class Flow
    {
        public string Name{get;}
        public string Source{get;}
        public string Destination{get;}
        public int Size{get;}
        public int Period{get;}
        public int Deadline{get;}

        public Flow(string name, string source, string destination,
            int size, int period, int deadline)
        {
            Name = name;
            Source = source;
            Destination = destination;
            Size = size;
            Period = period;
            Deadline = deadline;
        }
    }

    class Application
    {
        public List<Flow> Flows;

        public Application(List<Flow> flows)
        {
            Flows = flows;
        }
    }
}