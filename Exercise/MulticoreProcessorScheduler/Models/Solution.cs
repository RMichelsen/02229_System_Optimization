
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MulticoreProcessorScheduler.Models
{
	class Solution
	{
		public List<AssignedTask> AssignedTasks { get; set; }
		
		public Solution() {
			AssignedTasks = new List<AssignedTask>();
		}

		public override string ToString(){
			var sb = new StringBuilder();
            
            AssignedTasks.ForEach(t => sb.AppendLine(t.ToString()));

            return sb.ToString();
		}

		public Solution Copy()
		{	
			Solution copy = new Solution();
			var copiedTasks = this.AssignedTasks.Select(at => new AssignedTask(at.Task ,at.Core, at.Wcrt));
			copy.AssignedTasks.AddRange(copiedTasks);
			return copy;
		}
    }

	class AssignedTask
	{
		public Task Task { get; }
		public Core Core { get; set; }
		public decimal Wcrt { get; set; }
		
		public AssignedTask(Task task, Core core, decimal wcrt){
			Task = task;
			Core = core;
			Wcrt = wcrt;
		}

		public override string ToString(){
			return $"\tTask id ={Task.Id}, MCP ={Core.McpId}, Core ={Core.Id}, WCRT ={Wcrt}";
		}

	}
}