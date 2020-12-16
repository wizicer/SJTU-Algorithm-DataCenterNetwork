namespace NetworkAlgorithm.Visualizer
{
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class TaskVisualizer
    {
        public static void Visualize(DataHolder data, string output)
        {
            foreach (var group in data.Jobs.GroupBy(_ => _.Name.Substring(1, 1)))
            {
                Visualize(group.ToArray(), group.Key, output);
            }
        }

        private static void Visualize(DataHolder.JobInfo[] jobs, string taskName, string output)
        {
            var t = File.ReadAllText("taskdag.dot");
            var clusterSb = new StringBuilder();
            foreach (var job in jobs)
            {
                clusterSb.AppendLine($"{job.Name} [shape = {GraphShape.square}, fillcolor = brown1];");
            }

            var jobSb = new StringBuilder();
            foreach (var job in jobs)
            {
                foreach (var dep in job.Dependences)
                {
                    jobSb.AppendLine($"{dep.Depend} -> {job.Name} [label =\"{dep.Size}\", color = {(dep.Depend.StartsWith("t") ? "black" : "grey50")}];");
                }
            }

            var outputText = t
                .Replace("{{CLUSTERS}}", clusterSb.Indent())
                .Replace("{{CONNECTION}}", jobSb.Indent());

            File.WriteAllText("temptask.dot", outputText);
            var p = Process.Start("dot", $" -T{output.GetFormat()} temptask.dot -o {output.Suffix(taskName)}");
            p.WaitForExit();
        }
    }
}