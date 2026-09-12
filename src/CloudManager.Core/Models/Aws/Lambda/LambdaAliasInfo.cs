namespace CloudManager.Models.Aws.Lambda;

public sealed record LambdaAliasInfo(string Name, string FunctionVersion, string? Description, string? AdditionalVersion, double? AdditionalWeight);
