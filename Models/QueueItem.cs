using System.ComponentModel.DataAnnotations.Schema;

namespace queue_backend.Models;

public class QueueItem: BaseModel
{
    public string QueueNumber { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int RunningNumber { get; set; }
    public string Status { get; set; } = "waiting";
}
