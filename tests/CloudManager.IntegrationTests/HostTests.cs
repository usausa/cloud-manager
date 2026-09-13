namespace CloudManager;

public sealed class HostTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory factory;

    public HostTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task HealthReturnsOk()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task RootShowsHomePage()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Contains("CloudManager", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownPageReturnsNotFoundPage()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/unknown", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("ページが見つかりません", content, StringComparison.Ordinal);
    }

    // API returns a plain 404 instead of HTML
    [Fact]
    public async Task UnknownApiReturnsPlainNotFound()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/api/unknown", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(content);
    }

    [Fact]
    public async Task S3DownloadWithoutKeyReturnsBadRequest()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/api/s3/download/bucket?profile=cloudmanager-test", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // An unknown profile returns ProblemDetails without calling AWS
    [Fact]
    public async Task S3DownloadWithUnknownProfileReturnsProblem()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/api/s3/download/bucket?key=file.txt&profile=cloudmanager-test&region=ap-northeast-1", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("cloudmanager-test", content, StringComparison.Ordinal);
    }
}
