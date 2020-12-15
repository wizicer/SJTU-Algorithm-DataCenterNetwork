namespace NetworkAlgorithm
{
    using CsvHelper;
    using CsvHelper.Configuration.Attributes;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    public class DataHolder
    {
        private const string RowSubJob = "SubJob";
        private const string RowExecutionTime = "Time";
        private const string RowBandwidth = "Bandwidth/MBps";
        private readonly string basePath;

        public DataHolder(string basePath)
        {
            this.basePath = basePath;
            this.Partitions = GetRecords<DataCenterPartition>(Path.Combine(this.basePath, "DataCenterPartitions.csv"));
            this.Slots = GetRecords<DataCenterSlot>(Path.Combine(this.basePath, "DataCenterSlots.csv"));
            this.Jobs = GetJobs(Path.Combine(this.basePath, "JobList.csv")).ToArray();
            this.Links = GetLinks(Path.Combine(this.basePath, "Inter-DatacenterLinks.csv")).ToArray();
            this.AllLinks = GetArbitraryLinks(this.Links);
        }

        public DataCenterPartition[] Partitions { get; set; }
        public DataCenterSlot[] Slots { get; set; }
        public JobInfo[] Jobs { get; set; }
        public DataCenterLink[] Links { get; set; }
        public DataCenterArbitraryLink[] AllLinks { get; set; }

        private DataCenterArbitraryLink[] GetArbitraryLinks(DataCenterLink[] links)
        {
            var dcs = links
                .Aggregate(
                    new DataCenter[] { },
                    (p, dcl) => p.Concat(new[] { dcl.From, dcl.To }).ToArray(),
                    _ => _.Distinct())
                .ToArray();
            return LinkProcessor.GetArbitraryLinks(links, dcs).ToArray();
        }

        private static IEnumerable<JobInfo> GetJobs(string input)
        {
            using var reader = new StreamReader(input);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();
            var depHeaders = csv.Context.HeaderRecord.Except(
                new[] { "Job", RowSubJob, RowExecutionTime, "Precedence Constraint" });
            while (csv.Read())
            {
                var deps = depHeaders
                    .Select(_ => new { name = _, number = csv.GetField(_) })
                    .Where(_ => !string.IsNullOrWhiteSpace(_.number))
                    .Select(_ => new JobDependence(_.name, int.Parse(_.number)))
                    .ToArray();

                yield return new JobInfo(
                    csv.GetField<string>(RowSubJob), (int)(csv.GetField<double>(RowExecutionTime) * 1000), deps);
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
                    yield return new DataCenterLink(from, t.name, t.number);
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
        public record DataCenterPartition
        {
            public string Partition { get; }
            public string DataCenterCsvString { get; }

            public DataCenter DataCenter => DataCenterCsvString;

            public DataCenterPartition(
                [Name("Data Partition")] string partition,
                [Name("Location")] string dataCenterCsvString)
            {
                Partition = partition;
                DataCenterCsvString = dataCenterCsvString;
            }
        }

        [DebuggerDisplay("{DataCenter}: {Slot}")]
        public record DataCenterSlot
        {
            public string DataCenterCsvString { get; }
            public int Slot { get; }

            public DataCenter DataCenter => DataCenterCsvString;

            public DataCenterSlot(
                [Name("DC")] string dataCenterCsvString,
                [Name("Num of Slots")] int slot)
            {
                DataCenterCsvString = dataCenterCsvString;
                Slot = slot;
            }
        }

        [DebuggerDisplay("{From}->{To}: {Bandwidth}")]
        public record DataCenterLink(DataCenter From, DataCenter To, int Bandwidth);

        [DebuggerDisplay("Route {From}->{To} [{Links.Length}]: {MinBandwidth}")]
        public record DataCenterArbitraryLink(
            DataCenter From, DataCenter To, int MinBandwidth, DataCenterLink[] Links);

        [DebuggerDisplay("{Name}")]
        public record JobInfo(string Name, int DurationInMs, JobDependence[] Dependences);

        [DebuggerDisplay("{Depend}: {Size}")]
        public record JobDependence(string Depend, int Size);

        [DebuggerDisplay("{Value}")]
        public record DataCenter(string Value)
        {
            public static implicit operator string(DataCenter dc) => dc.Value;
            public static implicit operator DataCenter(string dc) => new DataCenter(dc);

            public override string ToString() => Value;
        }
    }
}