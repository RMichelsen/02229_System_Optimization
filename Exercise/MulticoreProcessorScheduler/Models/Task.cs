namespace MulticoreProcessorScheduler.Models
{
    class Task 
    {
        public string Id { get; }
        public int Deadline { get; set; }
        public int Period { get; set; }
        public int Wcet { get; set; }

        public Task(string id, int deadline, int period, int wcet)
        {
            Id = id;
            Deadline = deadline;
            Period = period;
            Wcet = wcet;
        }

        public override string ToString()
        {
            return $"Task\tId:{Id}, Deadline: {Deadline}, Period: {Period}, WCET: {Wcet}";
        }
    }
}