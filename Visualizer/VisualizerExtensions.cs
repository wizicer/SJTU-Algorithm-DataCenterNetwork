namespace NetworkAlgorithm.Visualizer
{
    using System;
    using System.Text;

    public static class VisualizerExtensions
    {
        public static void VisualizeRelationship(this DataHolder data, string output)
            => RelationshipVisualizer.Visualize(data, output);

        public static void VisualizeTask(this DataHolder data, string output)
            => TaskVisualizer.Visualize(data, output);

        public static void VisualizeTiming(this FinalExecutionInfoCollection col, string output)
            => TimingVisualizer.Visualize(col, output);

        internal static string Indent(this string input, int indent = 2)
        {
            var pad = new string(' ', indent);
            return pad + input.Replace(Environment.NewLine, Environment.NewLine + pad);
        }

        internal static string Indent(this StringBuilder input, int indent = 2) => input.ToString().Indent(indent);
    }
}