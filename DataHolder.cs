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

        public void Init()
        {
            this.Partitions = GetRecords<DataCenterPartition>(Path.Combine(this.basePath, "DataCenterPartitions.csv"));
            this.Slots = GetRecords<DataCenterSlot>(Path.Combine(this.basePath, "DataCenterSlots.csv"));
            this.Jobs = GetJobs(Path.Combine(this.basePath, "JobList.csv")).ToArray();
            this.Links = GetLinks(Path.Combine(this.basePath, "Inter-DatacenterLinks.csv")).ToArray();
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

        [DebuggerDisplay("{Name}")]
        public class JobInfo
        {
            public string Name { get; init; }
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
        }
    }
}