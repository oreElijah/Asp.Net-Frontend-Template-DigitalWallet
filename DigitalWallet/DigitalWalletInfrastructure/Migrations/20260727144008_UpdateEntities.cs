using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalWalletInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transaction_ReceiverWalletId",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_SenderWalletId",
                table: "Transaction");

            migrationBuilder.RenameColumn(
                name: "Pin",
                table: "Wallet",
                newName: "PinHash");

            migrationBuilder.AlterColumn<decimal>(
                name: "LockedBalance",
                table: "Wallet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "Wallet",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "SenderBalanceBefore",
                table: "Transaction",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "SenderBalanceAfter",
                table: "Transaction",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "ReceiverBalanceBefore",
                table: "Transaction",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "ReceiverBalanceAfter",
                table: "Transaction",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Transaction",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeactivated",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: true),
                    EntityType = table.Column<string>(type: "text", nullable: true),
                    EntityId = table.Column<string>(type: "text", nullable: true),
                    Details = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    TokenHash = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshToken_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wallet_WalletNumber",
                table: "Wallet",
                column: "WalletNumber",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallet_Balance_NonNegative",
                table: "Wallet",
                sql: "\"Balance\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallet_LockedBalance_NonNegative",
                table: "Wallet",
                sql: "\"LockedBalance\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_ReceiverWalletId_CreatedAt",
                table: "Transaction",
                columns: new[] { "ReceiverWalletId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_Reference",
                table: "Transaction",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_SenderWalletId_CreatedAt",
                table: "Transaction",
                columns: new[] { "SenderWalletId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_Status_CreatedAt",
                table: "Transaction",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transaction_Amount_Positive",
                table: "Transaction",
                sql: "\"Amount\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_School_Code",
                table: "School",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_ActorUserId_CreatedAt",
                table: "AuditLog",
                columns: new[] { "ActorUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EntityType_EntityId_CreatedAt",
                table: "AuditLog",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_TokenHash",
                table: "RefreshToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId_ExpiresAt",
                table: "RefreshToken",
                columns: new[] { "UserId", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropIndex(
                name: "IX_Wallet_WalletNumber",
                table: "Wallet");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallet_Balance_NonNegative",
                table: "Wallet");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallet_LockedBalance_NonNegative",
                table: "Wallet");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_ReceiverWalletId_CreatedAt",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_Reference",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_SenderWalletId_CreatedAt",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_Status_CreatedAt",
                table: "Transaction");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transaction_Amount_Positive",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_School_Code",
                table: "School");

            migrationBuilder.DropColumn(
                name: "IsDeactivated",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "PinHash",
                table: "Wallet",
                newName: "Pin");

            migrationBuilder.AlterColumn<decimal>(
                name: "LockedBalance",
                table: "Wallet",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "Wallet",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "SenderBalanceBefore",
                table: "Transaction",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "SenderBalanceAfter",
                table: "Transaction",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "ReceiverBalanceBefore",
                table: "Transaction",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "ReceiverBalanceAfter",
                table: "Transaction",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Transaction",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_ReceiverWalletId",
                table: "Transaction",
                column: "ReceiverWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_SenderWalletId",
                table: "Transaction",
                column: "SenderWalletId");
        }
    }
}
