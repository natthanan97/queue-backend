using System.ComponentModel.DataAnnotations.Schema;

namespace queue_backend.Models;

public class QueueCounter: BaseModel
{
    public string CurrentPrefix { get; set; } = "A";
    public int CurrentNumber { get; set; }

    [NotMapped]
    public string CurrentQueueNumber => $"{CurrentPrefix}{CurrentNumber}";
}
