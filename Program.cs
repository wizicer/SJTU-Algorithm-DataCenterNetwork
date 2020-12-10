namespace NetworkAlgorithm
{
    using System;
    using System.IO;
    using System.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Level2SimpleData"));
            //var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimpleData"));
            //var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ToyData"));
            dh.Init();

            var vis = new Visualizer();
            vis.VisualizeRelationship(dh, "datacenters.png");
            vis.VisualizeTask(dh, "task.png");

            var alloc = new Allocator();
            var colList = alloc.Allocate(dh);
            vis.VisualizeTiming(colList.Last(), "timing.png");
        }
    }
}
