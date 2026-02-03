using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.GitMirrorServer.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MirrorJobExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SuccessCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailureCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MirrorJobExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MirrorRepoExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobExecutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromOrg = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RepoName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TargetOrg = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    Log = table.Column<string>(type: "TEXT", maxLength: 100000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MirrorRepoExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MirrorRepoExecutions_MirrorJobExecutions_JobExecutionId",
                        column: x => x.JobExecutionId,
                        principalTable: "MirrorJobExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MirrorRepoExecutions_JobExecutionId",
                table: "MirrorRepoExecutions",
                column: "JobExecutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MirrorRepoExecutions");

            migrationBuilder.DropTable(
                name: "MirrorJobExecutions");
        }
    }
}
