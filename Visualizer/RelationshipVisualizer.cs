namespace NetworkAlgorithm.Visualizer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class RelationshipVisualizer
    {
        public static void Visualize(DataHolder data, string output)
        {
            var t = File.ReadAllText("relationship.dot");
            var clusterSb = new StringBuilder();
            foreach (var dc in data.Slots)
            {
                var nodes = new List<GraphNode>();
                nodes.AddRange(Enumerable.Range(0, dc.Slot)
                    .Select(_ => new GraphNode($"{dc.DataCenter}_{_}", "", GraphShape.circle)));
                nodes.AddRange(data.Partitions
                    .Where(_ => _.DataCenter == dc.DataCenter)
                    .Select(_ => new GraphNode(_.Partition, _.Partition, GraphShape.cylinder)));
                clusterSb.AppendLine(Cluster(dc.DataCenter, nodes.ToArray()));
            }

            var linkSb = new StringBuilder();
            foreach (var link in data.Links)
            {
                if (link.From == link.To) continue;
                linkSb.AppendLine($"cluster_{link.From} -> cluster_{link.To} [label =\"{link.Bandwidth}\", len = 3];");
            }

            var outputText = t
                .Replace("{{CLUSTERS}}", clusterSb.Indent())
                .Replace("{{CONNECTION}}", linkSb.Indent());

            File.WriteAllText("temprelation.dot", outputText);
            Process.Start("dot", $" -Tpng temprelation.dot -o {output}");
        }

        private static string Cluster(string name, GraphNode[] nodes)
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
                .Replace("{{NODES}}", string.Join(Environment.NewLine, nodes.Select(_ => _.ToString()), 2).Indent());
        }

        public record GraphNode(string Name, string Label, GraphShape Shape)
        {
            public override string ToString()
            {
                return $"{Name} [label = \"{Label}\", shape = {Shape}];";
            }
        }
    }
}