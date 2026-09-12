namespace CloudManager.Models.Aws.Lambda;

public sealed record LambdaFunctionInfo(
    string FunctionName,
    string Runtime,
    string Handler,
    long CodeSize,
    string State,
    DateTime? LastModified);
