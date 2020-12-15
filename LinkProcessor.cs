namespace NetworkAlgorithm
{
    using System.Collections.Generic;
    using System.Linq;

    public class LinkProcessor
    {
        private static readonly int NO_PARENT = -1;

        private static (int[] shortestDistances, int[] parents) Dijkstra(int[,] adjacencyMatrix, int startVertex)
        {
            int v = adjacencyMatrix.GetLength(0);
            int[] shortestDistances = new int[v];
            bool[] included = new bool[v];

            // Initialize
            for (int i = 0; i < v; i++)
            {
                shortestDistances[i] = int.MaxValue;
                included[i] = false;
            }

            // Distance of source vertex from itself is always 0
            shortestDistances[startVertex] = 0;

            // Parent array to store shortest path tree
            int[] parents = new int[v];

            // The starting vertex does not have a parent
            parents[startVertex] = NO_PARENT;

            for (int i = 1; i < v; i++)
            {
                int nearestVertex = -1;
                int shortestDistance = int.MaxValue;
                for (int j = 0; j < v; j++)
                {
                    if (!included[j] && shortestDistances[j] < shortestDistance)
                    {
                        nearestVertex = j;
                        shortestDistance = shortestDistances[j];
                    }
                }

                included[nearestVertex] = true;

                for (int j = 0; j < v; j++)
                {
                    int edgeDistance = adjacencyMatrix[nearestVertex, j];

                    if (edgeDistance > 0 && ((shortestDistance + edgeDistance) < shortestDistances[j]))
                    {
                        parents[j] = nearestVertex;
                        shortestDistances[j] = shortestDistance + edgeDistance;
                    }
                }
            }

            return (shortestDistances, parents);
        }

        public static IEnumerable<DataHolder.DataCenterArbitraryLink> GetArbitraryLinks(
            DataHolder.DataCenterLink[] links, DataHolder.DataCenter[] dataCenters)
        {
            var n = dataCenters.Length;
            var dictDc = dataCenters.Select((_, i) => (_, i)).ToDictionary(_ => _._, _ => _.i);
            var rdictDc = dataCenters.Select((_, i) => (_, i)).ToDictionary(_ => _.i, _ => _._);
            int[,] adjacencyMatrix = new int[n, n];
            foreach (var link in links)
            {
                adjacencyMatrix[dictDc[link.From], dictDc[link.To]] = link.Bandwidth;
            }

            for (int i = 0; i < n; i++)
            {
                var (distances, parents) = Dijkstra(adjacencyMatrix, i);
                for (int j = 0; j < distances.Length; j++)
                {
                    var paths = getPath(j, parents).ToArray();
                    var diaPath = string.Join(",", paths);
                    var pathLinks = paths
                        .Aggregate(
                            (new DataHolder.DataCenterLink[] { }, i),
                            (result, current) => (result.Item1.Concat(new[] { links.First(_ => dictDc[_.From] == result.Item2 && dictDc[_.To] == current) }).ToArray(), current),
                            _ => _.Item1)
                        .ToArray();
                    if (pathLinks.Length > 1) pathLinks = pathLinks.Where(_ => _.From != _.To).ToArray();
                    yield return new DataHolder.DataCenterArbitraryLink(
                        dataCenters[i], dataCenters[j], pathLinks.Min(_ => _.Bandwidth), pathLinks);
                }
            }

            static IEnumerable<int> getPath(int currentVertex, int[] parents)
            {
                if (currentVertex == NO_PARENT) yield break;
                var subpath = getPath(parents[currentVertex], parents);
                foreach (var p in subpath)
                {
                    yield return p;
                }

                yield return currentVertex;
            }
        }
    }
}