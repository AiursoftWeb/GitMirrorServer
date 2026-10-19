using System.Net;
using System.Text;
using Aiursoft.GitMirrorServer.Services;

namespace Aiursoft.GitMirrorServer.Tests;

[TestClass]
public class GitHubServiceTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task EnsureRepositoryExistsAsync_Organization_CreatesUnderOrganization(bool isOrg)
    {
        var requests = new List<(HttpMethod Method, string Path)>();
        var handler = new StubHandler(request =>
        {
            requests.Add((request.Method, request.RequestUri!.AbsolutePath));
            if (request.RequestUri.AbsolutePath == "/users/AiursoftWeb")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"type\":\"Organization\"}", Encoding.UTF8, "application/json")
                };
            return new HttpResponseMessage(request.Method == HttpMethod.Get
                ? HttpStatusCode.NotFound
                : HttpStatusCode.Created);
        });
        var service = new GitHubService("https://api.github.com", "test-token", handler);

        await service.EnsureRepositoryExistsAsync("AiursoftWeb", "Example", isOrg);

        CollectionAssert.AreEqual(
            new[]
            {
                (HttpMethod.Get, "/repos/AiursoftWeb/Example"),
                (HttpMethod.Get, "/users/AiursoftWeb"),
                (HttpMethod.Post, "/orgs/AiursoftWeb/repos")
            }, requests);
    }

    [TestMethod]
    public async Task EnsureRepositoryExistsAsync_DifferentPersonalOwner_RejectsBeforeCreation()
    {
        var requests = new List<(HttpMethod Method, string Path)>();
        var handler = new StubHandler(request =>
        {
            requests.Add((request.Method, request.RequestUri!.AbsolutePath));
            if (request.RequestUri.AbsolutePath == "/users/OtherUser")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"type\":\"User\"}", Encoding.UTF8, "application/json")
                };
            return request.RequestUri.AbsolutePath == "/user"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"login\":\"Anduin2017\"}", Encoding.UTF8, "application/json")
                }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var service = new GitHubService("https://api.github.com", "test-token", handler);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.EnsureRepositoryExistsAsync("OtherUser", "Example", isOrg: true));

        CollectionAssert.AreEqual(
            new[]
            {
                (HttpMethod.Get, "/repos/OtherUser/Example"),
                (HttpMethod.Get, "/users/OtherUser"),
                (HttpMethod.Get, "/user")
            }, requests);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task EnsureRepositoryExistsAsync_MatchingPersonalOwner_CreatesUnderUser(bool isOrg)
    {
        var requests = new List<(HttpMethod Method, string Path)>();
        var handler = new StubHandler(request =>
        {
            requests.Add((request.Method, request.RequestUri!.AbsolutePath));
            if (request.RequestUri.AbsolutePath == "/users/Anduin2017")
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"type\":\"User\"}", Encoding.UTF8, "application/json")
                };
            return request.RequestUri.AbsolutePath == "/user"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"login\":\"Anduin2017\"}", Encoding.UTF8, "application/json")
                }
                : new HttpResponseMessage(request.Method == HttpMethod.Get
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.Created);
        });
        var service = new GitHubService("https://api.github.com", "test-token", handler);

        await service.EnsureRepositoryExistsAsync("Anduin2017", "Example", isOrg);

        CollectionAssert.AreEqual(
            new[]
            {
                (HttpMethod.Get, "/repos/Anduin2017/Example"),
                (HttpMethod.Get, "/users/Anduin2017"),
                (HttpMethod.Get, "/user"),
                (HttpMethod.Post, "/user/repos")
            }, requests);
    }

    [TestMethod]
    public async Task EnsureRepositoryExistsAsync_ExistingRepository_DoesNotCreateOrLookUpOwner()
    {
        var requests = new List<(HttpMethod Method, string Path)>();
        var handler = new StubHandler(request =>
        {
            requests.Add((request.Method, request.RequestUri!.AbsolutePath));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = new GitHubService("https://api.github.com", "test-token", handler);

        await service.EnsureRepositoryExistsAsync("Anduin2017", "Video-float", isOrg: true);

        CollectionAssert.AreEqual(
            new[] { (HttpMethod.Get, "/repos/Anduin2017/Video-float") }, requests);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, true)]
    public async Task EnsureRepositoryExistsAsync_CreatesWithSelectedVisibility(bool isPrivate, bool isOrg)
    {
        string? body = null;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.Created);
            }
            var json = request.RequestUri!.AbsolutePath switch
            {
                "/users/target" => isOrg ? "{\"type\":\"Organization\"}" : "{\"type\":\"User\"}",
                "/user" => "{\"login\":\"target\"}",
                _ => null
            };
            return new HttpResponseMessage(json == null ? HttpStatusCode.NotFound : HttpStatusCode.OK)
            {
                Content = json == null ? null : new StringContent(json)
            };
        });
        var service = new GitHubService("https://api.github.com", "target-token", handler);

        await service.EnsureRepositoryExistsAsync("target", "Example", isOrg, isPrivate);

        using var document = System.Text.Json.JsonDocument.Parse(body!);
        Assert.AreEqual(isPrivate, document.RootElement.GetProperty("private").GetBoolean());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task EnsureRepositoryExistsAsync_PrivateSourceRefusesExistingPublicTarget(bool targetPrivate)
    {
        var requests = 0;
        var handler = new StubHandler(_ =>
        {
            requests++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(targetPrivate ? "{\"private\":true}" : "{\"private\":false}")
            };
        });
        var service = new GitHubService("https://api.github.com", "target-token", handler);
        if (targetPrivate)
        {
            await service.EnsureRepositoryExistsAsync("target", "Example", true, isPrivate: true, requirePrivate: true);
        }
        else
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                service.EnsureRepositoryExistsAsync("target", "Example", true, isPrivate: true, requirePrivate: true));
        }
        Assert.AreEqual(1, requests, "Existing target permissions must never be changed automatically.");
    }

    [TestMethod]
    [DataRow(false, false, 1)]
    [DataRow(true, false, 2)]
    [DataRow(false, true, 2)]
    [DataRow(true, true, 3)]
    public async Task GetRepositoriesAsync_RespectsPrivateAndArchivedSelection(
        bool includePrivate, bool includeArchived, int expectedCount)
    {
        var requests = new List<string>();
        var handler = new StubHandler(request =>
        {
            requests.Add(request.RequestUri!.PathAndQuery);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(request.RequestUri.AbsolutePath == "/user"
                    ? "{\"login\":\"source\"}"
                    : """[{"name":"public","private":false},{"name":"private","private":true},{"name":"archived","private":false,"archived":true}]""")
            };
        });
        var service = new GitHubService("https://api.github.com", "source-token", handler);

        var repositories = await service.GetRepositoriesAsync("source", false, includePrivate, includeArchived);

        Assert.AreEqual(expectedCount, repositories.Count);
        Assert.AreEqual(includePrivate, repositories.Any(repo => repo.Visibility == "private"));
        Assert.AreEqual(includeArchived, repositories.Any(repo => repo.Archived));
        CollectionAssert.AreEqual(includePrivate
            ? new[] { "/user", "/user/repos?per_page=100&affiliation=owner" }
            : new[] { "/users/source/repos?per_page=100" }, requests);
    }

    [TestMethod]
    public async Task GetRepositoriesAsync_ReadsBeyondFirstHundredProjects()
    {
        var requests = new List<string>();
        var handler = new StubHandler(request =>
        {
            requests.Add(request.RequestUri!.PathAndQuery);
            var projects = request.RequestUri.Query.Contains("&page=2", StringComparison.Ordinal)
                ? new[] { new { name = "last", @private = false } }
                : Enumerable.Range(0, 100).Select(i => new { name = $"repo-{i}", @private = false }).ToArray();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(projects))
            };
        });
        var service = new GitHubService("https://api.github.com", "source-token", handler);

        var repositories = await service.GetRepositoriesAsync("source", true);

        Assert.AreEqual(101, repositories.Count);
        Assert.IsTrue(repositories.Any(repo => repo.Name == "last"));
        CollectionAssert.AreEqual(new[] { "/orgs/source/repos?per_page=100", "/orgs/source/repos?per_page=100&page=2" }, requests);
    }

    [TestMethod]
    public async Task GetRepositoriesAsync_PrivatePersonalRepositoriesRequireMatchingSourceUser()
    {
        var requests = 0;
        var service = new GitHubService("https://api.github.com", "source-token", new StubHandler(_ =>
        {
            requests++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"login\":\"another-user\"}")
            };
        }));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.GetRepositoriesAsync("source", false, includePrivate: true));
        Assert.AreEqual(1, requests);
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
