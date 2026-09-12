namespace CloudManager.Models.Aws.Elb;

public sealed record ElbInfo(
    string Name,
    string Type,
    string Scheme,
    string DnsName,
    string State,
    string VpcId,
    string Arn);
