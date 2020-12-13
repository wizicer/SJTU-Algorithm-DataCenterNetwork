namespace NetworkAlgorithm
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    public static class Visualizer
    {
        public static void VisualizeRelationship(DataHolder data, string output)
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

            File.WriteAllText("temprelation.dot", outputText);
            Process.Start("dot", $" -Tpng temprelation.dot -o {output}");
        }

        public static void VisualizeTask(DataHolder data, string output)
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

            File.WriteAllText("temptask.dot", outputText);
            Process.Start("dot", $" -Tpng temptask.dot -o {output}");
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
                .Replace("{{NODES}}", Indent(string.Join(Environment.NewLine, nodes.Select(_ => _.ToString())), 2));
        }

        public static void VisualizeTiming(JobExecutionInfoCollection col, string output)
        {
            var t = @"@startuml {{OUTPUT}}
scale {{SCALE}} as 100 pixels
{{DEFINITIONS}}

{{TIMINGS}}
@enduml";
            var definitionSb = new StringBuilder();
            var deflist = new List<(string display, string name, string group)>();
            foreach (var link in col.linkJobs)
            {
                deflist.Add(($"<font color=seagreen><b>{link.From}</b> -> <b>{link.To}</b></font>", $"{simplifyLink(link.Name)}", link.To));
            }
            foreach (var slot in col.data.Slots)
            {
                for (int i = 0; i < slot.Slot; i++)
                {
                    if (!col.jobs.Any(_ => _.Slot == i && _.Location == slot.DataCenter)) continue;
                    var ps = string.Join(",", col.data.Partitions.Where(_ => _.DataCenter == slot.DataCenter).Select(_ => _.Partition));
                    deflist.Add(($"<font color=maroon><b>{slot.DataCenter} Slot{i}</b> (With {ps})", $"{slot.DataCenter}_{i}", slot.DataCenter));
                }
            }
            var deflistDedup = deflist.GroupBy(_ => _.name).Select(_ => _.First());
            foreach (var group in deflistDedup.GroupBy(_ => _.group))
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

                var list = new List<(int duration, string process)>();
                var jobList = group.OrderBy(_ => _.StartInMs).ToArray();
                for (int i = 0; i < jobList.Length; i++)
                {
                    var job = jobList[i];
                    var lastJob = i - 1 >= 0 ? jobList[i - 1] : null;

                    var lastTime = lastJob == null ? 0 : lastJob.StartInMs + lastJob.DurationInMs;
                    var gap = job.StartInMs - lastTime;
                    if (gap > 0) list.Add((gap, "{-}"));
                    list.Add((job.DurationInMs, job is LinkJobExecutionInfo lj
                        ? $"{lj.Partition} #lightgreen"
                        : $"{job.Name} #tomato"));
                }

                var offset = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    var (duration, process) = list[i];
                    timimgSb.AppendLine($"{(offset == 0 ? "0" : $"+{offset}") } is {process}");
                    offset = duration;
                }

                timimgSb.AppendLine($"{(offset == 0 ? "0" : $"+{offset}") } is {{-}}");

                timimgSb.AppendLine();
            }

            var outputText = t
                .Replace("{{OUTPUT}}", output)
                .Replace("{{SCALE}}", GetScale(col.Time).ToString())
                .Replace("{{DEFINITIONS}}", definitionSb.ToString())
                .Replace("{{TIMINGS}}", timimgSb.ToString());

            File.WriteAllText("temp.uml", outputText);
            //Process.Start("java", $" -jar plantuml.jar temp.uml");
            Process.Start(@"C:\DevTools\jrex86\bin\java.exe", $@" -jar C:\Tools\jar\plantuml.jar temp.uml");
        }

        internal static int GetScale(int milliseconds)
        {
            return (milliseconds / 1000) switch
            {
                > 100 => 10000,
                > 50 => 5000,
                > 40 => 4000,
                > 30 => 3000,
                > 20 => 2000,
                > 10 => 1000,
                _ => 500,
            };
        }

        private static string Indent(string input, int indent = 2)
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