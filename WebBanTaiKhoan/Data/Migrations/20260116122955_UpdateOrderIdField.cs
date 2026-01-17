using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanTaiKhoan.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderIdField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "AccountItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountItems_OrderId",
                table: "AccountItems",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountItems_Orders_OrderId",
                table: "AccountItems",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountItems_Orders_OrderId",
                table: "AccountItems");

            migrationBuilder.DropIndex(
                name: "IX_AccountItems_OrderId",
                table: "AccountItems");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "AccountItems");
        }
    }
}
