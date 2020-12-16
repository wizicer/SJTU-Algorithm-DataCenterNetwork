namespace NetworkAlgorithm.Visualizer
{
    using System;
    using System.IO;
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
        internal static string SetFont(this string content, string? color)
            => $"<font{(color == null ? "" : $" color={color}")}>{content}</font>";
        internal static string GetFormat(this string filename)
            => Path.GetExtension(filename).TrimStart('.');
        internal static string Suffix(this string filename, string suffix)
            => $"{Path.GetFileNameWithoutExtension(filename)}-{suffix}{Path.GetExtension(filename)}";
    }
}