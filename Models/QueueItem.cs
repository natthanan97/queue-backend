using System.ComponentModel.DataAnnotations.Schema;

namespace queue_backend.Models;

public class QueueItem
{
    public long ID { get; set; }
    public string QueueNumber { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int RunningNumber { get; set; }
    public string Status { get; set; } = "waiting";
    public DateTime CreatedAt { get; set; }
}
