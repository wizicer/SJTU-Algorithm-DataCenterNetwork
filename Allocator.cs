namespace NetworkAlgorithm
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using static NetworkAlgorithm.DataHolder;

    public class Allocator
    {
        public Allocator()
        {
        }

        public void Allocate(DataHolder data)
        {
            var ps = data.Partitions.ToDictionary(_ => _.Partition, _ => _.DataCenter);
            var slots = data.Slots.SelectMany(_ => Enumerable.Range(0, _.Slot).Select(o => (_.DataCenter, new int[] { }))).ToArray();
            var links = data.Links.ToDictionary(_ => (_.From, _.To), _ => _.Bandwidth);

            var jobs = data.Jobs.ToArray();

            dfs(ps, slots, links, jobs, new JobExecutionInfo[] { }, _ => Console.WriteLine(_.ToString()));
        }

        private void dfs(
            IReadOnlyDictionary<string, DataCenter> ps,
            (DataCenter location, int[] occupations)[] slots,
            IReadOnlyDictionary<(DataCenter From, DataCenter To), int> links,
            JobInfo[] jobs,
            JobExecutionInfo[] jobExecutions,
            Action<JobExecutionInfo[]> callbackFound)
        {
            if (!jobs.Any())
            {
                callbackFound(jobExecutions);
                return;
            }

            foreach (var job in jobs)
            {
                foreach (var slot in slots)
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
                        jobExecutions.Concat(new[] { new JobExecutionInfo { Name = job.Name, Location = slot.location } }).ToArray(),
                        callbackFound);
                }
            }
        }

        public class JobExecutionInfo
        {
            public string Name { get; init; }
            public int StartInMs { get; set; }
            public int DurationInMs { get; set; }
            public DataCenter Location { get; set; }
        }
    }
}