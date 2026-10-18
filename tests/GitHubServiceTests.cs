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

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(respond(request));
        }
    }
}
