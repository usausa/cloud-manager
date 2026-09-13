namespace CloudManager.Models;

// Progress information
public sealed record ProgressUpdate(double Ratio, string? Message);
