namespace CloudManager.Models.Aws.ElasticIp;

public sealed record ElasticIpInfo(
    string AllocationId,
    string PublicIp,
    string? AssociationId,
    string? InstanceId,
    string? NetworkInterfaceId);
