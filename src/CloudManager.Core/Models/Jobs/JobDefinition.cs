namespace CloudManager.Models.Jobs;

public sealed record JobDefinition(
    long Id,
    string Name,
    string? Description,
    string ProfileName,
    string RegionName,
    JobServiceType ServiceType,
    JobOperation Operation,
    JobParameters Parameters,
    string CronExpression,
    JobCronTimeZone CronTimeZone,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime UpdatedAt);
