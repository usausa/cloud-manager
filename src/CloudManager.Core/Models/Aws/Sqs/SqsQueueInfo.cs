namespace CloudManager.Models.Aws.Sqs;

public sealed record SqsQueueInfo(Uri QueueUrl, string QueueName, int ApproximateMessages, int MessagesNotVisible, int DelaySeconds, int VisibilityTimeout);
