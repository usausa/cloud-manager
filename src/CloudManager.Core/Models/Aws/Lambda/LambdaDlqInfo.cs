namespace CloudManager.Models.Aws.Lambda;

public sealed record LambdaDlqInfo(string FunctionName, string? DlqArn);
