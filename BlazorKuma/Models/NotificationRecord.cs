namespace BlazorKuma.Data; // or .Models depending on your structure

public class NotificationRecord
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsRead { get; set; } = false;
    public string Severity { get; set; } = "Warning";
}