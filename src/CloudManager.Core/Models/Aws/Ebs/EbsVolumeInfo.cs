namespace CloudManager.Models.Aws.Ebs;

public sealed record EbsVolumeInfo(
    string VolumeId,
    int SizeGb,
    string VolumeType,
    string State,
    string AvailabilityZone,
    string? AttachedInstanceId,
    DateTime CreateTime);
