using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BlazorKuma.Models; // Add this

namespace BlazorKuma.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {

        public DbSet<NotificationRecord> Notifications { get; set; }
        // Add these two lines so the DB creates the tables
        public DbSet<MonitorItem> Monitors { get; set; }
        public DbSet<HeartbeatRecord> Heartbeats { get; set; }
    }
}