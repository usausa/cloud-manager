namespace CloudManager.Models.Aws.Ec2;

public sealed record SsmRunResult(string Status, string StandardOutput, string StandardError);
