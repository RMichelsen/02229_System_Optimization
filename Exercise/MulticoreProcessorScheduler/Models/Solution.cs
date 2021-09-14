namespace MulticoreProcessorScheduler.Models
{
	class Solution
	{
		public List<AssignedTask> AssignedTasks { get; set; }

		public Solution() {
			AssignedTasks = new List<AssignedTask>();
		}
	}

	class AssignedTask
	{
		public Task Task { get; set; }
		public Core Core { get; set; }
		public decimal Wcrt { get; set; }
	}
}