namespace CloudManager.Models.Aws.Vpc;

public sealed record VpcRouteTableInfo(string RouteTableId, string Name, bool IsMain, int RouteCount);
