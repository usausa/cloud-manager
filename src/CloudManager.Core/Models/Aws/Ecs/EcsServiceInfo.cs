namespace CloudManager.Models.Aws.Ecs;

public sealed record EcsServiceInfo(string ServiceName, string Status, int DesiredCount, int RunningCount, int PendingCount, string TaskDefinition);
