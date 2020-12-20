namespace NetworkAlgorithm
{
    using NetworkAlgorithm.Visualizer;
    using System;
    using System.IO;
    using System.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            var folders = new[] {
                "SimpleData",
                "Level2SimpleData",
                "ToyData",
            };

            foreach (var folder in folders)
            {
                var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folder));

                dh.VisualizeRelationship(Prefix("datacenters.png"));
                dh.VisualizeTask(Prefix("task.png"));
                _ = dh.Allocate(Prefix("timingBest.png"), Prefix("timing.png"));

                string Prefix(string filename) => $"{folder}-{filename}";
            }
        }
    }
}
