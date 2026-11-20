using System.Net;
using System.Text;
using System.Text.Json;
using Aiursoft.GitMirrorServer.Services;

namespace Aiursoft.GitMirrorServer.Tests;

[TestClass]
public class GitLabServiceTests
{
    private const string Projects = """
        [{"name":"Video Float","path":"Video-float","description":"Demo","visibility":"public","archived":false}]
        """;

    [TestMethod]
    public async Task GetRepositoriesAsync_SkipsPendingDeletionEvenWhenArchivedMirrorsAreEnabled()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                [{"id":643,"path":"raillive-deletion_scheduled-643","visibility":"internal"},
                 {"id":644,"path":"marked","visibility":"internal","marked_for_deletion_on":"2026-11-20"},
                 {"id":645,"path":"also-marked","visibility":"internal","marked_for_deletion_at":"2026-11-20"},
                 {"id":646,"path":"docs-deletion_scheduled-999","visibility":"public","archived":true},
                 {"id":647,"path":"active","visibility":"public","marked_for_deletion_at":null}]
                """, Encoding.UTF8, "application/json")
        });
        var service = new GitLabService("https://gitlab.example.com", "source-token", handler);

        var repositories = await service.GetRepositoriesAsync("Anduin", false, includeArchived: true);

        CollectionAssert.AreEqual(new[] { "docs-deletion_scheduled-999", "active" },
            repositories.Select(repo => repo.Name).ToArray());
    }

    [TestMethod]
    public async Task GetRepositoriesAsync_RejectsNullOnLaterPage()
    {
        var handler = new StubHandler(request =>
        {
            var secondPage = request.RequestUri!.Query.Contains("&page=2", StringComparison.Ordinal);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(secondPage ? "null" : Projects, Encoding.UTF8, "application/json")
            };
            if (!secondPage) response.Headers.Add("X-Next-Page", "2");
            return response;
        });
        var service = new GitLabService("https://gitlab.example.com", "source-token", handler);
        await Assert.ThrowsExactlyAsync<JsonException>(() => service.GetRepositoriesAsync("Anduin", false));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task GetRepositoriesAsync_AuthenticatedDiscoveryMirrorsInternalButNeverPrivateProjects(bool isOrg)
    {
        var handler = new StubHandler(request =>
        {
            Assert.AreEqual("source-token", request.Headers.GetValues("PRIVATE-TOKEN").Single());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    [{"path":"public-project","visibility":"public"},
                     {"path":"internal-project","visibility":"internal"},
                     {"path":"secret-project","visibility":"private"},
                     {"path":"unknown-visibility","visibility":"unknown"}]
                    """, Encoding.UTF8, "application/json")
            };
        });
        var service = new GitLabService("https://gitlab.example.com", "source-token", handler);

        var repositories = await service.GetRepositoriesAsync("source", isOrg);

        CollectionAssert.AreEqual(new[] { "public-project", "internal-project" },
            repositories.Select(r => r.Name).ToArray());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task GetRepositoriesAsync_AuthenticatedDiscoveryReadsAllPages(bool isOrg)
    {
        var requestCount = 0;
        var handler = new StubHandler(request =>
        {
            Assert.AreEqual("source-token", request.Headers.GetValues("PRIVATE-TOKEN").Single());
            requestCount++;
            var secondPage = request.RequestUri!.Query.Contains("&page=2", StringComparison.Ordinal);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(secondPage
                    ? """[{"path":"last-project","visibility":"internal"}]"""
                    : Projects, Encoding.UTF8, "application/json")
            };
            response.Headers.Add("X-Next-Page", secondPage ? "" : "2");
            return response;
        });
        var service = new GitLabService("https://gitlab.example.com", "source-token", handler);

        var repositories = await service.GetRepositoriesAsync("source", isOrg);

        Assert.AreEqual(2, requestCount);
        CollectionAssert.AreEqual(new[] { "Video-float", "last-project" },
            repositories.Select(r => r.Name).ToArray());
    }

    [TestMethod]
    public async Task GetRepositoriesAsync_InvalidTokenDoesNotFallBackToAnonymousDiscovery()
    {
        var requestCount = 0;
        var handler = new StubHandler(_ =>
        {
            requestCount++;
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        });
        var service = new GitLabService("https://gitlab.example.com", "invalid-token", handler);

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => service.GetRepositoriesAsync("source", true));

        Assert.AreEqual(1, requestCount);
    }

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

    [TestMethod]
    [DataRow(false, false, 2)]
    [DataRow(true, false, 3)]
    [DataRow(false, true, 3)]
    [DataRow(true, true, 4)]
    public async Task GetRepositoriesAsync_OnlyIncludesPrivateAndArchivedWhenSelected(
        bool includePrivate, bool includeArchived, int expectedCount)
    {
        var handler = new StubHandler(request =>
        {
            Assert.AreEqual(!includeArchived, request.RequestUri!.Query.Contains("archived=false", StringComparison.Ordinal));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    [{"path":"public","visibility":"public"},
                     {"path":"internal","visibility":"internal"},
                     {"path":"private","visibility":"private"},
                     {"path":"archived","visibility":"public","archived":true},
                     {"path":"unknown","visibility":"future-value"}]
                    """, Encoding.UTF8, "application/json")
            };
        });
        var service = new GitLabService("https://gitlab.example.com", "source-token", handler);

        var repositories = await service.GetRepositoriesAsync("source", true, includePrivate, includeArchived);

        Assert.AreEqual(expectedCount, repositories.Count);
        Assert.AreEqual(includePrivate, repositories.Any(repo => repo.Visibility == "private"));
        Assert.AreEqual(includeArchived, repositories.Any(repo => repo.Archived));
        Assert.IsFalse(repositories.Any(repo => repo.Name == "unknown"));
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
