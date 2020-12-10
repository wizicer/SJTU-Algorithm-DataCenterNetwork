namespace NetworkAlgorithm
{
    using System.Diagnostics;
    using static NetworkAlgorithm.DataHolder;

    [DebuggerDisplay("{Name} ({StartInMs}, {DurationInMs})")]
    public class JobExecutionInfo
    {
        public string Name { get; init; }
        public int StartInMs { get; set; }
        public int DurationInMs { get; set; }
    }

    [DebuggerDisplay("Link: {Name} {From}->{To} [{Partition}] ({StartInMs}, {DurationInMs})")]
    public class LinkJobExecutionInfo : JobExecutionInfo
    {
        public string Partition { get; init; }
        public DataCenter From { get; init; }
        public DataCenter To { get; init; }
    }

    [DebuggerDisplay("Work: {Name} [{Location}_{Slot}] ({StartInMs}, {DurationInMs})")]
    public class WorkJobExecutionInfo : JobExecutionInfo
    {
        public JobInfo Job { get; set; }
        public DataCenter Location { get; set; }
        public int Slot { get; set; }
    }
}