using System.ComponentModel.DataAnnotations.Schema;

namespace queue_backend.Models;

public class QueueCounter
{
    public int ID { get; set; }
    public string CurrentPrefix { get; set; } = "A";
    public int CurrentNumber { get; set; }
    public DateTime UpdatedAt { get; set; }
}
