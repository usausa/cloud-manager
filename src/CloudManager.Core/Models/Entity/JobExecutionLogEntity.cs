namespace CloudManager.Models.Entity;

public sealed class JobExecutionLogEntity
{
    public long Id { get; set; }

    public long JobId { get; set; }

    public string JobName { get; set; } = default!;

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public string Status { get; set; } = default!;

    public string? Message { get; set; }

    public string? ErrorDetail { get; set; }
}
