namespace NetworkAlgorithm
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using static NetworkAlgorithm.DataHolder;

    public class JobExecutionInfoCollection : IReadOnlyCollection<JobExecutionInfo>
    {
        internal readonly DataHolder data;
        internal readonly WorkJobExecutionInfo[] jobs;
        internal LinkJobExecutionInfo[] linkJobs;
        internal JobExecutionInfo[] allJobs;

        public JobExecutionInfoCollection(DataHolder data)
            : this(data, new WorkJobExecutionInfo[] { })
        {
        }

        public JobExecutionInfoCollection(DataHolder data, WorkJobExecutionInfo[] jobs)
        {
            this.data = data;
            this.jobs = jobs;
        }

        public int Count => jobs.Length;

        public IEnumerator<JobExecutionInfo> GetEnumerator() => (IEnumerator<JobExecutionInfo>)jobs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => jobs.GetEnumerator();

        public static JobExecutionInfoCollection operator +(JobExecutionInfoCollection collection, WorkJobExecutionInfo job)
        {
            return new JobExecutionInfoCollection(collection.data, collection.jobs.Concat(new[] { job }).ToArray());
        }

        internal void Calculate()
        {
            var ps = data.Partitions.ToDictionary(_ => _.Partition, _ => new { parts = new List<(DataCenter dc, int avail)>(new[] { (_.DataCenter, 0) }) });
            var linkJobs = new List<LinkJobExecutionInfo>();
            var workJobs = new List<WorkJobExecutionInfo>();
            foreach (var job in jobs)
            {
                var depFinishTime = 0;
                foreach (var dep in job.Job.Dependences)
                {
                    var parts = ps[dep.Depend].parts;
                    var to = job.Location;
                    var part = parts.Any(_ => _.dc == to) ? parts.First(_ => _.dc == to) : parts.First();// assume only allow to copy from initial data center
                    var from = part.dc;
                        var avail = part.avail;
                    if (from != to)
                    {
                        var link = data.Links.FirstOrDefault(_ => _.From == from && _.To == to);
                        var flow = $"{from} -> {to}";
                        var exist = linkJobs.Where(_ => _.Name == flow)
                            .OrderByDescending(_ => _.StartInMs)
                            .FirstOrDefault();
                        var start = Math.Max(avail, exist == null ? 0 : exist.StartInMs + exist.DurationInMs);
                        var duration = (int)Math.Ceiling(dep.Size * 1000d / link.Bandwidth);
                        linkJobs.Add(new LinkJobExecutionInfo
                        {
                            Name = flow,
                            DurationInMs = duration,
                            StartInMs = start,
                            Partition = dep.Depend,
                            From = from,
                            To = to,
                        });
                        var thisDepFinishTime = start + duration;
                        parts.Add((to, thisDepFinishTime));
                        if (depFinishTime < thisDepFinishTime) depFinishTime = thisDepFinishTime;
                    }
                    else
                    {
                        depFinishTime = Math.Max(depFinishTime, avail);
                    }
                }

                workJobs.Add(new WorkJobExecutionInfo
                {
                    Job = job.Job,
                    Name = job.Name,
                    DurationInMs = job.DurationInMs,
                    Location = job.Location,
                    Slot = job.Slot,
                    StartInMs = depFinishTime,
                });

                ps.Add(job.Name, new { parts = new List<(DataCenter dc, int avail)>(new[] { (job.Location, depFinishTime + job.DurationInMs) }) });
            }

            this.linkJobs = linkJobs.ToArray();
            this.allJobs = workJobs
                .OfType<JobExecutionInfo>()
                .Concat(linkJobs)
                .ToArray();
        }

        public int Time => this.allJobs
            .Select(_ => _.StartInMs + _.DurationInMs)
            .OrderByDescending(_ => _)
            .First();

        public override string ToString()
        {
            var strJobs = allJobs
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