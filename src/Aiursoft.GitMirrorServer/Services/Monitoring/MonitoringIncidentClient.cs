using Aiursoft.AiurProtocol.Services;
using Aiursoft.AlertCenter.SDK;
using Aiursoft.AlertCenter.SDK.Models;
using Microsoft.Extensions.Options;

namespace Aiursoft.GitMirrorServer.Services.Monitoring;

public interface IMonitoringIncidentClient
{
    void Configure(MonitoringConfiguration configuration);
    Task<QueryIncidentsResponse> QueryAsync(QueryIncidentsRequest request);
    Task<CreateIncidentResponse> CreateAsync(CreateIncidentRequest request);
    Task<ChangeIncidentSeverityResponse> ChangeSeverityAsync(Guid id, ChangeIncidentSeverityRequest request);
    Task<MitigateIncidentResponse> MitigateAsync(Guid id, MitigateIncidentRequest request);
}

public sealed class MonitoringIncidentClient(AiurProtocolClient http) : IMonitoringIncidentClient
{
    private AlertCenterApiClient? _client;
    public void Configure(MonitoringConfiguration configuration)
    {
        configuration.Validate();
        _client = new AlertCenterApiClient(http,
            Options.Create(new AlertCenterApiOptions { Endpoint = configuration.Endpoint.TrimEnd('/') }),
            new SnapshotKeyProvider(configuration.ApiKey));
    }
    private Task<AlertCenterApiClient> GetClientAsync() => Task.FromResult(
        _client ?? throw new InvalidOperationException("Configure the client before synchronization."));
    public async Task<QueryIncidentsResponse> QueryAsync(QueryIncidentsRequest request) => await (await GetClientAsync()).QueryIncidentsAsync(request);
    public async Task<CreateIncidentResponse> CreateAsync(CreateIncidentRequest request) => await (await GetClientAsync()).CreateIncidentAsync(request);
    public async Task<ChangeIncidentSeverityResponse> ChangeSeverityAsync(Guid id, ChangeIncidentSeverityRequest request) =>
        await (await GetClientAsync()).ChangeIncidentSeverityAsync(id, request);
    public async Task<MitigateIncidentResponse> MitigateAsync(Guid id, MitigateIncidentRequest request) =>
        await (await GetClientAsync()).MitigateIncidentAsync(id, request);
    private sealed class SnapshotKeyProvider(string key) : IAlertCenterApiKeyProvider
    {
        public ValueTask<string> GetApiKeyAsync() => ValueTask.FromResult(key);
    }
}
