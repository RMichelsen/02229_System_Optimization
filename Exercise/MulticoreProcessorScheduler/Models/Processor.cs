
using System.Collections.Generic;
using System.Text;

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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Processor Id:{Id}");
            Cores.ForEach(core => sb.AppendLine(core.ToString()));

            return sb.ToString();
        }
    }

    class Core
    {
        public string Id { get; }
        public string McpId { get; }
        public double WcetFactor { get; set; }
        public Core(string id, string mcpId, double wcetFactor)
        {
            Id = id;
            McpId = mcpId;
            WcetFactor = wcetFactor;
        }

        public override string ToString()
        {
            return $"Core\tId:{Id}, McpId:{McpId}, WcetFactor: {WcetFactor}";
        }
    }
}