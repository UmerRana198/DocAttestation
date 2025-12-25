using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocAttestation.Migrations
{
    /// <inheritdoc />
    public partial class AddRegisteredDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: Status, VerificationRemarks, VerifiedAt, VerifiedByUserId columns
            // were already added in the AddDocumentVerificationStatus migration
            
            migrationBuilder.CreateTable(
                name: "RegisteredDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OSVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevocationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastIpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScanCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisteredDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegisteredDevices_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegisteredDevices_DeviceId",
                table: "RegisteredDevices",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_RegisteredDevices_DeviceTokenHash",
                table: "RegisteredDevices",
                column: "DeviceTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_RegisteredDevices_UserId",
                table: "RegisteredDevices",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegisteredDevices");
        }
    }
}
