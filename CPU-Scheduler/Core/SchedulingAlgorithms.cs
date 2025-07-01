using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CPU_Scheduler.Models;

namespace CPU_Scheduler
{
    public static class SchedulingAlgorithms
    {
        public static List<Process> FCFS(List<Process> processes)
        {
            var sorted = processes.OrderBy(p => p.ArrivalTime).ToList();
            int currentTime = 0;

            foreach (var p in sorted)
            {
                if (currentTime < p.ArrivalTime)
                    currentTime = p.ArrivalTime;

                p.StartTime = currentTime;
                p.WaitingTime = currentTime - p.ArrivalTime;
                p.FinishTime = currentTime + p.BurstTime;
                p.TurnaroundTime = p.FinishTime - p.ArrivalTime;

                currentTime += p.BurstTime;
            }

            return sorted;
        }

        public static List<Process> Priority(List<Process> processes)
        {
            var result = new List<Process>();
            var waiting = new List<Process>(processes);
            int currentTime = 0;

            while (waiting.Count > 0)
            {
                var readyQueue = waiting
                    .Where(p => p.ArrivalTime <= currentTime)
                    .OrderBy(p => p.Priority)
                    .ThenBy(p => p.ArrivalTime)
                    .ToList();

                if (readyQueue.Count == 0)
                {
                    currentTime = waiting.Min(p => p.ArrivalTime);
                    continue;
                }

                var selected = readyQueue.First();
                selected.StartTime = currentTime;
                selected.WaitingTime = currentTime - selected.ArrivalTime;
                selected.FinishTime = selected.StartTime + selected.BurstTime;
                selected.TurnaroundTime = selected.FinishTime - selected.ArrivalTime;

                currentTime += selected.BurstTime;

                result.Add(selected);
                waiting.Remove(selected);
            }

            return result;
        }

        public static List<Process> PriorityPreemptive(List<Process> processes)
        {
            int time = 0;
            var result = new List<Process>();

            // نسخة للعمل بدون تعديل الأصل
            var tempProcesses = processes.Select(p => new Process(p.Name, p.ArrivalTime, p.BurstTime)
            {
                Priority = p.Priority
            }).ToDictionary(p => p.Name);

            var ready = new List<Process>(tempProcesses.Values.OrderBy(p => p.ArrivalTime));
            var active = new List<Process>();

            string lastProcess = null;
            int execStartTime = 0;

            var startTimes = new Dictionary<string, int>();
            var finishTimes = new Dictionary<string, int>();
            var burstOriginal = processes.ToDictionary(p => p.Name, p => p.BurstTime);

            while (ready.Count > 0 || active.Count > 0)
            {
                active.AddRange(ready.Where(p => p.ArrivalTime <= time));
                ready.RemoveAll(p => p.ArrivalTime <= time);

                if (active.Count == 0)
                {
                    if (lastProcess != null)
                    {
                        result.Add(new Process(lastProcess, execStartTime, time - execStartTime));
                        lastProcess = null;
                    }

                    time = ready.Min(p => p.ArrivalTime);
                    continue;
                }

                var selected = active.OrderBy(p => p.Priority).ThenBy(p => p.ArrivalTime).First();

                if (!startTimes.ContainsKey(selected.Name))
                    startTimes[selected.Name] = time;

                if (lastProcess != selected.Name)
                {
                    if (lastProcess != null)
                    {
                        result.Add(new Process(lastProcess, execStartTime, time - execStartTime));
                    }

                    lastProcess = selected.Name;
                    execStartTime = time;
                }

                selected.BurstTime--;
                time++;

                if (selected.BurstTime == 0)
                {
                    finishTimes[selected.Name] = time;
                    active.Remove(selected);
                }
            }

            if (lastProcess != null)
            {
                result.Add(new Process(lastProcess, execStartTime, time - execStartTime));
            }

            // حساب النتائج النهائية لكل عملية أصلية
            foreach (var p in processes)
            {
                p.StartTime = startTimes.ContainsKey(p.Name) ? startTimes[p.Name] : -1;
                p.FinishTime = finishTimes.ContainsKey(p.Name) ? finishTimes[p.Name] : -1;
                p.WaitingTime = p.FinishTime - p.ArrivalTime - burstOriginal[p.Name];
                p.TurnaroundTime = p.FinishTime - p.ArrivalTime;
            }

            return result;
        }

        public static List<Process> RoundRobin(List<Process> processes, int quantum)
        {
            var result = new List<Process>();
            var waiting = processes.Select(p => new Process(p.Name, p.ArrivalTime, p.BurstTime)).ToList();
            var queue = new Queue<Process>();
            int currentTime = 0;

            var burstOriginal = processes.ToDictionary(p => p.Name, p => p.BurstTime);

            waiting = waiting.OrderBy(p => p.ArrivalTime).ToList();

            while (queue.Any() || waiting.Any())
            {
                while (waiting.Any() && waiting.First().ArrivalTime <= currentTime)
                {
                    queue.Enqueue(waiting.First());
                    waiting.RemoveAt(0);
                }

                if (queue.Any())
                {
                    var proc = queue.Dequeue();
                    int burst = Math.Min(proc.BurstTime, quantum);

                    result.Add(new Process(proc.Name, currentTime, burst)
                    {
                        StartTime = currentTime,
                        FinishTime = currentTime + burst
                    });

                    currentTime += burst;
                    proc.BurstTime -= burst;

                    int tot = currentTime;
                    while (waiting.Any() && waiting.First().ArrivalTime <= tot)
                    {
                        queue.Enqueue(waiting.First());
                        tot += waiting.First().BurstTime;
                        waiting.RemoveAt(0);
                    }

                    if (proc.BurstTime > 0)
                    {
                        queue.Enqueue(proc);
                    }
                }
                else if (waiting.Any())
                {
                    currentTime = waiting.First().ArrivalTime;
                }
            }

            // حساب waiting و turnaround من result
            var grouped = result.GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var p in processes)
            {
                var parts = grouped[p.Name];
                int waitingTime = 0;
                int lastFinish = p.ArrivalTime;
                int firstStart = int.MaxValue;
                int finalFinish = 0;

                foreach (var part in parts)
                {
                    if (part.StartTime > lastFinish)
                        waitingTime += part.StartTime - lastFinish;

                    lastFinish = part.StartTime + part.BurstTime;

                    if (part.StartTime < firstStart)
                        firstStart = part.StartTime;

                    if (lastFinish > finalFinish)
                        finalFinish = lastFinish;
                }

                p.StartTime = firstStart;
                p.FinishTime = finalFinish;
                p.TurnaroundTime = finalFinish - p.ArrivalTime;
                p.WaitingTime = p.TurnaroundTime - burstOriginal[p.Name];
            }

            return result;
        }

        public static List<Process> SJF(List<Process> processes)
        {
            var result = new List<Process>();
            var waiting = new List<Process>(processes);
            int currentTime = 0;

            while (waiting.Count > 0)
            {
                var readyQueue = waiting.Where(p => p.ArrivalTime <= currentTime)
                                        .OrderBy(p => p.BurstTime)
                                        .ThenBy(p => p.ArrivalTime)
                                        .ToList();

                if (readyQueue.Count == 0)
                {
                    currentTime = waiting.Min(p => p.ArrivalTime);
                    continue;
                }

                var selected = readyQueue.First();
                selected.StartTime = currentTime;
                selected.FinishTime = selected.StartTime + selected.BurstTime;
                selected.TurnaroundTime = selected.FinishTime - selected.ArrivalTime;
                selected.WaitingTime = selected.StartTime - selected.ArrivalTime;

                currentTime = selected.FinishTime;
                result.Add(selected);
                waiting.Remove(selected);
            }

            return result;
        }

        public static List<Process> SJFPreemptive(List<Process> processes)
        {
            int time = 0;
            var result = new List<Process>();

            var tempProcesses = processes.Select(p => new Process(p.Name, p.ArrivalTime, p.BurstTime)).ToList();

            var ready = new List<Process>(tempProcesses.OrderBy(p => p.ArrivalTime));
            var active = new List<Process>();

            string lastProcess = null;
            int execStartTime = 0;

            while (ready.Count > 0 || active.Count > 0)
            {
                active.AddRange(ready.Where(p => p.ArrivalTime <= time));
                ready.RemoveAll(p => p.ArrivalTime <= time);

                if (active.Count == 0)
                {
                    if (lastProcess != null)
                    {
                        result.Add(new Process(lastProcess, execStartTime, time - execStartTime)
                        {
                            StartTime = execStartTime,
                            FinishTime = time
                        });
                        lastProcess = null;
                    }

                    time = ready.Min(p => p.ArrivalTime);
                    continue;
                }

                var selected = active.OrderBy(p => p.BurstTime).ThenBy(p => p.ArrivalTime).First();

                if (lastProcess != selected.Name)
                {
                    if (lastProcess != null)
                    {
                        result.Add(new Process(lastProcess, execStartTime, time - execStartTime)
                        {
                            StartTime = execStartTime,
                            FinishTime = time
                        });
                    }

                    lastProcess = selected.Name;
                    execStartTime = time;
                }

                selected.BurstTime--;
                time++;

                if (selected.BurstTime == 0)
                {
                    active.Remove(selected);
                }
            }

            if (lastProcess != null)
            {
                result.Add(new Process(lastProcess, execStartTime, time - execStartTime)
                {
                    StartTime = execStartTime,
                    FinishTime = time
                });
            }

            var grouped = result.GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var p in processes)
            {
                var parts = grouped[p.Name];
                int waiting = 0;
                int lastFinish = p.ArrivalTime;
                int firstStart = int.MaxValue;
                int finalFinish = 0;

                foreach (var part in parts)
                {
                    if (part.StartTime > lastFinish)
                        waiting += part.StartTime - lastFinish;

                    lastFinish = part.StartTime + part.BurstTime;

                    // Actual first and last times
                    if (part.StartTime < firstStart)
                        firstStart = part.StartTime;

                    if (part.StartTime + part.BurstTime > finalFinish)
                        finalFinish = part.StartTime + part.BurstTime;
                }

                p.StartTime = firstStart;
                p.FinishTime = finalFinish;
                p.TurnaroundTime = finalFinish - p.ArrivalTime;
                p.WaitingTime = p.TurnaroundTime - p.BurstTime;
            }
            return result;
        }

        public static string BestAlgorithm(List<Process> processes)
        {
            double min = 99999;
            string name = null;
            string title = null;
            int n = 0;
            for (int i = 0; i < 6; i++)
            {
                double totalWaiting = 0;
                double totalTurnaround = 0;
                List<Models.Process> result = null;
                List<Process> p1 = processes;
                switch (i)
                {
                    case 0:
                        result = FCFS(processes);
                        title = "FCFS";
                        break;
                    case 1:
                        result = SJF(processes);
                        title = "Short Job First (SJF)";
                        break;
                    case 2:
                        result = Priority(processes);
                        title = "Priority Non Preemptive";
                        break;
                    case 3:
                        result = SJFPreemptive(processes);
                        title = "Short Job First Preemptive (SJF)";
                        break;
                    case 4:
                        result = PriorityPreemptive(processes);
                        title = "Priority Preemptive";
                        break;
                    case 5:
                        result = RoundRobin(processes, 2);
                        title = "Round Robin";
                        break;
                }
                var finalSummary = GetSummaryResult(processes);
                foreach (var p in processes)
                {
                    foreach (var a in result)
                    {
                        if (a.Name == p.Name)
                        {
                            a.WaitingTime += p.WaitingTime;
                        }
                    }
                }
                foreach (var p in finalSummary)
                {
                    totalWaiting += p.WaitingTime;
                    totalTurnaround += p.TurnaroundTime;
                }
                if (min > totalWaiting)
                {
                    min = totalWaiting;
                    name = title;
                }
                n = finalSummary.Count;
            }
            return name;
        }

        public static string BestAlgorithmAvgWaiting(List<Process> processes)
        {
            double min = 99999;
            int n = 0;
            for(int i=0;i<6;i++)
            {
                double totalWaiting = 0;
                double totalTurnaround = 0;
                List<Models.Process> result = null;
                List<Process> p1 = processes;
                switch(i)
                {
                    case 0:
                        result = FCFS(processes);
                        break;
                    case 1:
                        result = SJF(processes);
                        break;
                    case 2:
                        result = Priority(processes);
                        break;
                    case 3:
                        result = SJFPreemptive(processes);
                        break;
                    case 4:
                        result = PriorityPreemptive(processes);
                        break;
                    case 5:
                        result = RoundRobin(processes, 2);
                        break;
                }
                var finalSummary = GetSummaryResult(processes);
                foreach (var p in processes)
                {
                    foreach (var a in result)
                    {
                        if (a.Name == p.Name)
                        {
                            a.WaitingTime += p.WaitingTime;
                        }
                    }
                }
                foreach (var p in finalSummary)
                {
                    totalWaiting += p.WaitingTime;
                    totalTurnaround += p.TurnaroundTime;
                }
                if(min>totalWaiting)
                {
                    min = totalWaiting;
                }
                n = finalSummary.Count;
            }
            return $"{((float)min / (float)n):0.00}";
        }
        public static List<Process> GetSummaryResult(List<Process> processes)
        {
            return processes.Select(p =>
            {
                var newP = new Process(p.Name, p.ArrivalTime, p.BurstTime, p.Priority);
                newP.StartTime = p.StartTime;
                newP.FinishTime = p.FinishTime;
                newP.WaitingTime = p.WaitingTime;
                newP.TurnaroundTime = p.TurnaroundTime;
                return newP;
            }).ToList();
        }

    }
}
