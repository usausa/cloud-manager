namespace CloudManager.Models.Aws.EventBridge;

public sealed record EventBridgeRuleInfo(
    string Name,
    string State,
    string? ScheduleExpression,
    string? EventPattern,
    IReadOnlyList<string> Targets);
