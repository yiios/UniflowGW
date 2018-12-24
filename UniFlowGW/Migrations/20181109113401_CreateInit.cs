using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UniFlowGW.Migrations
{
    public partial class CreateInit : Migration
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
                name: "PrintTasks");
        }
    }
}
