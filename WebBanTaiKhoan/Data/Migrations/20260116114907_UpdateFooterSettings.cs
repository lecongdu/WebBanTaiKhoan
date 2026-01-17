using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanTaiKhoan.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFooterSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscordUrl",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DmcaUrl",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterAbout",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TiktokUrl",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YoutubeUrl",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordUrl",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DmcaUrl",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "FooterAbout",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TiktokUrl",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "YoutubeUrl",
                table: "SystemSettings");
        }
    }
}
