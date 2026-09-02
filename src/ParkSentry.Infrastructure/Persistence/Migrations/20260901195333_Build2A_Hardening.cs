using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkSentry.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Build2A_Hardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WatchlistEntries_OrganizationId_NormalizedRegistration",
                table: "WatchlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OrganizationId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_VehicleId",
                table: "ParkingSessions");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "SystemSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Payments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistEntries_OrganizationId_NormalizedRegistration",
                table: "WatchlistEntries",
                columns: new[] { "OrganizationId", "NormalizedRegistration" },
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrganizationId_IdempotencyKey",
                table: "Payments",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_OrganizationId_EntryTime",
                table: "ParkingSessions",
                columns: new[] { "OrganizationId", "EntryTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_VehicleId",
                table: "ParkingSessions",
                column: "VehicleId",
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Organizations_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Organizations_OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_WatchlistEntries_OrganizationId_NormalizedRegistration",
                table: "WatchlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OrganizationId_IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_OrganizationId_EntryTime",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_VehicleId",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Payments");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "SystemSettings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistEntries_OrganizationId_NormalizedRegistration",
                table: "WatchlistEntries",
                columns: new[] { "OrganizationId", "NormalizedRegistration" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrganizationId",
                table: "Payments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_VehicleId",
                table: "ParkingSessions",
                column: "VehicleId");
        }
    }
}
