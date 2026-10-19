using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.GitMirrorServer.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddMirrorPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncludeArchivedRepositories",
                table: "MirrorConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeInternalRepositories",
                table: "MirrorConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludePrivateRepositories",
                table: "MirrorConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludePublicRepositories",
                table: "MirrorConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NewGitHubRepositoriesPrivate",
                table: "MirrorConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Before selection was configurable, GitHub organization sources included private repositories.
            // Preserve existing relationships; newly created ones exclude Private by default.
            migrationBuilder.Sql("""
                UPDATE MirrorConfigurations
                SET IncludePrivateRepositories = 1
                WHERE LOWER(FromType) = 'github' AND LOWER(OrgOrUser) LIKE 'org%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludeArchivedRepositories",
                table: "MirrorConfigurations");

            migrationBuilder.DropColumn(
                name: "IncludeInternalRepositories",
                table: "MirrorConfigurations");

            migrationBuilder.DropColumn(
                name: "IncludePrivateRepositories",
                table: "MirrorConfigurations");

            migrationBuilder.DropColumn(
                name: "IncludePublicRepositories",
                table: "MirrorConfigurations");

            migrationBuilder.DropColumn(
                name: "NewGitHubRepositoriesPrivate",
                table: "MirrorConfigurations");
        }
    }
}
