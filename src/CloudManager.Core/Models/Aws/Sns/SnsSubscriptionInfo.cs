namespace CloudManager.Models.Aws.Sns;

public sealed record SnsSubscriptionInfo(
    string Endpoint,
    string Protocol,
    string SubscriptionArn);
