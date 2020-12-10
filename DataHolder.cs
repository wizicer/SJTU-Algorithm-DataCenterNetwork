namespace NetworkAlgorithm
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using CsvHelper;
    using CsvHelper.Configuration.Attributes;

    public class DataHolder
    {
        private const string RowSubJob = "SubJob";
        private const string RowExecutionTime = "Time";
        private const string RowBandwidth = "Bandwidth/MBps";
        private readonly string basePath;

        public DataHolder(string basePath)
        {
            this.basePath = basePath;
        }

        public DataCenterPartition[] Partitions { get; set; }
        public DataCenterSlot[] Slots { get; set; }
        public JobInfo[] Jobs { get; set; }
        public DataCenterLink[] Links { get; set; }
        public DataCenterArbitraryLink[] AllLinks { get; set; }

        public void Init()
        {
            this.Partitions = GetRecords<DataCenterPartition>(Path.Combine(this.basePath, "DataCenterPartitions.csv"));
            this.Slots = GetRecords<DataCenterSlot>(Path.Combine(this.basePath, "DataCenterSlots.csv"));
            this.Jobs = GetJobs(Path.Combine(this.basePath, "JobList.csv")).ToArray();
            this.Links = GetLinks(Path.Combine(this.basePath, "Inter-DatacenterLinks.csv")).ToArray();
            this.AllLinks = ProcessArbitraryLinks(this.Links);
        }

        private DataCenterArbitraryLink[] ProcessArbitraryLinks(DataCenterLink[] links)
        {
            var dcs = links
                .Aggregate(
                    new DataCenter[] { },
                    (p, dcl) => p.Concat(new[] { dcl.From, dcl.To }).ToArray(),
                    _ => _.Distinct())
                .ToArray();
            return DijkstrasAlgorithm.GetArbitraryLinks(links, dcs).ToArray();
        }

        private static IEnumerable<JobInfo> GetJobs(string input)
        {
            using var reader = new StreamReader(input);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();
            var depHeaders = csv.Context.HeaderRecord.Except(new[] { "Job", RowSubJob, RowExecutionTime, "Precedence Constraint" });
            while (csv.Read())
            {
                var deps = depHeaders
                    .Select(_ => new { name = _, number = csv.GetField(_) })
                    .Where(_ => !string.IsNullOrWhiteSpace(_.number))
                    .Select(_ => new JobDependence { Depend = _.name, Size = int.Parse(_.number) })
                    .ToArray();

                yield return new JobInfo
                {
                    Name = csv.GetField<string>(RowSubJob),
                    DurationInMs = (int)(csv.GetField<double>(RowExecutionTime) * 1000),
                    Dependences = deps,
                };
            }
        }

        private static IEnumerable<DataCenterLink> GetLinks(string input)
        {
            using var reader = new StreamReader(input);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();
            var toHeaders = csv.Context.HeaderRecord.Except(new[] { RowBandwidth });
            while (csv.Read())
            {
                var tos = toHeaders
                    .Select(_ => new { name = _, number = csv.GetField(_) })
                    .Where(_ => _.number != "-")
                    .Select(_ => new { _.name, number = int.Parse(_.number) });

                var from = csv.GetField<string>(RowBandwidth);

                foreach (var t in tos)
                {
                    yield return new DataCenterLink
                    {
                        From = from,
                        To = t.name,
                        Bandwidth = t.number,
                    };
                }
            }
        }

        private static T[] GetRecords<T>(string input)
        {
            using var reader = new StreamReader(input);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            return csv.GetRecords<T>()
                .ToArray();
        }

        [DebuggerDisplay("{DataCenter}: {Partition}")]
        public class DataCenterPartition
        {
            [Name("Data Partition")]
            public string Partition { get; init; }

            [Name("Location")]
            public string DataCenterCsvString { get; init; }

            public DataCenter DataCenter => DataCenterCsvString;
        }

        [DebuggerDisplay("{DataCenter}: {Slot}")]
        public class DataCenterSlot
        {
            [Name("DC")]
            public string DataCenterCsvString { get; init; }

            [Name("Num of Slots")]
            public int Slot { get; init; }

            public DataCenter DataCenter => DataCenterCsvString;
        }

        [DebuggerDisplay("{From}->{To}: {Bandwidth}")]
        public class DataCenterLink
        {
            public DataCenter From { get; init; }
            public DataCenter To { get; init; }
            public int Bandwidth { get; init; }
        }

        [DebuggerDisplay("Route {From}->{To} [{Links.Length}]: {MinBandwidth}")]
        public class DataCenterArbitraryLink
        {
            public DataCenter From { get; init; }
            public DataCenter To { get; init; }
            public int MinBandwidth { get; init; }
            public DataCenterLink[] Links { get; set; }
        }

        [DebuggerDisplay("{Name}")]
        public class JobInfo
        {
            public string Name { get; init; }
            public int DurationInMs { get; init; }
            public JobDependence[] Dependences { get; init; }
        }

        [DebuggerDisplay("{Depend}: {Size}")]
        public class JobDependence
        {
            public string Depend { get; init; }
            public int Size { get; init; }
        }

        [DebuggerDisplay("{Value}")]
        public class DataCenter
        {
            public DataCenter(string value)
            {
                Value = value;
            }

            public string Value { get; init; }

            public static implicit operator string(DataCenter dc)
            {
                return dc.Value;
            }

            public static implicit operator DataCenter(string dc)
            {
                return new DataCenter(dc);
            }

            public override string ToString()
            {
                return Value;
            }

            public bool Equals(DataCenter other)
            {
                if (ReferenceEquals(other, null))
                    return false;
                if (ReferenceEquals(other, this))
                    return true;
                return Value == other.Value;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as DataCenter);
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }

            public static bool operator ==(DataCenter left, DataCenter right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(DataCenter left, DataCenter right)
            {
                return !(left == right);
            }
        }
    }
    public class DijkstrasAlgorithm
    {

        private static readonly int NO_PARENT = -1;

        private static (int[] shortestDistances, int[] parents) dijkstra(int[,] adjacencyMatrix, int startVertex)
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
                var (distances, parents) = dijkstra(adjacencyMatrix, i);
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
                    yield return new DataHolder.DataCenterArbitraryLink
                    {
                        From = dataCenters[i],
                        To = dataCenters[j],
                        MinBandwidth = pathLinks.Min(_ => _.Bandwidth),
                        Links = pathLinks,
                    };
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