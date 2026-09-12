namespace CloudManager.Models.Aws.Lambda;

public sealed record LambdaConcurrencyInfo(string FunctionName, int? ReservedConcurrency, int? ProvisionedConcurrency);
