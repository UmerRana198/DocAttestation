using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocAttestation.Migrations
{
    /// <inheritdoc />
    public partial class AddFrontBackDocumentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackDocumentHash",
                table: "ApplicationDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackDocumentPath",
                table: "ApplicationDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrontDocumentHash",
                table: "ApplicationDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrontDocumentPath",
                table: "ApplicationDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SingleDocumentHash",
                table: "ApplicationDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SingleDocumentPath",
                table: "ApplicationDocuments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackDocumentHash",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "BackDocumentPath",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "FrontDocumentHash",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "FrontDocumentPath",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "SingleDocumentHash",
                table: "ApplicationDocuments");

            migrationBuilder.DropColumn(
                name: "SingleDocumentPath",
                table: "ApplicationDocuments");
        }
    }
}
