namespace CloudManager.Services;

using System.Text.Json;

using CloudManager.Accessors;
using CloudManager.Models.Jobs;

using Smart.Mapper;

public sealed partial class JobService
{
    private readonly JobAccessor jobAccessor;

    private readonly TimeProvider timeProvider;

    public JobService(
        JobAccessor jobAccessor,
        TimeProvider timeProvider)
    {
        this.jobAccessor = jobAccessor;
        this.timeProvider = timeProvider;
    }

    public void CreateTable() =>
        jobAccessor.Create();

    public async ValueTask<List<JobDefinition>> QueryAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await jobAccessor.QueryAllAsync(cancellationToken);
#pragma warning disable IDE0028
        return entities.Select(ToModel).ToList();
#pragma warning restore IDE0028
    }

    public async ValueTask<JobDefinition?> QueryAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await jobAccessor.QueryAsync(id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    // Created and updated timestamps are assigned here
    public ValueTask<long> InsertAsync(JobDefinition job, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetLocalNow().DateTime;
        var entity = ToEntity(job);
        return jobAccessor.InsertAsync(
            entity.Name,
            entity.Description,
            entity.ProfileName,
            entity.RegionName,
            entity.ServiceType,
            entity.Operation,
            entity.ParametersJson,
            entity.CronExpression,
            entity.CronTimeZone,
            entity.IsEnabled,
            now,
            now,
            cancellationToken);
    }

    public async ValueTask<bool> UpdateAsync(JobDefinition job, CancellationToken cancellationToken = default)
    {
        var entity = ToEntity(job);
        var rows = await jobAccessor.UpdateAsync(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.ProfileName,
            entity.RegionName,
            entity.ServiceType,
            entity.Operation,
            entity.ParametersJson,
            entity.CronExpression,
            entity.CronTimeZone,
            entity.IsEnabled,
            timeProvider.GetLocalNow().DateTime,
            cancellationToken);
        return rows > 0;
    }

    public async ValueTask<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var rows = await jobAccessor.DeleteAsync(id, cancellationToken);
        return rows > 0;
    }

    // Parameters are stored as JSON and enums by name
    [Mapper]
    [MapProperty(nameof(JobDefinition.Parameters), nameof(JobDefinitionEntity.ParametersJson), Converter = nameof(DeserializeParameters))]
    private static partial JobDefinition ToModel(JobDefinitionEntity entity);

    [Mapper]
    [MapProperty(nameof(JobDefinitionEntity.ParametersJson), nameof(JobDefinition.Parameters), Converter = nameof(SerializeParameters))]
    private static partial JobDefinitionEntity ToEntity(JobDefinition job);

    private static JobParameters DeserializeParameters(string json) =>
        JsonSerializer.Deserialize<JobParameters>(json) ??
        throw new InvalidOperationException("Failed to deserialize parameters.");

    private static string SerializeParameters(JobParameters parameters) =>
        JsonSerializer.Serialize(parameters);
}
