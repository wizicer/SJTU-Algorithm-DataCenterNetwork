namespace NetworkAlgorithm
{
    using System;
    using System.IO;
    using System.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimpleData"));
            //var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ToyData"));
            dh.Init();
            var alloc = new Allocator();
            var colList = alloc.Allocate(dh);

            var vis = new Visualizer();
            vis.VisualizeTiming(colList.Last(), "timing.png");
            //vis.VisualizeRelationship(dh, "datacenters.png");
            //vis.VisualizeTask(dh, "task.png");
        }
    }
}
