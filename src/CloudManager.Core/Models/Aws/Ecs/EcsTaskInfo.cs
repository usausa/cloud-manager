namespace CloudManager.Models.Aws.Ecs;

public sealed record EcsTaskInfo(string TaskArn, string TaskId, string Status, string LastStatus, string StartedBy, DateTime? StartedAt);
