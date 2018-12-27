using Microsoft.EntityFrameworkCore.Migrations;

namespace UniFlowGW.Migrations
{
    public partial class Add_is_binded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBinded",
                table: "BindUsers",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBinded",
                table: "BindUsers");
        }
    }
}
