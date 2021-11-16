// namespace projectSA
// {
//     class SolutionGenerator
// 	{
// 		static SolutionGenerator() {}

//     public static SolutionSA GetInititalSolution(List<Task> tasks, List<Processor> processors) 
// 		{
// 			var solution = new Solution();
// 			int i = 0;
			
// 			var cores = processors.SelectMany(processor => processor.Cores).ToList();
// 			Cores = cores;
// 			int coreCount = cores.Count();

// 			foreach(var task in tasks){
// 				var assignedTask = new AssignedTask(task, cores[i], 0);
// 				i = (i+1)%coreCount;
// 				solution.AssignedTasks.Add(assignedTask);
// 			}

// 			solution.AssignedTasks = OrderByDeadlineThenByWcet(solution.AssignedTasks);
// 			return solution;
// 	}

//     }   
// }