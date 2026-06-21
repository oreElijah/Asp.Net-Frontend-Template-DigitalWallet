using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalWalletInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MerchanAndWalletEntityFieldChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LockedBalance",
                table: "Wallet",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverWalletNumber",
                table: "Transaction",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderWalletNumber",
                table: "Transaction",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "Merchant",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferRecipientCode",
                table: "Merchant",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockedBalance",
                table: "Wallet");

            migrationBuilder.DropColumn(
                name: "ReceiverWalletNumber",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "SenderWalletNumber",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "Merchant");

            migrationBuilder.DropColumn(
                name: "TransferRecipientCode",
                table: "Merchant");
        }
    }
}
