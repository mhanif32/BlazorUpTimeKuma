using BlazorKuma.Data;
using BlazorKuma.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

namespace BlazorKuma.Services
{
    public class UptimeWorker(IDbContextFactory<ApplicationDbContext> dbFactory) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var db = await dbFactory.CreateDbContextAsync(stoppingToken);
                var activeMonitors = await db.Monitors.Where(m => !m.IsPaused).ToListAsync(stoppingToken);

                foreach (var monitor in activeMonitors)
                {
                    // Only check if it's time based on the interval
                    if (DateTime.Now >= monitor.LastCheck.AddSeconds(monitor.IntervalSeconds))
                    {
                        _ = Task.Run(async () => await CheckStatus(monitor), stoppingToken);
                    }
                }

                await Task.Delay(5000, stoppingToken); // Check the queue every 5 seconds
            }
        }

        private async Task CheckStatus(MonitorItem monitor)
        {
            using var db = await dbFactory.CreateDbContextAsync();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool isUp = false;

            try
            {
                if (monitor.Target.StartsWith("http"))
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = await client.GetAsync(monitor.Target);
                    isUp = response.IsSuccessStatusCode;
                }
                else
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(monitor.Target, 3000);
                    isUp = reply.Status == IPStatus.Success;
                }
            }
            catch { isUp = false; }

            stopwatch.Stop();

            // Update Monitor State
            var dbMonitor = await db.Monitors.FindAsync(monitor.Id);
            if (dbMonitor != null)
            {
                dbMonitor.IsUp = isUp;
                dbMonitor.LastCheck = DateTime.Now;

                // Log the Heartbeat
                db.Heartbeats.Add(new HeartbeatRecord
                {
                    MonitorItemId = monitor.Id,
                    Timestamp = DateTime.Now,
                    IsUp = isUp,
                    LatencyMs = stopwatch.ElapsedMilliseconds
                });

                await db.SaveChangesAsync();
            }
        }
    }
}