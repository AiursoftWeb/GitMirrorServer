using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.GitMirrorServer.MySql.Migrations
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
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MirrorJobExecutions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MirrorRepoExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    JobExecutionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FromOrg = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RepoName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetOrg = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Log = table.Column<string>(type: "longtext", maxLength: 100000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "varchar(5000)", maxLength: 5000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
