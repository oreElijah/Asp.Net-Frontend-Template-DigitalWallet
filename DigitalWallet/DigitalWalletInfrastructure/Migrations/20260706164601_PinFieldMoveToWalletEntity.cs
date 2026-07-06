using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalWalletInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PinFieldMoveToWalletEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pin",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "Pin",
                table: "Wallet",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pin",
                table: "Wallet");

            migrationBuilder.AddColumn<int>(
                name: "Pin",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);
        }
    }
}
