using System.Collections.Generic;

namespace Project.Models
{
    class Flow
    {
        public string Name;
        public string Source;
        public string Destination;
        public int Size;
        public int Period;
        public int Deadline;

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