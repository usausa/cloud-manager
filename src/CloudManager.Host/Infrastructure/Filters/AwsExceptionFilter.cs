namespace CloudManager.Host.Infrastructure.Filters;

using Amazon.Runtime;

// Converts AWS call failures to ProblemDetails
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
            // Unresolved profile or region
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }
}
