namespace NetworkAlgorithm
{
    using NetworkAlgorithm.Visualizer;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using static NetworkAlgorithm.DataHolder;

    public class Allocator
    {
        public Allocator()
        {
        }

        public FinalExecutionInfoCollection[] Allocate(DataHolder data)
        {
            var ps = data.Partitions.ToDictionary(_ => _.Partition, _ => _.DataCenter);
            var slots = data.Slots.SelectMany(_ => Enumerable.Range(0, _.Slot).Select(o => (_.DataCenter, o))).ToArray();
            var links = data.Links.ToDictionary(_ => (_.From, _.To), _ => _.Bandwidth);

            var jobs = data.Jobs.ToArray();

            var list = new List<FinalExecutionInfoCollection>();
            var best = int.MaxValue;
            var dt = DateTime.Now;
            dfs(new AttemptInfo(ps, slots, links, jobs, new JobExecutionInfoCollection(data)), col =>
            {
                var result = col.Calculate();
                if (result.Time < best)
                {
                    best = result.Time;
                    list.Clear();
                    Console.WriteLine(result.ToString());
                    result.VisualizeTiming("timingBest.eps");
                }

                if (result.Time == best)
                {
                    list.Add(result);
                }

                if ((DateTime.Now - dt).TotalSeconds > 5)
                {
                    result.VisualizeTiming("timing.eps");
                    dt = DateTime.Now;
                }
            });

            foreach (var item in list)
            {
                Console.WriteLine(item.ToString());
            }

            return list.ToArray();
        }

        private void dfs(
            AttemptInfo info,
            Action<JobExecutionInfoCollection> callbackFound)
        {
            var (ps, slots, links, jobs, jobExecutions) = info;
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

                var hslots = heuristic(job, info);
                foreach (var (location, number) in hslots)
                {
                    dfs(
                        info with
                        {
                            PartitionDc = ps.Concat(new[] { new KeyValuePair<string, DataCenter>(job.Name, new DataCenter(location)) }).ToDictionary(_ => _.Key, _ => _.Value),
                            Jobs = jobs.Where(_ => _ != job).ToArray(),
                            Executions = jobExecutions + new WorkJobExecutionInfo(job.Name, job, location, number) { DurationInMs = job.DurationInMs }
                        },
                        callbackFound);
                }
            }
        }

        private IEnumerable<(DataCenter location, int number)> heuristic(
            JobInfo job,
            AttemptInfo info)
        {
            var (ps, slots, links, jobs, jobExecutions) = info;
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

        private record AttemptInfo(
            IReadOnlyDictionary<string, DataCenter> PartitionDc,
            (DataCenter location, int number)[] Slots,
            IReadOnlyDictionary<(DataCenter From, DataCenter To), int> Links,
            JobInfo[] Jobs,
            JobExecutionInfoCollection Executions);
    }
}