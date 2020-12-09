namespace NetworkAlgorithm
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using static NetworkAlgorithm.Allocator;

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

        public void VisualizeTiming(JobExecutionInfoCollection col, string output)
        {
            var t = @"@startuml {{OUTPUT}}
scale 1000 as 100 pixels
{{DEFINITIONS}}

{{TIMINGS}}
@enduml";
            var definitionSb = new StringBuilder();
            var deflist = new List<(string display, string name, string group)>();
            foreach (var link in col.linkJobs)
            {
                deflist.Add(($"{link.Name}", $"{simplifyLink(link.Name)}", link.To));
            }
            foreach (var slot in col.data.Slots)
            {
                for (int i = 0; i < slot.Slot; i++)
                {
                    var ps = string.Join(",", col.data.Partitions.Where(_ => _.DataCenter == slot.DataCenter).Select(_ => _.Partition));
                    deflist.Add(($"{slot.DataCenter} Slot{i} (With {ps})", $"{slot.DataCenter}_{i}", slot.DataCenter));
                }
            }
            foreach (var group in deflist.GroupBy(_ => _.group))
            {
                foreach (var def in group)
                {
                    definitionSb.AppendLine($"concise \"{def.display}\" as {def.name}");
                }
            }

            var timimgSb = new StringBuilder();

            static string simplifyLink(string name) => name.Replace(" -> ", "_");
            static string getName(JobExecutionInfo _)
                => _ is LinkJobExecutionInfo lj ? simplifyLink(lj.Name)
                : _ is WorkJobExecutionInfo wj ? $"{wj.Location}_{wj.Slot}"
                : throw new Exception("Unexpected");

            foreach (var group in col.allJobs.GroupBy(_ => getName(_)))
            {
                timimgSb.AppendLine($"@{group.Key}");

                var lastTime = 0;
                var lastDuration = 0;
                foreach (var job in group.OrderBy(_ => _.StartInMs))
                {
                    var offset = job is LinkJobExecutionInfo ? job.StartInMs : job.StartInMs - lastTime;
                    if (lastTime == 0 && offset > 0) timimgSb.AppendLine($"0 is {{-}}");

                    timimgSb.AppendLine($"{(offset == 0 ? "0" : $"+{ offset}") } is {(job is LinkJobExecutionInfo lj ? $"transfer_{lj.Partition}" : job.Name)}");

                    lastTime = job.StartInMs + job.DurationInMs;
                    lastDuration = job.DurationInMs;
                }

                if (lastDuration > 0) timimgSb.AppendLine($"+{lastDuration} is {{-}}");

                timimgSb.AppendLine();
            }

            var outputText = t
                .Replace("{{OUTPUT}}", output)
                .Replace("{{DEFINITIONS}}", definitionSb.ToString())
                .Replace("{{TIMINGS}}", timimgSb.ToString());

            File.WriteAllText("temp.uml", outputText);
            //Process.Start("java", $" -jar plantuml.jar temp.uml");
            Process.Start(@"C:\DevTools\jrex86\bin\java.exe", $@" -jar C:\Tools\jar\plantuml.jar temp.uml");
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