using MulticoreProcessorScheduler.Models;
using System.Collections.Generic;
using System.Linq;

namespace MulticoreProcessorScheduler
{
	class SolutionGenerator
	{
		public static Solution GetInititalSolution(List<Task> tasks, List<Processor> processors) {
			var solution = new Solution();
			int i = 0;
			
			var cores = processors.SelectMany(processor => processor.Cores).ToList();
			int coreCount = cores.Count();

			foreach(var task in tasks){
				var assignedTask = new AssignedTask(task, cores[i], 0);
				i = (i+1)%coreCount;
				solution.AssignedTasks.Add(assignedTask);
			}
			return solution;
		}
	}
}