using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalWalletInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PinFielToUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Pin",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pin",
                table: "AspNetUsers");
        }
    }
}
