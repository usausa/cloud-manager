namespace CloudManager.Models.Aws.Cost;

public sealed record CostEstimateResult(string Service, string Description, decimal HourlyUsd, decimal MonthlyUsd, int Hours);
