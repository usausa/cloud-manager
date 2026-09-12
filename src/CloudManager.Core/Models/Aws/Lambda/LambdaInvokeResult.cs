namespace CloudManager.Models.Aws.Lambda;

public sealed record LambdaInvokeResult(int StatusCode, string? FunctionError, string Payload, string? LogResult);
