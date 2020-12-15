namespace NetworkAlgorithm
{
    using System;
    using System.IO;
    using System.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            //var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Level2SimpleData"));
            //var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimpleData"));
            var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ToyData"));

            Visualizer.VisualizeRelationship(dh, "datacenters.png");
            Visualizer.VisualizeTask(dh, "task.png");

            var alloc = new Allocator();
            var colList = alloc.Allocate(dh);
            Visualizer.VisualizeTiming(colList.Last(), "timing.png");
        }
    }
}
