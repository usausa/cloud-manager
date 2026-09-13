namespace CloudManager.Host.Infrastructure.Filters;

using Amazon.Runtime;

// AWS 呼び出しの失敗を ProblemDetails に変換する
public sealed class AwsExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (AmazonServiceException ex)
        {
            var statusCode = (int)ex.StatusCode;
            return TypedResults.Problem(
                statusCode: (statusCode >= 400) && (statusCode <= 599) ? statusCode : StatusCodes.Status502BadGateway,
                title: $"[{ex.ErrorCode}] {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            // プロファイル/リージョン未解決
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }
}
