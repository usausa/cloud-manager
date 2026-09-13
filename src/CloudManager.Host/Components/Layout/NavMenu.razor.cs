namespace CloudManager.Host.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

public sealed partial class NavMenu
{
    // Expand the group containing the current URL
    private static readonly Dictionary<NavGroup, string[]> GroupRoutes = new()
    {
        [NavGroup.Compute] = ["ec2", "ebs", "ecs", "lambda"],
        [NavGroup.Container] = ["ecr"],
        [NavGroup.Storage] = ["s3", "rds", "dynamodb"],
        [NavGroup.Network] = ["vpc", "elastic-ip", "cloudfront", "elb", "route53", "acm"],
        [NavGroup.Api] = ["apigw", "eventbridge"],
        [NavGroup.Messaging] = ["sqs", "sns"],
        [NavGroup.Monitor] = ["cloudwatch", "cloudwatch-logs"],
        [NavGroup.Security] = ["ssm-parameters", "secrets-manager", "cognito"],
        [NavGroup.Jobs] = ["jobs"],
        [NavGroup.Other] = ["cost", "settings"]
    };

    private readonly Dictionary<NavGroup, bool> expanded = Enum.GetValues<NavGroup>().ToDictionary(static x => x, static _ => false);

    [Inject]
    public required NavigationManager Navigation { get; set; }

    protected override void OnInitialized()
    {
        ApplyActiveGroup(Navigation.Uri);
        Navigation.LocationChanged += OnLocationChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Navigation.LocationChanged -= OnLocationChanged;
        }

        base.Dispose(disposing);
    }

    private bool IsExpanded(NavGroup group) => expanded[group];

    private void SetExpanded(NavGroup group, bool value) => expanded[group] = value;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        ApplyActiveGroup(e.Location);
        _ = InvokeAsync(StateHasChanged);
    }

    private void ApplyActiveGroup(string uri)
    {
        var group = ResolveGroup(new Uri(uri).AbsolutePath.TrimStart('/'));
        if (group.HasValue)
        {
            expanded[group.Value] = true;
        }
    }

    private static NavGroup? ResolveGroup(string path)
    {
        foreach (var (group, routes) in GroupRoutes)
        {
            if (routes.Any(route => path.Equals(route, StringComparison.OrdinalIgnoreCase) || path.StartsWith(route + "/", StringComparison.OrdinalIgnoreCase)))
            {
                return group;
            }
        }

        return null;
    }

    private enum NavGroup
    {
        Compute,
        Container,
        Storage,
        Network,
        Api,
        Messaging,
        Monitor,
        Security,
        Jobs,
        Other
    }
}
