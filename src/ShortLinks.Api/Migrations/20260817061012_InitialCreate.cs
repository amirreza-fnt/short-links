using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortLinks.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LinkGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    UtmParamsJson = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShortLinks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    GroupId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ClickCount = table.Column<long>(type: "bigint", nullable: false),
                    LastRedirectAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShortLinks_LinkGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "LinkGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ClickStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShortLinkId = table.Column<long>(type: "bigint", nullable: false),
                    ClickedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Browser = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Referrer = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    UtmTemplate = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    QueryString = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClickStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClickStats_ShortLinks_ShortLinkId",
                        column: x => x.ShortLinkId,
                        principalTable: "ShortLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClickStats_ShortLinkId_ClickedAt",
                table: "ClickStats",
                columns: new[] { "ShortLinkId", "ClickedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LinkGroups_Name",
                table: "LinkGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortLinks_Code",
                table: "ShortLinks",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortLinks_CreatedAt",
                table: "ShortLinks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShortLinks_ExpiresAt",
                table: "ShortLinks",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShortLinks_GroupId",
                table: "ShortLinks",
                column: "GroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClickStats");

            migrationBuilder.DropTable(
                name: "ShortLinks");

            migrationBuilder.DropTable(
                name: "LinkGroups");
        }
    }
}
