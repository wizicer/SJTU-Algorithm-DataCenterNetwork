namespace NetworkAlgorithm
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class Visualizer
    {
        public void Visualize(DataHolder data)
        {
            var t = File.ReadAllText("template.dot");
            var clustersb = new StringBuilder();
            foreach (var dc in data.Slots)
            {
                var nodes = new List<GraphNode>();
                nodes.AddRange(Enumerable.Range(0, dc.Slot)
                    .Select(_ => new GraphNode { Label = "", Name = $"{dc.DataCenter}_{_}", Shape = GraphShape.circle }));
                nodes.AddRange(data.Partitions
                    .Where(_ => _.DataCenter == dc.DataCenter)
                    .Select(_ => new GraphNode { Label = _.Partition, Name = _.Partition, Shape = GraphShape.cylinder }));
                clustersb.AppendLine(Indent(Cluster(dc.DataCenter, nodes.ToArray())));
            }

            var output = t
                .Replace("{{CLUSTERS}}", clustersb.ToString())
                .Replace("{{CONNECTION}}", "");

            File.WriteAllText("output.dot", output);
            Process.Start("dot", " -Tpng output.dot -o output.png");
        }

        private string Cluster(string name, GraphNode[] nodes)
        {
            var template = @"
subgraph cluster_{{NAME}} {
  label = ""{{NAME}}"";
  labelloc = ""t"";
  labeljust = ""l"";
  fillcolor = ""#DAE8FC"";

{{NODES}}
};
";
            return template
                .Replace("{{NAME}}", name)
                .Replace("{{NODES}}", Indent(string.Join(Environment.NewLine, nodes.Select(_ => _.ToString())), 2));
        }

        private string Indent(string input, int indent = 2)
        {
            var pad = new string(' ', indent);
            return pad + input.Replace(Environment.NewLine, Environment.NewLine + pad);
        }

        public class GraphNode
        {
            public string Name { get; init; }
            public string Label { get; init; }
            public GraphShape Shape { get; init; }

            public override string ToString()
            {
                return $"{Name} [label = \"{Label}\", shape = {Shape}];";
            }
        }

        public enum GraphShape
        {
            None,
            Mdiamond,
            box,
            circle,
            note,
            cylinder,
        }
    }
}