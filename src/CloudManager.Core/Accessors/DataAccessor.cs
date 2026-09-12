namespace CloudManager.Accessors;

[DataAccessor]
public sealed partial class DataAccessor
{
    [Execute]
    public partial void Create();

    [ExecuteScalar]
    public partial ValueTask<int> CountAsync(string? name, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<DataEntity>> QueryPageAsync(string? name, string order, int offset, int size, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<DataEntity>> QueryAllAsync(CancellationToken cancellationToken);

    // ReSharper disable once CSharpWarnings::CS8424 (SDA0305がEnumeratorCancellation注釈を要求するため。実装はジェネレータ側で処理される)
    [Query]
    public partial IAsyncEnumerable<DataEntity> QueryExportEnumerable([EnumeratorCancellation] CancellationToken cancellationToken);

    [QueryFirst]
    public partial ValueTask<DataEntity?> QueryAsync(long id);

    [ExecuteScalar]
    public partial ValueTask<long> InsertAsync(string name, int value, DateTime createdAt);

    [Execute]
    public partial ValueTask<int> UpdateAsync(long id, string name, int value);

    [Execute]
    public partial ValueTask<int> DeleteAsync(long id);
}
