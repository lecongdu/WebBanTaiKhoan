using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanTaiKhoan.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreBanners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerUrl2",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerUrl3",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerUrl2",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "BannerUrl3",
                table: "SystemSettings");
        }
    }
}
