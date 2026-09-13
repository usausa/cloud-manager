namespace CloudManager.Host.Endpoints;

using CloudManager.Host.Infrastructure.Filters;
using CloudManager.Infrastructure.Aws;

public static class S3Endpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapS3Endpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.S3)
            .AddEndpointFilter<AwsExceptionFilter>();

        group.MapGet("/download/{bucket}", HandleDownloadAsync);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    // ブラウザの直接ダウンロード用。HTTP リクエストは画面のセッションを持たないため、プロファイル/リージョンはクエリで受け取る
    private static async ValueTask<IResult> HandleDownloadAsync(
        string bucket,
        [Required] string key,
        [Required] string profile,
        string? region,
        CancellationToken cancellationToken)
    {
        var service = new S3Service(AwsClientFactory.Create(profile, region ?? string.Empty));

        var head = await service.HeadObjectAsync(bucket, key, cancellationToken);
        if (head is null)
        {
            return TypedResults.NotFound();
        }

        var fileName = key.Split('/').LastOrDefault(static x => x.Length > 0) ?? key;
        return TypedResults.Stream(
            stream => service.DownloadToStreamAsync(bucket, key, stream, cancellationToken).AsTask(),
            head.ContentType,
            fileName);
    }
}
