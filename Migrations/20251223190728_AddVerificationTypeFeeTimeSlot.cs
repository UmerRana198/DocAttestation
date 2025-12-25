using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocAttestation.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationTypeFeeTimeSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Fee",
                table: "Applications",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "TimeSlot",
                table: "Applications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationType",
                table: "Applications",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fee",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TimeSlot",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "VerificationType",
                table: "Applications");
        }
    }
}
