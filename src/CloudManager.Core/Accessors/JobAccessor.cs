namespace CloudManager.Accessors;

[DataAccessor]
public sealed partial class JobAccessor
{
    [Execute]
    public partial void Create();

    [Query]
    public partial ValueTask<List<JobDefinitionEntity>> QueryAllAsync(CancellationToken cancellationToken);

    [QueryFirst]
    public partial ValueTask<JobDefinitionEntity?> QueryAsync(long id, CancellationToken cancellationToken);

    [ExecuteScalar]
    public partial ValueTask<long> InsertAsync(
        string name,
        string? description,
        string profileName,
        string regionName,
        string serviceType,
        string operation,
        string parametersJson,
        string cronExpression,
        string cronTimeZone,
        bool isEnabled,
        DateTime createdAt,
        DateTime updatedAt,
        CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> UpdateAsync(
        long id,
        string name,
        string? description,
        string profileName,
        string regionName,
        string serviceType,
        string operation,
        string parametersJson,
        string cronExpression,
        string cronTimeZone,
        bool isEnabled,
        DateTime updatedAt,
        CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> DeleteAsync(long id, CancellationToken cancellationToken);
}
