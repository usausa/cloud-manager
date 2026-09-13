namespace CloudManager.Services.Aws;

using Amazon.EventBridge.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.EventBridge;

public sealed class EventBridgeService
{
    private readonly AwsClientFactory factory;

    public EventBridgeService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<EventBridgeRuleInfo>> ListRulesAsync(string? busName = null, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateEventBridgeClient();
        var results = new List<EventBridgeRuleInfo>();
        string? nextToken = null;
        do
        {
            var response = await client.ListRulesAsync(new ListRulesRequest
            {
                EventBusName = busName,
                NextToken = nextToken
            },
            cancellationToken);
            foreach (var rule in response.Rules ?? [])
            {
                var targetsResp = await client.ListTargetsByRuleAsync(new ListTargetsByRuleRequest
                {
                    Rule = rule.Name,
                    EventBusName = busName
                },
                cancellationToken);
                var targets = targetsResp.Targets.Select(t => t.Arn ?? string.Empty).ToList();
                results.Add(new EventBridgeRuleInfo(
                    rule.Name ?? string.Empty,
                    rule.State?.Value ?? string.Empty,
                    rule.ScheduleExpression,
                    rule.EventPattern,
                    targets));
            }
            nextToken = response.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));
        return results;
    }

    public async ValueTask EnableRuleAsync(
        string ruleName,
        string? busName = null,
        CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateEventBridgeClient();
        await client.EnableRuleAsync(new EnableRuleRequest { Name = ruleName, EventBusName = busName }, cancellationToken);
    }

    public async ValueTask DisableRuleAsync(
        string ruleName,
        string? busName = null,
        CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateEventBridgeClient();
        await client.DisableRuleAsync(new DisableRuleRequest { Name = ruleName, EventBusName = busName }, cancellationToken);
    }

    public async ValueTask<string> PutEventAsync(
        string source,
        string detailType,
        string detail,
        string? busName = null,
        CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateEventBridgeClient();
        var response = await client.PutEventsAsync(
            new PutEventsRequest
            {
                Entries =
                [
                    new PutEventsRequestEntry
                    {
                        Source = source,
                        DetailType = detailType,
                        Detail = detail,
                        EventBusName = busName
                    }
                ]
            },
            cancellationToken);
        return response.Entries?.FirstOrDefault()?.EventId ?? string.Empty;
    }
}
