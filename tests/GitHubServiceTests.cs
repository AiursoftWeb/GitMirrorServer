using System.Net;
using System.Text;
using Aiursoft.GitMirrorServer.Services;

namespace Aiursoft.GitMirrorServer.Tests;

[TestClass]
public class GitHubServiceTests
{
    [TestMethod]
    public async Task EnsureRepositoryExistsAsync_Organization_CreatesUnderOrganization()
    {
        var requests = new List<(HttpMethod Method, string Path)>();
        var handler = new StubHandler(request =>
        {
            requests.Add((request.Method, request.RequestUri!.AbsolutePath));
            return new HttpResponseMessage(request.Method == HttpMethod.Get
                ? HttpStatusCode.NotFound
                : HttpStatusCode.Created);
        });
        var service = new GitHubService("https://api.github.com", "test-token", handler);

        await service.EnsureRepositoryExistsAsync("AiursoftWeb", "Example", isOrg: true);

        CollectionAssert.AreEqual(
            new[]
            {
                (HttpMethod.Get, "/repos/AiursoftWeb/Example"),
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
            return request.RequestUri.AbsolutePath == "/user"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"login\":\"Anduin2017\"}", Encoding.UTF8, "application/json")
                }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var service = new GitHubService("https://api.github.com", "test-token", handler);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.EnsureRepositoryExistsAsync("AiursoftWeb", "Example", isOrg: false));

        CollectionAssert.AreEqual(
            new[]
            {
                (HttpMethod.Get, "/repos/AiursoftWeb/Example"),
                (HttpMethod.Get, "/user")
            }, requests);
    }

    [TestMethod]
    public async Task EnsureRepositoryExistsAsync_MatchingPersonalOwner_CreatesUnderUser()
    {
        var requests = new List<(HttpMethod Method, string Path)>();
        var handler = new StubHandler(request =>
        {
            requests.Add((request.Method, request.RequestUri!.AbsolutePath));
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

        await service.EnsureRepositoryExistsAsync("Anduin2017", "Example", isOrg: false);

        CollectionAssert.AreEqual(
            new[]
            {
                (HttpMethod.Get, "/repos/Anduin2017/Example"),
                (HttpMethod.Get, "/user"),
                (HttpMethod.Post, "/user/repos")
            }, requests);
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
