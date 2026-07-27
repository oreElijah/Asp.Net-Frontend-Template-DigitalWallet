using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalWalletInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTransactionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReceiverBalanceAfter",
                table: "Transaction",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReceiverBalanceBefore",
                table: "Transaction",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SenderBalanceAfter",
                table: "Transaction",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SenderBalanceBefore",
                table: "Transaction",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiverBalanceAfter",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "ReceiverBalanceBefore",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "SenderBalanceAfter",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "SenderBalanceBefore",
                table: "Transaction");
        }
    }
}
