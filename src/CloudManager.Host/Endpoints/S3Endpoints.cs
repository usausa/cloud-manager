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

    // Direct browser download; profile and region are taken from the query
    private static async ValueTask<IResult> HandleDownloadAsync(
        string bucket,
        string? key,
        string? profile,
        string? region,
        CancellationToken cancellationToken)
    {
        if (String.IsNullOrEmpty(key) || String.IsNullOrEmpty(profile))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "key and profile are required.");
        }

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
