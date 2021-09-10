
using System.Collections.Generic;

namespace MulticoreProcessorScheduler.Models
{
    class Processor
    {
        public string Id { get; }
        public List<Core> Cores { get; set; }

        public Processor(string id)
        {
            Id = id;
            Cores = new List<Core>();
        }
    }

    class Core
    {
        public string Id { get; }
        public decimal WcetFactor { get; set; }
        public Core(string id, decimal wcetFactor)
        {
            Id = id;
            WcetFactor = wcetFactor;
        }
    }
}