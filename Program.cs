namespace NetworkAlgorithm
{
    using System;
    using System.IO;

    class Program
    {
        static void Main(string[] args)
        {
            var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimpleData"));
            //var dh = new DataHolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ToyData"));
            dh.Init();
            var vis = new Visualizer();
            vis.VisualizeRelationship(dh, "datacenters.png");
            vis.VisualizeTask(dh, "task.png");
        }
    }
}
