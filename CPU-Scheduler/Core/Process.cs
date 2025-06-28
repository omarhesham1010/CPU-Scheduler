using System.Windows.Forms;
using System.Drawing;

namespace CPU_Scheduler.Models
{
    public class Process
    {
        public string Name { get; set; }
        public int ArrivalTime { get; set; }
        public int BurstTime { get; set; }
        public int Priority { get; set; }

        // معلومات بعد الجدولة
        public int WaitingTime { get; set; }
        public int TurnaroundTime { get; set; }
        public int StartTime { get; set; }
        public int FinishTime { get; set; }
        public Color color { get; set; }
        public bool Drawn = false;

        public Process(string name, int arrivalTime, int burstTime, int priority = 0)
        {
            Name = name;
            ArrivalTime = arrivalTime;
            BurstTime = burstTime;
            Priority = priority;
        }

    }
}
 