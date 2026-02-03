using Aiursoft.GitMirrorServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.GitMirrorServer.Tests.IntegrationTests;

[TestClass]
public class MirrorsControllerTests : TestBase
{
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
            { "Mirror.FromType", "GitLab" },
            { "Mirror.FromServer", "https://gitlab.com" },
            { "Mirror.FromOrgName", fromOrg },
            { "Mirror.FromToken", "test-token" },
            { "Mirror.OrgOrUser", "Org" },
            { "Mirror.TargetType", "GitHub" },
            { "Mirror.TargetServer", "https://api.github.com" },
            { "Mirror.TargetOrgName", "TestTargetOrg" },
            { "Mirror.TargetToken", "test-target-token" }
        });
        AssertRedirect(createResponse, "/Mirrors");

        // Verify creation
        var db = GetService<GitMirrorServerDbContext>();
        var mirror = await db.MirrorConfigurations.FirstOrDefaultAsync(m => m.FromOrgName == fromOrg);
        Assert.IsNotNull(mirror);
        Assert.AreEqual("https://gitlab.com", mirror.FromServer);

        // 3. Edit (GET)
        var editPageResponse = await Http.GetAsync($"/Mirrors/Edit/{mirror.Id}");
        editPageResponse.EnsureSuccessStatusCode();

        // 4. Edit (POST)
        var newFromServer = "https://gitlab.example.com";
        var editResponse = await PostForm("/Mirrors/Edit", new Dictionary<string, string>
        {
            { "Mirror.Id", mirror.Id.ToString() }, // Binding to Mirror.Id based on View/Model
            { "Mirror.CreationTime", mirror.CreationTime.ToString("O") },
            { "Mirror.FromType", "GitLab" },
            { "Mirror.FromServer", newFromServer },
            { "Mirror.FromOrgName", fromOrg },
            { "Mirror.FromToken", "test-token" },
            { "Mirror.OrgOrUser", "Org" },
            { "Mirror.TargetType", "GitHub" },
            { "Mirror.TargetServer", "https://api.github.com" },
            { "Mirror.TargetOrgName", "TestTargetOrg" },
            { "Mirror.TargetToken", "test-target-token" }
        });
        AssertRedirect(editResponse, "/Mirrors");

        // Verify edit
        db.ChangeTracker.Clear(); // Clear cache to get fresh data
        var editedMirror = await db.MirrorConfigurations.FindAsync(mirror.Id);
        Assert.IsNotNull(editedMirror);
        Assert.AreEqual(newFromServer, editedMirror.FromServer);

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
    public async Task HistoryAccessTest()
    {
        await LoginAsAdmin();
        var response = await Http.GetAsync("/History/Index");
        response.EnsureSuccessStatusCode();
    }
}
