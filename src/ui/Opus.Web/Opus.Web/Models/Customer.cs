namespace Opus.Web.Models;

public class Customer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class TelemetryMetric
{
    public string DeviceId { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}

public class WorkflowInstance
{
    public string InstanceId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = "Running";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}
