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
            //var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Level2SimpleData"));
            //var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimpleData"));
            var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ToyData"));

            dh.VisualizeRelationship("datacenters.png");
            dh.VisualizeTask("task.png");

            var alloc = new Allocator();
            var colList = alloc.Allocate(dh);
            colList.Last().VisualizeTiming("timing.png");
        }
    }
}
