namespace CloudManager.Models.Aws.Rds;

public sealed record RdsParamGroupInfo(string GroupName, string Family, string Description)
{
    public string Name => GroupName;
}
