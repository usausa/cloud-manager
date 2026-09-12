namespace CloudManager.Models.Jobs;

public sealed class JobOperationCatalogTests
{
    // 全操作がいずれかのサービスに属し、重複しないこと
    [Fact]
    public void EveryOperationBelongsToExactlyOneService()
    {
        var all = Enum.GetValues<JobServiceType>().SelectMany(JobOperationCatalog.ForService).ToList();

        Assert.Equal(Enum.GetValues<JobOperation>().Order(), all.Order());
        Assert.Equal(all.Count, all.Distinct().Count());
    }

    [Fact]
    public void EveryOperationHasDisplayName()
    {
        foreach (var operation in Enum.GetValues<JobOperation>())
        {
            var name = JobOperationCatalog.DisplayName(operation);

            Assert.False(String.IsNullOrWhiteSpace(name));
            Assert.NotEqual(operation.ToString(), name);
        }
    }
}
