using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanTaiKhoan.Migrations
{
    /// <inheritdoc />
    public partial class AddWelcomeSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WelcomeBadge",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WelcomeButtonText",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WelcomeSubTitle",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WelcomeTitle",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WelcomeBadge",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "WelcomeButtonText",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "WelcomeSubTitle",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "WelcomeTitle",
                table: "SystemSettings");
        }
    }
}
