using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.src.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfSignatureBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdfSignatureBlocksJson",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PdfSignatureBlock",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PageNumber = table.Column<int>(type: "integer", nullable: false),
                    XPercent = table.Column<double>(type: "double precision", nullable: false),
                    YPercent = table.Column<double>(type: "double precision", nullable: false),
                    WidthPercent = table.Column<double>(type: "double precision", nullable: false),
                    HeightPercent = table.Column<double>(type: "double precision", nullable: false),
                    SignerRole = table.Column<string>(type: "text", nullable: false),
                    IsSigned = table.Column<bool>(type: "boolean", nullable: false),
                    SignatureId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentId = table.Column<string>(type: "character varying(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdfSignatureBlock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PdfSignatureBlock_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PdfSignatureBlock_DocumentId",
                table: "PdfSignatureBlock",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PdfSignatureBlock");

            migrationBuilder.DropColumn(
                name: "PdfSignatureBlocksJson",
                table: "Documents");
        }
    }
}
