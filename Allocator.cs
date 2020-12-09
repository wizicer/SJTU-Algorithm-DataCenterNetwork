namespace NetworkAlgorithm
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using static NetworkAlgorithm.DataHolder;

    public class Allocator
    {
        public Allocator()
        {
        }

        public JobExecutionInfoCollection[] Allocate(DataHolder data)
        {
            var ps = data.Partitions.ToDictionary(_ => _.Partition, _ => _.DataCenter);
            var slots = data.Slots.SelectMany(_ => Enumerable.Range(0, _.Slot).Select(o => (_.DataCenter, o, new int[] { }))).ToArray();
            var links = data.Links.ToDictionary(_ => (_.From, _.To), _ => _.Bandwidth);

            var jobs = data.Jobs.ToArray();

            var list = new List<JobExecutionInfoCollection>();
            var best = int.MaxValue;
            dfs(ps, slots, links, jobs, new JobExecutionInfoCollection(data), _ =>
            {
                _.Calculate();
                if (_.Time < best)
                {
                    best = _.Time;
                    list.Clear();
                }

                if (_.Time == best)
                {
                    list.Add(_);
                }
            });

            foreach (var item in list)
            {
                Console.WriteLine(item.ToString());
            }

            return list.ToArray();
        }

        private void dfs(
            IReadOnlyDictionary<string, DataCenter> ps,
            (DataCenter location, int number, int[] occupations)[] slots,
            IReadOnlyDictionary<(DataCenter From, DataCenter To), int> links,
            JobInfo[] jobs,
            JobExecutionInfoCollection jobExecutions,
            Action<JobExecutionInfoCollection> callbackFound)
        {
            if (!jobs.Any())
            {
                callbackFound(jobExecutions);
                return;
            }

            foreach (var job in jobs)
            {
                foreach (var slot in slots.GroupBy(_ => _.location).Select(_ => _.First()))
                {
                    var flagFeasible = true;

                    // check dependence
                    foreach (var dep in job.Dependences)
                    {
                        if (!ps.ContainsKey(dep.Depend))
                        {
                            flagFeasible = false;
                            break;
                        }

                        if (!links.ContainsKey((ps[dep.Depend], slot.location)))
                        {
                            flagFeasible = false;
                            break;
                        }
                    }

                    if (!flagFeasible) continue; // not feasible to put into this slot, try next

                    dfs(
                        ps.Concat(new[] { new KeyValuePair<string, DataCenter>(job.Name, new DataCenter(slot.location)) }).ToDictionary(_ => _.Key, _ => _.Value),
                        slots.Where(_ => _ != slot).ToArray(),
                        links,
                        jobs.Where(_ => _ != job).ToArray(),
                        jobExecutions + new JobExecutionInfo { Name = job.Name, Location = slot.location, DurationInMs = job.DurationInMs, Slot = slot.number, Job = job },
                        callbackFound);
                }
            }
        }

        public class JobExecutionInfoCollection : IReadOnlyCollection<JobExecutionInfo>
        {
            internal readonly DataHolder data;
            internal readonly JobExecutionInfo[] jobs;
            internal JobExecutionInfo[] linkJobs;
            internal JobExecutionInfo[] allJobs;

            public JobExecutionInfoCollection(DataHolder data)
                : this(data, new JobExecutionInfo[] { })
            {
            }

            public JobExecutionInfoCollection(DataHolder data, JobExecutionInfo[] jobs)
            {
                this.data = data;
                this.jobs = jobs;
            }

            public int Count => jobs.Length;

            public IEnumerator<JobExecutionInfo> GetEnumerator() => (IEnumerator<JobExecutionInfo>)jobs.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => jobs.GetEnumerator();

            public static JobExecutionInfoCollection operator +(JobExecutionInfoCollection collection, JobExecutionInfo job)
            {
                return new JobExecutionInfoCollection(collection.data, collection.jobs.Concat(new[] { job }).ToArray());
            }

            internal void Calculate()
            {
                var ps = data.Partitions.ToDictionary(_ => _.Partition, _ => new { dc = _.DataCenter, avail = 0 });
                var linkJobs = new List<JobExecutionInfo>();
                var workJobs = new List<JobExecutionInfo>();
                foreach (var job in jobs)
                {
                    var depFinishTime = 0;
                    foreach (var dep in job.Job.Dependences)
                    {
                        var avail = ps[dep.Depend].avail;
                        var from = ps[dep.Depend].dc;
                        var to = job.Location;
                        var link = data.Links.FirstOrDefault(_ => _.From == from && _.To == to);
                        var flow = $"{from} -> {to}";
                        if (from != to)
                        {
                            var exist = linkJobs.Where(_ => _.Name == flow)
                                .OrderByDescending(_ => _.StartInMs)
                                .FirstOrDefault();
                            var start = exist == null ? avail : exist.StartInMs + exist.DurationInMs;
                            var duration = (int)Math.Ceiling(dep.Size * 1000d / link.Bandwidth);
                            linkJobs.Add(new JobExecutionInfo
                            {
                                Name = flow,
                                DurationInMs = duration,
                                StartInMs = start,
                                ForJobName = dep.Depend,
                                From = from,
                                To = to,
                            });
                            var thisDepFinishTime = start + duration;
                            if (depFinishTime < thisDepFinishTime) depFinishTime = thisDepFinishTime;
                        }
                    }

                    workJobs.Add(new JobExecutionInfo
                    {
                        Job = job.Job,
                        Name = job.Name,
                        DurationInMs = job.DurationInMs,
                        Location = job.Location,
                        Slot = job.Slot,
                        StartInMs = depFinishTime,
                    });

                    ps.Add(job.Name, new { dc = job.Location, avail = depFinishTime + job.DurationInMs });
                }

                this.linkJobs = linkJobs.ToArray();
                this.allJobs = workJobs
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
                    .Select(_ => (_.Job == null ? $"{_.Name}" : $"{_.Name}[{_.Location}]") + $" ({_.StartInMs}, {_.DurationInMs})")
                    ;
                return $"{Time}: " + string.Join(", ", strJobs);
            }
        }

        public class JobExecutionInfo
        {
            public string Name { get; init; }
            public int StartInMs { get; set; }
            public int DurationInMs { get; set; }
            public DataCenter Location { get; set; }
            public int Slot { get; set; }

            public JobInfo Job { get; set; }

            public string ForJobName { get; init; }
            public DataCenter From { get; init; }
            public DataCenter To { get; init; }
        }
    }
}