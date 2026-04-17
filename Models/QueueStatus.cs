namespace queue_backend.Models;

public static class QueueStatus
{
    public const string Waiting = "waiting";
    public const string Called = "called";
    public const string Done = "done";
    public const string Cancel = "cancel";

    public static readonly string[] All =
    [
        Waiting,
        Called,
        Done,
        Cancel
    ];

    public static bool IsValid(string? status)
    {
        return status is not null && All.Contains(status, StringComparer.OrdinalIgnoreCase);
    }
}
