namespace CloudManager.Models.Jobs;

using System.Text.Json.Serialization;

// Per-operation parameters, serialized as JSON with a type discriminator
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(Ec2InstanceParameters), nameof(Ec2InstanceParameters))]
[JsonDerivedType(typeof(RdsInstanceParameters), nameof(RdsInstanceParameters))]
[JsonDerivedType(typeof(EcsDesiredCountParameters), nameof(EcsDesiredCountParameters))]
[JsonDerivedType(typeof(LambdaInvokeParameters), nameof(LambdaInvokeParameters))]
[JsonDerivedType(typeof(CloudFrontInvalidateParameters), nameof(CloudFrontInvalidateParameters))]
public abstract record JobParameters;

public sealed record Ec2InstanceParameters(string InstanceId) : JobParameters;

public sealed record RdsInstanceParameters(string DbInstanceId) : JobParameters;

public sealed record EcsDesiredCountParameters(string Cluster, string ServiceName, int DesiredCount) : JobParameters;

public sealed record LambdaInvokeParameters(string FunctionName, string? Payload, string InvocationType) : JobParameters;

public sealed record CloudFrontInvalidateParameters(string DistributionId, string Paths) : JobParameters;
