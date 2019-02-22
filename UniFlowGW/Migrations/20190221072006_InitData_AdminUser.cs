using Microsoft.EntityFrameworkCore.Migrations;

namespace UniFlowGW.Migrations
{
    public partial class InitData_AdminUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Admins",
                columns: new[] { "AdminId", "Login", "PasswordHash" },
                values: new object[] { 1, "admin", "F4E1B9EB0780D62BDB3B6193829F1721" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 1);
        }
    }
}
