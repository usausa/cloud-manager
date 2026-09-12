namespace CloudManager.Models.Aws.Ecs;

public sealed record EcsClusterInfo(string ClusterArn, string ClusterName, string Status, int ActiveServicesCount, int RunningTasksCount);
