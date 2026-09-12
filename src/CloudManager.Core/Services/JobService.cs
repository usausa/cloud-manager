namespace CloudManager.Services;

using CloudManager.Accessors;
using CloudManager.Mappers;
using CloudManager.Models.Jobs;

public sealed class JobService
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
        return entities.Select(JobDefinitionMapper.ToModel).ToList();
    }

    public async ValueTask<JobDefinition?> QueryAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await jobAccessor.QueryAsync(id, cancellationToken);
        return entity is null ? null : JobDefinitionMapper.ToModel(entity);
    }

    // 作成日時・更新日時はここで採番する
    public ValueTask<long> InsertAsync(JobDefinition job, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetLocalNow().DateTime;
        var entity = JobDefinitionMapper.ToEntity(job);
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
        var entity = JobDefinitionMapper.ToEntity(job);
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
}
