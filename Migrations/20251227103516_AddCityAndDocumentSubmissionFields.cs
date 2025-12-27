using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocAttestation.Migrations
{
    /// <inheritdoc />
    public partial class AddCityAndDocumentSubmissionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentSubmissionMethod",
                table: "Applications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelationCNIC",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelationType",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionBy",
                table: "Applications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TCSNumber",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "DocumentSubmissionMethod",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "RelationCNIC",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "RelationType",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SubmissionBy",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TCSNumber",
                table: "Applications");
        }
    }
}
