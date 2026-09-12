namespace CloudManager.Models.Aws.ApiGateway;

public sealed record StageInfo(
    string StageName,
    string? DeploymentId,
    DateTime? LastUpdated,
    bool TracingEnabled);
