using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanTaiKhoan.Data.Migrations
{
    public partial class SyncTopUpModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Xóa cột Date cũ để dọn dẹp Database
            migrationBuilder.DropColumn(
                name: "Date",
                table: "TopUpTransactions");

            // 2. Thêm cột CreatedAt (Dùng cho logic nạp tiền mới)
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TopUpTransactions",
                type: "datetime2",
                nullable: false,
                defaultValue: DateTime.Now);

            // 3. Thêm cột TransactionCode (Lưu mã nạp tiền)
            migrationBuilder.AddColumn<string>(
                name: "TransactionCode",
                table: "TopUpTransactions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionCode",
                table: "TopUpTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TopUpTransactions");

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "TopUpTransactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}