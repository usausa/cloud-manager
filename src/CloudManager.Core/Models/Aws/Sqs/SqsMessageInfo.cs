namespace CloudManager.Models.Aws.Sqs;

public sealed record SqsMessageInfo(string MessageId, string? ReceiptHandle, string Body, string? ApproximateFirstReceiveTimestamp);
