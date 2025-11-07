using System.Net.Http.Json;
using Opus.Web.Models;

namespace Opus.Web.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiService> _logger;

    public ApiService(HttpClient http, ILogger<ApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    // Customer API methods
    public async Task<PagedResult<Customer>> GetCustomersAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            // For now, return mock data - will integrate with real API
            var customers = GenerateMockCustomers(100);
            var skip = (page - 1) * pageSize;

            return new PagedResult<Customer>
            {
                Items = customers.Skip(skip).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = customers.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customers");
            throw new ApplicationException("Failed to fetch customers. Please try again.", ex);
        }
    }

    public async Task<Customer?> GetCustomerAsync(string id)
    {
        try
        {
            var response = await _http.GetAsync($"/customers/{id}");
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                throw new ApplicationException($"API returned {response.StatusCode}");
            }

            return await response.Content.ReadFromJsonAsync<Customer>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching customer {Id}", id);
            throw new ApplicationException("Network error. Please check your connection.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer {Id}", id);
            throw new ApplicationException("Failed to fetch customer. Please try again.", ex);
        }
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/customers", customer);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Customer>()
                ?? throw new ApplicationException("Invalid response from server");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating customer");
            throw new ApplicationException("Network error. Please check your connection.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            throw new ApplicationException("Failed to create customer. Please try again.", ex);
        }
    }

    public async Task UpdateCustomerAsync(string id, Customer customer)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/customers/{id}", customer);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error updating customer {Id}", id);
            throw new ApplicationException("Network error. Please check your connection.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {Id}", id);
            throw new ApplicationException("Failed to update customer. Please try again.", ex);
        }
    }

    public async Task DeleteCustomerAsync(string id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/customers/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting customer {Id}", id);
            throw new ApplicationException("Network error. Please check your connection.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {Id}", id);
            throw new ApplicationException("Failed to delete customer. Please try again.", ex);
        }
    }

    // Telemetry API methods
    public async Task<PagedResult<TelemetryMetric>> GetTelemetryMetricsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            // Mock data for now
            var metrics = GenerateMockTelemetry(500);
            var skip = (page - 1) * pageSize;

            return new PagedResult<TelemetryMetric>
            {
                Items = metrics.Skip(skip).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = metrics.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching telemetry metrics");
            throw new ApplicationException("Failed to fetch telemetry data. Please try again.", ex);
        }
    }

    // Workflow API methods
    public async Task<PagedResult<WorkflowInstance>> GetWorkflowsAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            // Mock data for now
            var workflows = GenerateMockWorkflows(50);
            var skip = (page - 1) * pageSize;

            return new PagedResult<WorkflowInstance>
            {
                Items = workflows.Skip(skip).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = workflows.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflows");
            throw new ApplicationException("Failed to fetch workflows. Please try again.", ex);
        }
    }

    public async Task<WorkflowInstance> StartWorkflowAsync(string type)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"/workflow/start/{type}", new { });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<WorkflowInstance>()
                ?? throw new ApplicationException("Invalid response from server");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting workflow");
            throw new ApplicationException("Failed to start workflow. Please try again.", ex);
        }
    }

    // Mock data generators
    private List<Customer> GenerateMockCustomers(int count)
    {
        return Enumerable.Range(1, count).Select(i => new Customer
        {
            Id = $"customer-{i:D3}",
            Name = $"Customer {i}",
            Email = $"customer{i}@example.com",
            CreatedAt = DateTime.UtcNow.AddDays(-count + i)
        }).ToList();
    }

    private List<TelemetryMetric> GenerateMockTelemetry(int count)
    {
        var random = new Random();
        return Enumerable.Range(1, count).Select(i => new TelemetryMetric
        {
            DeviceId = $"device-{random.Next(1, 11):D3}",
            MetricType = random.Next(2) == 0 ? "temperature" : "humidity",
            Value = random.NextDouble() * 100,
            Timestamp = DateTime.UtcNow.AddMinutes(-count + i)
        }).ToList();
    }

    private List<WorkflowInstance> GenerateMockWorkflows(int count)
    {
        var statuses = new[] { "Running", "Completed", "Failed", "Pending" };
        var types = new[] { "data-processing", "report-generation", "data-validation" };
        var random = new Random();

        return Enumerable.Range(1, count).Select(i => new WorkflowInstance
        {
            InstanceId = Guid.NewGuid().ToString(),
            Type = types[random.Next(types.Length)],
            Status = statuses[random.Next(statuses.Length)],
            StartedAt = DateTime.UtcNow.AddHours(-count + i)
        }).ToList();
    }
}
