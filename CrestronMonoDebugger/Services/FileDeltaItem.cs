namespace CrestronMonoDebugger.Services
{
    public class FileDeltaItem
    {
        public string Name { get; set; }

        public bool Delete { get; set; }

        public bool New { get; set; }

        public bool Changed { get; set; }
    }
}
