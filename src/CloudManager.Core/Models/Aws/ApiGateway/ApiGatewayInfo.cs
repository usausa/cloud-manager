namespace CloudManager.Models.Aws.ApiGateway;

public sealed record ApiGatewayInfo(
    string Id,
    string Name,
    string? Description,
    DateTime? CreatedDate);
