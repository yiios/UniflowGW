using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UniFlowGW.Migrations
{
    public partial class WeChatUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeChatUsers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    openId = table.Column<string>(nullable: true),
                    userId = table.Column<string>(nullable: true),
                    bindDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeChatUsers", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeChatUsers");
        }
    }
}
