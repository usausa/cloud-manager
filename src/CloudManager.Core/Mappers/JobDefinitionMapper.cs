namespace CloudManager.Mappers;

using System.Text.Json;

using CloudManager.Models.Jobs;

// JobDefinitionEntity と JobDefinition の相互変換。パラメータはJSON、列挙は名前で保存する
public static class JobDefinitionMapper
{
    public static JobDefinition ToModel(JobDefinitionEntity entity)
    {
        var parameters = JsonSerializer.Deserialize<JobParameters>(entity.ParametersJson) ??
                         throw new InvalidOperationException($"Failed to deserialize parameters. id=[{entity.Id}]");

        return new JobDefinition(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.ProfileName,
            entity.RegionName,
            Enum.Parse<JobServiceType>(entity.ServiceType),
            Enum.Parse<JobOperation>(entity.Operation),
            parameters,
            entity.CronExpression,
            Enum.Parse<JobCronTimeZone>(entity.CronTimeZone),
            entity.IsEnabled,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public static JobDefinitionEntity ToEntity(JobDefinition job) => new()
    {
        Id = job.Id,
        Name = job.Name,
        Description = job.Description,
        ProfileName = job.ProfileName,
        RegionName = job.RegionName,
        ServiceType = job.ServiceType.ToString(),
        Operation = job.Operation.ToString(),
        ParametersJson = JsonSerializer.Serialize(job.Parameters),
        CronExpression = job.CronExpression,
        CronTimeZone = job.CronTimeZone.ToString(),
        IsEnabled = job.IsEnabled,
        CreatedAt = job.CreatedAt,
        UpdatedAt = job.UpdatedAt
    };
}
