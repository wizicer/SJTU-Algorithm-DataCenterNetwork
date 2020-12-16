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

                dh.VisualizeRelationship(Prefix("datacenters.pdf"));
                dh.VisualizeTask(Prefix("task.pdf"));
                _ = dh.Allocate(Prefix("timingBest.eps"), Prefix("timing.eps"));

                string Prefix(string filename) => $"{folder}-{filename}";
            }
        }
    }
}
