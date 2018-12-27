using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UniFlowGW.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    AdminId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Login = table.Column<string>(nullable: true),
                    PasswordHash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.AdminId);
                });

            migrationBuilder.CreateTable(
                name: "BindUsers",
                columns: table => new
                {
                    BindUserId = table.Column<string>(nullable: false),
                    UserLogin = table.Column<string>(nullable: false),
                    BindTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BindUsers", x => x.BindUserId);
                });

            migrationBuilder.CreateTable(
                name: "PrintTasks",
                columns: table => new
                {
                    PrintTaskId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Document = table.Column<string>(nullable: true),
                    UserID = table.Column<string>(nullable: true),
                    Detail = table.Column<string>(nullable: true),
                    Time = table.Column<DateTime>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    QueuedTask = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintTasks", x => x.PrintTaskId);
                });

            migrationBuilder.CreateTable(
                name: "ExternBindings",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExternalId = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: false),
                    BindUserId = table.Column<string>(nullable: false),
                    BindTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternBindings_BindUsers_BindUserId",
                        column: x => x.BindUserId,
                        principalTable: "BindUsers",
                        principalColumn: "BindUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternBindings_BindUserId",
                table: "ExternBindings",
                column: "BindUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternBindings_Type_ExternalId",
                table: "ExternBindings",
                columns: new[] { "Type", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrintTasks_Time",
                table: "PrintTasks",
                column: "Time");

            migrationBuilder.CreateIndex(
                name: "IX_PrintTasks_UserID",
                table: "PrintTasks",
                column: "UserID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "ExternBindings");

            migrationBuilder.DropTable(
                name: "PrintTasks");

            migrationBuilder.DropTable(
                name: "BindUsers");
        }
    }
}
