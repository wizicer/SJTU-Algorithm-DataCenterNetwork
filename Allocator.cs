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

                var hslots = heuristic(job, ps, slots, links, jobs, jobExecutions);
                foreach (var (location, number) in hslots)
                {
                    dfs(
                        ps.Concat(new[] { new KeyValuePair<string, DataCenter>(job.Name, new DataCenter(location)) }).ToDictionary(_ => _.Key, _ => _.Value),
                        slots.ToArray(),
                        links,
                        jobs.Where(_ => _ != job).ToArray(),
                        jobExecutions + new WorkJobExecutionInfo { Name = job.Name, Location = location, DurationInMs = job.DurationInMs, Slot = number, Job = job },
                        callbackFound);
                }
            }
        }

        private IEnumerable<(DataCenter location, int number)> heuristic(
            JobInfo job,
            IReadOnlyDictionary<string, DataCenter> ps,
            (DataCenter location, int number)[] slots,
            IReadOnlyDictionary<(DataCenter From, DataCenter To), int> links,
            JobInfo[] jobs,
            JobExecutionInfoCollection jobExecutions)
        {
            return slots
                .GroupBy(_ => _.location)
                .Select(_ => _.First())
                .Select(_ => new
                {
                    slot = _,
                    weight = (DataCenterContainsSameMainJob(_.location), DataCenterContainsMainJobPartitions(_.location)) switch
                    {
                        (true, true) => 3,
                        (true, false) => 2,
                        (false, true) => 1,
                        _ => 0,
                    }
                })
                .OrderByDescending(_ => _.weight)
                .Select(_ => _.slot)
                ;

            bool DataCenterContainsSameMainJob(DataCenter location) => jobExecutions.OfType<WorkJobExecutionInfo>()
                .Any(_ => _.Location == location && GetMainJobName(_.Name) == GetMainJobName(job.Name));

            bool DataCenterContainsMainJobPartitions(DataCenter location) => ps
                .Any(_ => _.Value == location && GetMainJobNameFromPartition(_.Key) == GetMainJobName(job.Name));

            string GetMainJobName(string name) => name.Substring(1, 1);
            string GetMainJobNameFromPartition(string partition) => partition.Substring(0, 1);
        }
    }
}