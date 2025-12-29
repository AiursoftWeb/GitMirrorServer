using Aiursoft.CSTools.Tools;
using Microsoft.Extensions.Hosting;
using static Aiursoft.WebTools.Extends;

[assembly:DoNotParallelize]

namespace Aiursoft.GitMirrorServer.Tests.IntegrationTests;

[TestClass]
public class BasicTests
{
    // ReSharper disable once NotAccessedField.Local
    private readonly string _endpointUrl;
    private readonly int _port;
    // ReSharper disable once NotAccessedField.Local
    private readonly HttpClient _http;
    private IHost? _server;

    public BasicTests()
    {
        _port = Network.GetAvailablePort();
        _endpointUrl = $"http://localhost:{_port}";
        _http = new HttpClient();
    }

    [TestInitialize]
    public async Task CreateServer()
    {
        _server = await AppAsync<Startup>([], port: _port);
        await _server.StartAsync();
    }

    [TestCleanup]
    public async Task CleanServer()
    {
        if (_server == null) return;
        await _server.StopAsync();
        _server.Dispose();
    }

    [TestMethod]
    [DataRow("/")]
    [DataRow("/hOmE?aaaaaa=bbbbbb")]
    [DataRow("/hOmE/InDeX")]
    public async Task GetHome(string url)
    {
        var response = await _http.GetAsync(_endpointUrl + url);
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.AreEqual("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }
}
