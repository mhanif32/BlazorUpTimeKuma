namespace BlazorKuma.Models
{
    public class MonitorItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public int IntervalSeconds { get; set; } = 60;
        public bool IsUp { get; set; } = true;
        public bool IsPaused { get; set; } = false;
        public DateTime LastCheck { get; set; } = DateTime.Now;

        // --- ADD THIS LINE TO FIX CS1061 ---
        public long LastResponse { get; set; }

        public List<HeartbeatRecord> Heartbeats { get; set; } = new();
    }

    public class HeartbeatRecord
    {
        public int Id { get; set; }
        public int MonitorItemId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsUp { get; set; }
        public long LatencyMs { get; set; }

        public MonitorItem? MonitorItem { get; set; }
    }
}