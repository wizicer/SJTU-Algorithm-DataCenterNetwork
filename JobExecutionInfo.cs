namespace NetworkAlgorithm
{
    using System.Diagnostics;
    using static NetworkAlgorithm.DataHolder;

    [DebuggerDisplay("{Name} ({StartInMs}, {DurationInMs})")]
    public record JobExecutionInfo(string Name)
    {
        public int StartInMs { get; init; }
        public int DurationInMs { get; init; }
    }

    [DebuggerDisplay("Link: {Name} {From}->{To} [{Partition}] ({StartInMs}, {DurationInMs})")]
    public record LinkJobExecutionInfo(string Name, string Partition, DataCenter From, DataCenter To)
        : JobExecutionInfo(Name)
    {
    }

    [DebuggerDisplay("Work: {Name} [{Location}_{Slot}] ({StartInMs}, {DurationInMs})")]
    public record WorkJobExecutionInfo(string Name, JobInfo Job, DataCenter Location, int Slot)
        : JobExecutionInfo(Name)
    {
    }
}