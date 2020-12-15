namespace NetworkAlgorithm
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using static NetworkAlgorithm.DataHolder;

    public class JobExecutionInfoCollection : IReadOnlyCollection<JobExecutionInfo>
    {
        private readonly DataHolder data;
        private readonly WorkJobExecutionInfo[] jobs;

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

        internal FinalExecutionInfoCollection Calculate()
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
                    var part = parts.Any(_ => _.dc == to) ? parts.First(_ => _.dc == to) : parts.First();
                    var from = part.dc;
                    var avail = part.avail;
                    if (from != to)
                    {
                        var aLink = data.AllLinks.FirstOrDefault(_ => _.From == from && _.To == to);
                        if (aLink == null) throw new Exception("dependence unmet");
                        var thisLinkFinishTime = 0;
                        foreach (var link in aLink.Links)
                        {
                            var flow = $"{link.From} -> {link.To}";
                            var exist = linkJobs.Where(_ => _.Name == flow)
                                .OrderByDescending(_ => _.StartInMs)
                                .FirstOrDefault();
                            var start = Math.Max(thisLinkFinishTime, Math.Max(avail, exist == null ? 0 : exist.StartInMs + exist.DurationInMs));
                            var duration = (int)Math.Ceiling(dep.Size * 1000d / link.Bandwidth);
                            linkJobs.Add(new LinkJobExecutionInfo(flow, dep.Depend, link.From, link.To)
                            {
                                DurationInMs = duration,
                                StartInMs = start,
                            });
                            thisLinkFinishTime = start + duration;
                            parts.Add((link.To, thisLinkFinishTime));
                        }

                        var thisDepFinishTime = thisLinkFinishTime;
                        if (depFinishTime < thisDepFinishTime) depFinishTime = thisDepFinishTime;
                    }
                    else
                    {
                        depFinishTime = Math.Max(depFinishTime, avail);
                    }
                }

                workJobs.Add(new WorkJobExecutionInfo(job.Name, job.Job, job.Location, job.Slot)
                {
                    DurationInMs = job.DurationInMs,
                    StartInMs = depFinishTime,
                });

                ps.Add(job.Name, new { parts = new List<(DataCenter dc, int avail)>(new[] { (job.Location, depFinishTime + job.DurationInMs) }) });
            }

            return new FinalExecutionInfoCollection(this.data, linkJobs.ToArray(), workJobs.ToArray());
        }
    }
}