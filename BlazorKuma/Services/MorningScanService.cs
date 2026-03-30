using Microsoft.Toolkit.Uwp.Notifications;
using System.Net.NetworkInformation;
using BlazorKuma.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorKuma.Services;

public class MorningScanService : BackgroundService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly SslCheckService _sslService;
    private readonly ILogger<MorningScanService> _logger;

    public MorningScanService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        SslCheckService sslService,
        ILogger<MorningScanService> logger)
    {
        _dbFactory = dbFactory;
        _sslService = sslService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TechUnited Morning Scan Service Started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // 1. Calculate time until 8:00 AM
            var now = DateTime.Now;
            var nextRun = now.Date.AddHours(8);
            if (now > nextRun) nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;
            _logger.LogInformation("Next Morning Audit scheduled for {Time}", nextRun);

            // 2. Wait until the scheduled time
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break; // Exit gracefully if service stops
            }

            // 3. Run the Audit
            await RunFullInfrastructureAudit();
        }
    }

    private async Task RunFullInfrastructureAudit()
    {
        _logger.LogInformation("Starting TechUnited Morning Audit...");

        using var db = await _dbFactory.CreateDbContextAsync();
        var monitors = await db.Monitors.ToListAsync();

        int failures = 0;
        var failureSummary = new List<string>();

        foreach (var m in monitors)
        {
            bool isUp = true;
            string issueType = "";

            // Handle Internal IP Pings (e.g., 10.0.0.30)
            if (m.Target.Contains("10.0.0."))
            {
                try
                {
                    var ping = new Ping();
                    var reply = await ping.SendPingAsync(m.Target.Split(':')[0], 2000);
                    isUp = reply.Status == IPStatus.Success;
                    if (!isUp) issueType = $"Ping: {reply.Status}";
                }
                catch (Exception) { isUp = false; issueType = "Ping Exception"; }
            }
            // Handle Domain SSL Checks
            else if (m.Target.StartsWith("http"))
            {
                var domain = m.Target.Replace("https://", "").Replace("http://", "").Split('/')[0];
                var ssl = await _sslService.GetSslDetailsAsync(domain);

                if (!ssl.IsValid)
                {
                    isUp = false;
                    issueType = "SSL Invalid";
                }
                else if (ssl.ExpiryDate < DateTime.Now.AddDays(7))
                {
                    isUp = false;
                    issueType = $"SSL Expiry ({ssl.ExpiryDate:MM/dd})";
                }
            }

            if (!isUp)
            {
                failures++;
                failureSummary.Add($"{m.Name} ({issueType})");
                _logger.LogWarning("Morning Audit Issue: {Name} - {Issue}", m.Name, issueType);
            }
        }

        // --- RECORD TO DATABASE ---
        if (failures > 0)
        {
            var notification = new NotificationRecord
            {
                Title = "Morning Audit Failure",
                Message = $"{failures} items failed: {string.Join(", ", failureSummary)}",
                Timestamp = DateTime.Now,
                Severity = "Critical",
                IsRead = false
            };

            db.Notifications.Add(notification);
            await db.SaveChangesAsync();

            // --- TRIGGER WINDOWS NOTIFICATION ---
            ShowWindowsNotification(failures);
        }
        else
        {
            _logger.LogInformation("Morning Audit completed: All TechUnited systems green.");
        }
    }

    private void ShowWindowsNotification(int failureCount)
    {
        try
        {
            new ToastContentBuilder()
                .AddHeader("TechUnited_Audit", "Morning Infrastructure Report", "")
                .AddText($"⚠️ Warning: {failureCount} servers/domains need attention!")
                .AddArgument("action", "viewDashboard")
                // Use protocol activation to open the browser directly
                .AddButton(new ToastButton("Open Dashboard", "https://localhost:7233/")
                {
                    ActivationType = ToastActivationType.Protocol
                })
                .Show();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show Windows Toast notification.");
        }
    }
}