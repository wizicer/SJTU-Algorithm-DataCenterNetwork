namespace NetworkAlgorithm.Visualizer
{
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    public class TaskVisualizer
    {
        public static void Visualize(DataHolder data, string output)
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
                .Replace("{{CLUSTERS}}", clusterSb.Indent())
                .Replace("{{CONNECTION}}", jobSb.Indent());

            File.WriteAllText("temptask.dot", outputText);
            Process.Start("dot", $" -T{output.GetFormat()} temptask.dot -o {output}");
        }
    }
}