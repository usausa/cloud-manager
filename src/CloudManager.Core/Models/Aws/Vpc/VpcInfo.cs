namespace CloudManager.Models.Aws.Vpc;

public sealed record VpcInfo(string VpcId, string Name, string CidrBlock, bool IsDefault, string State);
