namespace CloudManager.Models.Entity;

public sealed class JobDefinitionEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    public string ProfileName { get; set; } = default!;

    public string RegionName { get; set; } = default!;

    public string ServiceType { get; set; } = default!;

    public string Operation { get; set; } = default!;

    public string ParametersJson { get; set; } = default!;

    public string CronExpression { get; set; } = default!;

    public string CronTimeZone { get; set; } = default!;

    public bool IsEnabled { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
