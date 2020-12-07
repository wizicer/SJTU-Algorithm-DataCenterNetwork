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
        public void VisualizeRelationship(DataHolder data, string output)
        {
            var t = File.ReadAllText("relationship.dot");
            var clusterSb = new StringBuilder();
            foreach (var dc in data.Slots)
            {
                var nodes = new List<GraphNode>();
                nodes.AddRange(Enumerable.Range(0, dc.Slot)
                    .Select(_ => new GraphNode { Label = "", Name = $"{dc.DataCenter}_{_}", Shape = GraphShape.circle }));
                nodes.AddRange(data.Partitions
                    .Where(_ => _.DataCenter == dc.DataCenter)
                    .Select(_ => new GraphNode { Label = _.Partition, Name = _.Partition, Shape = GraphShape.cylinder }));
                clusterSb.AppendLine(Cluster(dc.DataCenter, nodes.ToArray()));
            }

            var linkSb = new StringBuilder();
            foreach (var link in data.Links)
            {
                if (link.From == link.To) continue;
                linkSb.AppendLine($"cluster_{link.From} -> cluster_{link.To} [label =\"{link.Bandwidth}\", len = 3];");
            }

            var outputText = t
                .Replace("{{CLUSTERS}}", Indent(clusterSb.ToString()))
                .Replace("{{CONNECTION}}", Indent(linkSb.ToString()));

            File.WriteAllText("temp.dot", outputText);
            Process.Start("dot", $" -Tpng temp.dot -o {output}");
        }

        public void VisualizeTask(DataHolder data, string output)
        {
            var t = File.ReadAllText("taskdag.dot");
            var clusterSb = new StringBuilder();
            foreach (var job in data.Jobs)
            {
                clusterSb.AppendLine($"{job.Name} [shape = {GraphShape.square}];");
            }

            var jobSb = new StringBuilder();
            foreach (var job in data.Jobs)
            {
                foreach (var dep in job.Dependences)
                {
                    jobSb.AppendLine($"{dep.Depend} -> {job.Name} [label =\"{dep.Size}\", len = 3];");
                }
            }

            var outputText = t
                .Replace("{{CLUSTERS}}", Indent(clusterSb.ToString()))
                .Replace("{{CONNECTION}}", Indent(jobSb.ToString()));

            File.WriteAllText("temp.dot", outputText);
            Process.Start("dot", $" -Tpng temp.dot -o {output}");
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
            square,
            circle,
            note,
            cylinder,
        }
    }
}