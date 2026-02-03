using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aiursoft.GitMirrorServer.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddMirrors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MirrorConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FromServer = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FromToken = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    FromOrgName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OrgOrUser = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TargetType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TargetServer = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    TargetToken = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    TargetOrgName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MirrorConfigurations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MirrorConfigurations");
        }
    }
}
