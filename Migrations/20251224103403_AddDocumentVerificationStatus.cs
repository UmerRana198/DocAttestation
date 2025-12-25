using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocAttestation.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentVerificationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ApplicationDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedByUserId",
                table: "ApplicationDocuments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "ApplicationDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationRemarks",
                table: "ApplicationDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDocuments_VerifiedByUserId",
                table: "ApplicationDocuments",
                column: "VerifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationDocuments_AspNetUsers_VerifiedByUserId",
                table: "ApplicationDocuments",
                column: "VerifiedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationDocuments_AspNetUsers_VerifiedByUserId",
                table: "ApplicationDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationDocuments_VerifiedByUserId",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "VerifiedByUserId",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "VerificationRemarks",
                table: "ApplicationDocuments");
        }
    }
}
