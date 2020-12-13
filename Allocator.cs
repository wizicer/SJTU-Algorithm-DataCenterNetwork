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

        public JobExecutionInfoCollection[] Allocate(DataHolder data)
        {
            var ps = data.Partitions.ToDictionary(_ => _.Partition, _ => _.DataCenter);
            var slots = data.Slots.SelectMany(_ => Enumerable.Range(0, _.Slot).Select(o => (_.DataCenter, o))).ToArray();
            var links = data.Links.ToDictionary(_ => (_.From, _.To), _ => _.Bandwidth);

            var jobs = data.Jobs.ToArray();

            var list = new List<JobExecutionInfoCollection>();
            var best = int.MaxValue;
            dfs(ps, slots, links, jobs, new JobExecutionInfoCollection(data), col =>
            {
                col.Calculate();
                if (col.Time < best)
                {
                    best = col.Time;
                    list.Clear();
                    Console.WriteLine(col.ToString());
                }

                if (col.Time == best)
                {
                    list.Add(col);
                    var vis = new Visualizer();
                    vis.VisualizeTiming(col, "timing.png");
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
            (DataCenter location, int number)[] slots,
            IReadOnlyDictionary<(DataCenter From, DataCenter To), int> links,
            JobInfo[] jobs,
            JobExecutionInfoCollection jobExecutions,
            Action<JobExecutionInfoCollection> callbackFound)
        {
            //if (jobs.Length < 9)
            //    Console.WriteLine($"ps: {ps.Count}, slots: {slots.Length}, jobs: {jobs.Length}, exec: {jobExecutions.Count}");
            if (!jobs.Any())
            {
                callbackFound(jobExecutions);
                return;
            }

            foreach (var job in jobs)
            {
                // check partition dependence
                if (!job.Dependences.All(dep => ps.ContainsKey(dep.Depend))) continue;

                foreach (var slot in slots.GroupBy(_ => _.location).Select(_ => _.First()))
                {
                    dfs(
                        ps.Concat(new[] { new KeyValuePair<string, DataCenter>(job.Name, new DataCenter(slot.location)) }).ToDictionary(_ => _.Key, _ => _.Value),
                        slots.ToArray(),
                        links,
                        jobs.Where(_ => _ != job).ToArray(),
                        jobExecutions + new WorkJobExecutionInfo { Name = job.Name, Location = slot.location, DurationInMs = job.DurationInMs, Slot = slot.number, Job = job },
                        callbackFound);
                }
            }
        }
    }
}