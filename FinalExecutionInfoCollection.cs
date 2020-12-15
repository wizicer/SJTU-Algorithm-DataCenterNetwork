namespace NetworkAlgorithm
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public record FinalExecutionInfoCollection : IReadOnlyCollection<JobExecutionInfo>
    {
        public JobExecutionInfo[] AllJobs;

        public FinalExecutionInfoCollection(DataHolder data, LinkJobExecutionInfo[] linkJobs, WorkJobExecutionInfo[] workJobs)
        {
            this.Data = data;
            this.LinkJobs = linkJobs;
            this.WorkJobs = workJobs;
            this.AllJobs = workJobs
                .OfType<JobExecutionInfo>()
                .Concat(linkJobs)
                .ToArray();
        }

        public int Count => this.AllJobs.Length;

        public IEnumerator<JobExecutionInfo> GetEnumerator() => (IEnumerator<JobExecutionInfo>)this.AllJobs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.AllJobs.GetEnumerator();

        public int Time => this.AllJobs
            .Select(_ => _.StartInMs + _.DurationInMs)
            .OrderByDescending(_ => _)
            .First();

        public DataHolder Data { get; }
        public LinkJobExecutionInfo[] LinkJobs { get; }
        public WorkJobExecutionInfo[] WorkJobs { get; }

        public override string? ToString()
        {
            if (this.AllJobs == null) return base.ToString();
            var strJobs = this.AllJobs
                .OrderBy(_ => _.StartInMs)
                .Select(_ => getName(_)
                    + $" ({_.StartInMs}, {_.DurationInMs})")
                ;
            return $"{Time}: " + string.Join(", ", strJobs);

            static string getName(JobExecutionInfo _)
                => _ is LinkJobExecutionInfo lj ? $"{lj.Name}"
                : _ is WorkJobExecutionInfo wj ? $"{wj.Name}[{wj.Location}]"
                : throw new Exception("Unexpected");
        }
    }
}