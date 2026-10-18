using System.Net;
using System.Text;
using Aiursoft.GitMirrorServer.Services;

namespace Aiursoft.GitMirrorServer.Tests;

[TestClass]
public class GitLabServiceTests
{
    private const string Projects = """
        [{"name":"Video Float","path":"Video-float","description":"Demo","visibility":"public","archived":false}]
        """;

    [TestMethod]
    [DataRow(true, "groups")]
    [DataRow(false, "users")]
    public async Task GetRepositoriesAsync_UsesRepositoryPathForMirroring(bool isOrg, string collection)
    {
        var requests = new List<string>();
        var handler = new StubHandler(request =>
        {
            requests.Add(request.RequestUri!.PathAndQuery);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Projects, Encoding.UTF8, "application/json")
            };
        });
        var service = new GitLabService("https://gitlab.example.com", null, handler);

        var repositories = await service.GetRepositoriesAsync("Anduin", isOrg);

        var repository = repositories.Single();
        Assert.AreEqual("Video-float", repository.Name);
        Assert.AreEqual("Demo", repository.Description);
        Assert.IsFalse(repository.IsPrivate);
        Assert.IsFalse(repository.Archived);
        Assert.AreEqual("https://gitlab.example.com/Anduin/Video-float.git",
            service.GetCloneUrl("Anduin", repository.Name));
        CollectionAssert.AreEqual(
            new[] { $"/api/v4/{collection}/Anduin/projects?per_page=100{(isOrg ? "&include_subgroups=true" : "")}&archived=false" },
            requests);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task GetRepositoriesAsync_ExcludesArchivedProjectsWhenResponseOmitsArchiveMetadata(bool isOrg)
    {
        var handler = new StubHandler(request =>
        {
            // Anonymous GitLab lists may omit archived, even for archived projects.
            var json = request.RequestUri!.Query.TrimStart('?').Split('&').Contains("archived=false")
                ? """[{"path":"active","visibility":"public"}]"""
                : """[{"path":"active","visibility":"public"},{"path":"archived","visibility":"public"}]""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        var service = new GitLabService("https://gitlab.example.com", null, handler);

        var repositories = await service.GetRepositoriesAsync("Anduin", isOrg);

        Assert.AreEqual(1, repositories.Count);
        Assert.AreEqual("active", repositories.Single().Name);
    }

    [TestMethod]
    [DataRow(true, "groups")]
    [DataRow(false, "users")]
    public async Task EnsureRepositoryExistsAsync_MatchesPathInsteadOfDisplayName(bool isOrg, string collection)
    {
        var requests = new List<(HttpMethod Method, string Path)>();
        var handler = new StubHandler(request =>
        {
            requests.Add((request.Method, request.RequestUri!.PathAndQuery));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Projects, Encoding.UTF8, "application/json")
            };
        });
        var service = new GitLabService("https://gitlab.example.com", null, handler);

        await service.EnsureRepositoryExistsAsync("Anduin", "Video-float", isOrg);

        CollectionAssert.AreEqual(
            new[] { (HttpMethod.Get, $"/api/v4/{collection}/Anduin/projects?search=Video-float") }, requests);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(respond(request));
        }
    }
}
