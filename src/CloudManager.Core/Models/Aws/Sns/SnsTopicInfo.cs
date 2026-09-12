namespace CloudManager.Models.Aws.Sns;

public sealed record SnsTopicInfo(
    string TopicArn,
    string DisplayName,
    int SubscriptionCount);
