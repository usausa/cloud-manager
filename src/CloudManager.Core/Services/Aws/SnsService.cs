namespace CloudManager.Services.Aws;

using Amazon.SimpleNotificationService.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Sns;

public sealed class SnsService
{
    private readonly AwsClientFactory factory;

    public SnsService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<SnsTopicInfo>> ListTopicsAsync()
    {
        using var client = factory.CreateSnsClient();
        var results = new List<SnsTopicInfo>();
        string? nextToken = null;
        do
        {
            var response = await client.ListTopicsAsync(new ListTopicsRequest { NextToken = nextToken });
            foreach (var t in response.Topics ?? [])
            {
                var attrs = await client.GetTopicAttributesAsync(new GetTopicAttributesRequest { TopicArn = t.TopicArn });
                attrs.Attributes.TryGetValue("DisplayName", out var displayName);
                attrs.Attributes.TryGetValue("SubscriptionsConfirmed", out var subCount);
                results.Add(new SnsTopicInfo(
                    t.TopicArn ?? string.Empty,
                    displayName ?? string.Empty,
                    int.TryParse(subCount, out var n) ? n : 0));
            }
            nextToken = response.NextToken;
        }
        while (!string.IsNullOrEmpty(nextToken));
        return results;
    }

    public async ValueTask<List<SnsSubscriptionInfo>> ListSubscriptionsAsync(string topicArn)
    {
        using var client = factory.CreateSnsClient();
        var results = new List<SnsSubscriptionInfo>();
        string? nextToken = null;
        do
        {
            var response = await client.ListSubscriptionsByTopicAsync(new ListSubscriptionsByTopicRequest
            {
                TopicArn = topicArn,
                NextToken = nextToken
            });
            foreach (var s in response.Subscriptions ?? [])
            {
                results.Add(new SnsSubscriptionInfo(
                    s.Endpoint ?? string.Empty,
                    s.Protocol ?? string.Empty,
                    s.SubscriptionArn ?? string.Empty));
            }
            nextToken = response.NextToken;
        }
        while (!string.IsNullOrEmpty(nextToken));
        return results;
    }

    public async ValueTask<string> PublishAsync(
        string topicArn,
        string? subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateSnsClient();
        var request = new PublishRequest
        {
            TopicArn = topicArn,
            Message = message
        };
        if (!string.IsNullOrWhiteSpace(subject))
        {
            request.Subject = subject;
        }
        var response = await client.PublishAsync(request, cancellationToken);
        return response.MessageId ?? string.Empty;
    }
}
