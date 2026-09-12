namespace CloudManager.Host.Settings;

public sealed class AwsSetting
{
    [Required]
    public string DefaultProfile { get; set; } = default!;

    public string? DefaultRegion { get; set; }
}
