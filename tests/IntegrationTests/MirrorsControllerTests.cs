using System.Net;
using Aiursoft.GitMirrorServer.Entities;
using Aiursoft.GitMirrorServer.Services;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.GitMirrorServer.Tests.IntegrationTests;

[TestClass]
public class MirrorsControllerTests : TestBase
{
    [TestMethod]
    public async Task FailedHistorySave_DoesNotPreventRecordingNextRepositoryOrJobCompletion()
    {
        using var scope = Server!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GitMirrorServerDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<MirrorService>();
        var job = new MirrorJobExecution { Id = Guid.NewGuid(), FailureCount = 1, SuccessCount = 1 };
        db.MirrorJobExecutions.Add(job);
        await db.SaveChangesAsync();
        var failed = new MirrorRepoExecution
        {
            JobExecutionId = job.Id, FromOrg = "source", TargetOrg = "target", RepoName = "blocked"
        };
        EventHandler<SavingChangesEventArgs> failSave = (_, _) =>
            throw new DbUpdateException(new string('x', 10000));
        db.SavingChanges += failSave;
        try
        {
            await service.RecordRepositoryExecutionAsync(job, failed, new string('x', 120000));
        }
        finally
        {
            db.SavingChanges -= failSave;
        }
        Assert.AreEqual(EntityState.Detached, db.Entry(failed).State);
        Assert.IsFalse(job.IsSuccess);
        Assert.AreEqual(2000, job.ErrorMessage!.Length);

        var next = new MirrorRepoExecution
        {
            JobExecutionId = job.Id, FromOrg = "source", TargetOrg = "target",
            RepoName = "next", IsSuccess = true
        };
        await service.RecordRepositoryExecutionAsync(job, next, "Mirrored successfully");
        job.EndTime = DateTime.UtcNow;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.IsNull(await db.MirrorRepoExecutions.FindAsync(failed.Id));
        Assert.IsTrue((await db.MirrorRepoExecutions.FindAsync(next.Id))!.IsSuccess);
        var savedJob = await db.MirrorJobExecutions.FindAsync(job.Id);
        Assert.IsNotNull(savedJob!.EndTime);
        Assert.IsFalse(savedJob.IsSuccess);
    }

    [TestMethod]
    public async Task MirrorsWorkflowTest()
    {
        await LoginAsAdmin();

        // 1. Index
        var indexResponse = await Http.GetAsync("/Mirrors/Index");
        indexResponse.EnsureSuccessStatusCode();
        var indexHtml = await indexResponse.Content.ReadAsStringAsync();
        Assert.Contains("Mirrors", indexHtml);

        // 2. Create
        var fromOrg = "TestFromOrg-" + Guid.NewGuid();
        var createResponse = await PostForm("/Mirrors/Create", new Dictionary<string, string>
        {
            { "FromType", "GitLab" },
            { "FromServer", "https://gitlab.com" },
            { "FromOrgName", fromOrg },
            { "FromToken", "test-token" },
            { "OrgOrUser", "Org" },
            { "TargetType", "GitHub" },
            { "TargetServer", "https://api.github.com" },
            { "TargetOrgName", "TestTargetOrg" },
            { "TargetToken", "test-target-token" }
        });
        AssertRedirect(createResponse, "/Mirrors");

        // Verify creation
        var db = GetService<GitMirrorServerDbContext>();
        var mirror = await db.MirrorConfigurations.FirstOrDefaultAsync(m => m.FromOrgName == fromOrg);
        Assert.IsNotNull(mirror);
        Assert.AreEqual("https://gitlab.com", mirror.FromServer);
        Assert.IsTrue(mirror.IncludePublicRepositories);
        Assert.IsTrue(mirror.IncludeInternalRepositories);
        Assert.IsFalse(mirror.IncludePrivateRepositories);
        Assert.IsFalse(mirror.IncludeArchivedRepositories);
        Assert.IsFalse(mirror.NewGitHubRepositoriesPrivate);

        // 3. Edit (GET)
        var editPageResponse = await Http.GetAsync($"/Mirrors/Edit/{mirror.Id}");
        editPageResponse.EnsureSuccessStatusCode();

        // 4. Edit (POST)
        var newFromServer = "https://gitlab.example.com";
        var editResponse = await PostForm("/Mirrors/Edit", new Dictionary<string, string>
        {
            { "Id", mirror.Id.ToString() }, 
            { "FromType", "GitLab" },
            { "FromServer", newFromServer },
            { "IncludePublicRepositories", "false" },
            { "IncludeInternalRepositories", "false" },
            { "IncludePrivateRepositories", "true" },
            { "IncludeArchivedRepositories", "true" },
            { "NewGitHubRepositoriesPrivate", "true" },
            { "FromOrgName", fromOrg },
            { "FromToken", "test-token" },
            { "OrgOrUser", "Org" },
            { "TargetType", "GitHub" },
            { "TargetServer", "https://api.github.com" },
            { "TargetOrgName", "TestTargetOrg" },
            { "TargetToken", "test-target-token" }
        });
        AssertRedirect(editResponse, "/Mirrors");

        // Verify edit
        db.ChangeTracker.Clear(); // Clear cache to get fresh data
        var editedMirror = await db.MirrorConfigurations.FindAsync(mirror.Id);
        Assert.IsNotNull(editedMirror);
        Assert.AreEqual(newFromServer, editedMirror.FromServer);
        Assert.IsFalse(editedMirror.IncludePublicRepositories);
        Assert.IsFalse(editedMirror.IncludeInternalRepositories);
        Assert.IsTrue(editedMirror.IncludePrivateRepositories);
        Assert.IsTrue(editedMirror.IncludeArchivedRepositories);
        Assert.IsTrue(editedMirror.NewGitHubRepositoriesPrivate);
        var editedHtml = await (await Http.GetAsync($"/Mirrors/Edit/{mirror.Id}")).Content.ReadAsStringAsync();
        Assert.Contains("Private: access is controlled by the target account", editedHtml);

        // 5. Trigger
        var triggerResponse = await PostForm("/Mirrors/Trigger", new Dictionary<string, string>());
        AssertRedirect(triggerResponse, "/History");
        // We can't easily verify background job ran without waiting, but redirect is enough.

        // 6. Delete
        var deleteResponse = await PostForm($"/Mirrors/Delete/{mirror.Id}", new Dictionary<string, string>());
        AssertRedirect(deleteResponse, "/Mirrors");

        // Verify delete
        db.ChangeTracker.Clear();
        var deletedMirror = await db.MirrorConfigurations.FindAsync(mirror.Id);
        Assert.IsNull(deletedMirror);
    }
    
    [TestMethod]
    [DataRow(false, false, false)]
    [DataRow(false, true, true)]
    [DataRow(true, false, true)]
    public async Task PrivatePublicationRequiresConfirmationOrPrivateTarget(
        bool targetPrivate, bool confirmPublication, bool accepted)
    {
        await LoginAsAdmin();
        var fromOrg = "policy-" + Guid.NewGuid();
        var response = await PostForm("/Mirrors/Create", new Dictionary<string, string>
        {
            ["FromType"] = "GitLab", ["FromServer"] = "https://gitlab.example.com",
            ["FromOrgName"] = fromOrg, ["FromToken"] = "source-token", ["OrgOrUser"] = "Org",
            ["TargetType"] = "GitHub", ["TargetServer"] = "https://api.github.com",
            ["TargetOrgName"] = "target", ["TargetToken"] = "target-token",
            ["IncludePrivateRepositories"] = "true",
            ["NewGitHubRepositoriesPrivate"] = targetPrivate.ToString(),
            ["ConfirmPrivatePublication"] = confirmPublication.ToString()
        });
        Assert.AreEqual(accepted ? HttpStatusCode.Found : HttpStatusCode.OK, response.StatusCode);
        using var scope = Server!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GitMirrorServerDbContext>();
        Assert.AreEqual(accepted, await db.MirrorConfigurations.AnyAsync(m => m.FromOrgName == fromOrg));
        if (accepted)
        {
            var mirror = await db.MirrorConfigurations.SingleAsync(m => m.FromOrgName == fromOrg);
            var editResponse = await PostForm("/Mirrors/Edit", new Dictionary<string, string>
            {
                ["Id"] = mirror.Id.ToString(), ["FromType"] = "GitLab",
                ["FromServer"] = mirror.FromServer, ["FromOrgName"] = fromOrg, ["FromToken"] = "source-token",
                ["OrgOrUser"] = "Org", ["TargetType"] = "GitHub", ["TargetServer"] = mirror.TargetServer,
                ["TargetOrgName"] = "target", ["TargetToken"] = "target-token",
                ["IncludePrivateRepositories"] = "true", ["NewGitHubRepositoriesPrivate"] = "false"
            });
            Assert.AreEqual(HttpStatusCode.OK, editResponse.StatusCode, "Editing cannot bypass publication confirmation.");
            db.ChangeTracker.Clear();
            Assert.AreEqual(targetPrivate, (await db.MirrorConfigurations.FindAsync(mirror.Id))!.NewGitHubRepositoriesPrivate);
        }
        else
        {
            Assert.Contains("Confirm Public publication", await response.Content.ReadAsStringAsync());
        }
    }

    [TestMethod]
    [DataRow(false, false, false, false)]
    [DataRow(true, true, false, false)]
    [DataRow(true, false, false, true)]
    public async Task SourcePolicyValidatesSelectionAndAuthentication(
        bool includePublic, bool includeInternal, bool includePrivate, bool accepted)
    {
        await LoginAsAdmin();
        var fromOrg = "selection-" + Guid.NewGuid();
        var response = await PostForm("/Mirrors/Create", new Dictionary<string, string>
        {
            ["FromType"] = "GitLab", ["FromServer"] = "https://gitlab.example.com",
            ["FromOrgName"] = fromOrg, ["OrgOrUser"] = "Org",
            ["TargetType"] = "GitHub", ["TargetServer"] = "https://api.github.com",
            ["TargetOrgName"] = "target", ["TargetToken"] = "target-token",
            ["IncludePublicRepositories"] = includePublic.ToString(),
            ["IncludeInternalRepositories"] = includeInternal.ToString(),
            ["IncludePrivateRepositories"] = includePrivate.ToString()
        });
        Assert.AreEqual(accepted ? HttpStatusCode.Found : HttpStatusCode.OK, response.StatusCode);
        using var scope = Server!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GitMirrorServerDbContext>();
        Assert.AreEqual(accepted, await db.MirrorConfigurations.AnyAsync(m => m.FromOrgName == fromOrg));
    }

    [TestMethod]
    public async Task HistoryAccessTest()
    {
        await LoginAsAdmin();
        var response = await Http.GetAsync("/History/Index");
        response.EnsureSuccessStatusCode();
    }
}
