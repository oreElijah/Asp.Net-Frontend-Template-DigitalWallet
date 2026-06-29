using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalWalletInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QrCodeStingFieldForMerchant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QRCodeString",
                table: "Merchant",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QRCodeString",
                table: "Merchant");
        }
    }
}
