using MulticoreProcessorScheduler.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MulticoreProcessorScheduler
{
	class SolutionGenerator
	{
		static Random rnd;
		static SolutionGenerator() { rnd = new Random(); }
		static List<Core> Cores { get; set; }

		public static Solution GetInititalSolution(List<Task> tasks, List<Processor> processors) 
		{
			var solution = new Solution();
			int i = 0;
			
			var cores = processors.SelectMany(processor => processor.Cores).ToList();
			Cores = cores;
			int coreCount = cores.Count();

			foreach(var task in tasks){
				var assignedTask = new AssignedTask(task, cores[i], 0);
				i = (i+1)%coreCount;
				solution.AssignedTasks.Add(assignedTask);
			}

			solution.AssignedTasks = OrderByDeadlineThenByWcet(solution.AssignedTasks);
			return solution;
		}

		public static Solution GenerateNeighbour(Solution solution) {
			if(rnd.NextDouble() > 0.5) {
				return GenerateNeighbourSwap(solution);
            }
			else {
				return GenerateNeighbourMove(solution);
            }
        }

		public static Solution GenerateNeighbourSwap(Solution solution)
		{
			var neighbour = new Solution();
			neighbour = solution.Copy();

			int taskindex = rnd.Next(neighbour.AssignedTasks.Count());
			var task1 = neighbour.AssignedTasks[taskindex];
			AssignedTask task2;
			do
			{
				taskindex = rnd.Next(neighbour.AssignedTasks.Count());
				task2 = neighbour.AssignedTasks[taskindex];
			} while (task1.Core.McpId == task2.Core.McpId && task1.Core.Id == task2.Core.Id);

			var t1Core = task1.Core;
			task1.Core = task2.Core;
			task2.Core = t1Core;
			
			return neighbour;
		}
		
		public static Solution GenerateNeighbourMove(Solution solution)
		{
			var neighbour = new Solution();
			neighbour = solution.Copy();

			int coreId = rnd.Next(Cores.Count());
			int taskId = rnd.Next(neighbour.AssignedTasks.Count());

			neighbour.AssignedTasks[taskId].Core = Cores[coreId];
			return neighbour;
		}


		public static List<AssignedTask> OrderByDeadlineThenByWcet(List<AssignedTask> assignedTasks) => 
			assignedTasks
			.OrderBy(at => at.Task.Deadline)
			.ThenByDescending(at => at.Wcet)
			.ToList();

	}
}