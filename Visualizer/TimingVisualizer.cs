namespace NetworkAlgorithm.Visualizer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class TimingVisualizer
    {
        public static void Visualize(FinalExecutionInfoCollection col, string output)
        {
            var t = @"@startuml {{OUTPUT}}
scale {{SCALE}} as 100 pixels
{{DEFINITIONS}}

{{TIMINGS}}
@enduml";

            var outputText = t
                .Replace("{{OUTPUT}}", output)
                .Replace("{{SCALE}}", GetScale(col.Time).ToString())
                .Replace("{{DEFINITIONS}}", GetDefinitions(col).ToString())
                .Replace("{{TIMINGS}}", GetTimings(col).ToString());

            File.WriteAllText("temp.uml", outputText);
            //Process.Start("java", $" -jar plantuml.jar temp.uml");
            var format = Path.GetExtension(output).TrimStart('.');
            Process.Start(@"C:\DevTools\jrex86\bin\java.exe", $@" -jar C:\Tools\jar\plantuml.jar -t{format} temp.uml");
        }

        private static string SimplifyLink(string name) => name.Replace(" -> ", "_");

        private static StringBuilder GetDefinitions(FinalExecutionInfoCollection col)
        {
            var definitionSb = new StringBuilder();
            var deflist = new List<(string display, string name, string group)>();
            foreach (var link in col.LinkJobs)
            {
                deflist.Add(($"<b>{link.From}</b> -> <b>{link.To}</b>".SetFont("seagreen"), $"{SimplifyLink(link.Name)}", link.To));
            }
            foreach (var slot in col.Data.Slots)
            {
                for (int i = 0; i < slot.Slot; i++)
                {
                    if (!col.WorkJobs.Any(_ => _.Slot == i && _.Location == slot.DataCenter)) continue;
                    var ps = string.Join(",", col.Data.Partitions.Where(_ => _.DataCenter == slot.DataCenter).Select(_ => _.Partition));
                    deflist.Add(($"<b>{slot.DataCenter} Slot{i}</b> (With {ps})".SetFont("maroon"), $"{slot.DataCenter}_{i}", slot.DataCenter));
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

            return definitionSb;
        }

        private static StringBuilder GetTimings(FinalExecutionInfoCollection col)
        {
            var timimgSb = new StringBuilder();
            static string getName(JobExecutionInfo _)
                => _ is LinkJobExecutionInfo lj ? SimplifyLink(lj.Name)
                : _ is WorkJobExecutionInfo wj ? $"{wj.Location}_{wj.Slot}"
                : throw new Exception("Unexpected");

            foreach (var group in col.AllJobs.GroupBy(_ => getName(_)))
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

            return timimgSb;
        }

        private static int GetScale(int milliseconds)
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
    }
}