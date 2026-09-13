namespace CloudManager.Services.Aws;

using Amazon.SQS.Model;

using CloudManager.Infrastructure.Aws;
using CloudManager.Models.Aws.Sqs;

public sealed class SqsService
{
    private readonly AwsClientFactory factory;

    public SqsService(AwsClientFactory factory)
    {
        this.factory = factory;
    }

    public async ValueTask<List<SqsQueueInfo>> ListQueuesAsync(CancellationToken cancellationToken = default)
    {
        using var sqs = factory.CreateSqsClient();
        var urls = new List<string>();
        string? nextToken = null;

        do
        {
            var listResponse = await sqs.ListQueuesAsync(
                new ListQueuesRequest { NextToken = nextToken },
                cancellationToken);
            urls.AddRange(listResponse.QueueUrls ?? []);
            nextToken = listResponse.NextToken;
        }
        while (!String.IsNullOrEmpty(nextToken));

        var result = new List<SqsQueueInfo>();

        foreach (var url in urls)
        {
            var attrResponse = await sqs.GetQueueAttributesAsync(
                new GetQueueAttributesRequest
                {
                    QueueUrl = url,
                    AttributeNames = ["ApproximateNumberOfMessages", "ApproximateNumberOfMessagesNotVisible", "DelaySeconds", "VisibilityTimeout"]
                },
                cancellationToken);

            var name = url.Split('/').LastOrDefault() ?? url;
            _ = Int32.TryParse(attrResponse.Attributes.GetValueOrDefault("ApproximateNumberOfMessages", "0"), out var messages);
            _ = Int32.TryParse(attrResponse.Attributes.GetValueOrDefault("ApproximateNumberOfMessagesNotVisible", "0"), out var notVisible);
            _ = Int32.TryParse(attrResponse.Attributes.GetValueOrDefault("DelaySeconds", "0"), out var delay);
            _ = Int32.TryParse(attrResponse.Attributes.GetValueOrDefault("VisibilityTimeout", "30"), out var visibility);

            result.Add(new SqsQueueInfo(new Uri(url), name, messages, notVisible, delay, visibility));
        }

        return result;
    }

    public async ValueTask<string> SendMessageAsync(Uri queueUrl, string messageBody, int delaySeconds, string? groupId, CancellationToken cancellationToken = default)
    {
        using var sqs = factory.CreateSqsClient();
        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl.AbsoluteUri,
            MessageBody = messageBody,
            DelaySeconds = delaySeconds
        };

        var isFifo = queueUrl.AbsoluteUri.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase);
        if (isFifo)
        {
            request.MessageGroupId = groupId ?? throw new ArgumentException("GroupId is required for FIFO queues.", nameof(groupId));
            request.MessageDeduplicationId = Guid.NewGuid().ToString();
        }

        var response = await sqs.SendMessageAsync(
            request,
            cancellationToken);
        return response.MessageId;
    }

    public async ValueTask<List<SqsMessageInfo>> ReceiveMessagesAsync(Uri queueUrl, int maxMessages, CancellationToken cancellationToken = default)
    {
        using var sqs = factory.CreateSqsClient();
        var response = await sqs.ReceiveMessageAsync(
            new ReceiveMessageRequest
            {
                QueueUrl = queueUrl.AbsoluteUri,
                MaxNumberOfMessages = Math.Min(maxMessages, 10),
                MessageSystemAttributeNames = ["ApproximateFirstReceiveTimestamp"],
                WaitTimeSeconds = 0
            },
            cancellationToken);

#pragma warning disable IDE0028
        return (response.Messages ?? [])
            .Select(m => new SqsMessageInfo(
                m.MessageId,
                m.ReceiptHandle,
                m.Body,
                m.Attributes.GetValueOrDefault("ApproximateFirstReceiveTimestamp")))
            .ToList();
#pragma warning restore IDE0028
    }

    public async ValueTask PurgeQueueAsync(Uri queueUrl, CancellationToken cancellationToken = default)
    {
        using var sqs = factory.CreateSqsClient();
        await sqs.PurgeQueueAsync(
            new PurgeQueueRequest { QueueUrl = queueUrl.AbsoluteUri },
            cancellationToken);
    }
}
