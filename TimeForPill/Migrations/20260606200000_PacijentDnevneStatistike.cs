using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeForPill.Migrations
{
    /// <inheritdoc />
    public partial class PacijentDnevneStatistike : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PacijentDnevneStatistike",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PacijentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Datum = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BrojUzetih = table.Column<int>(type: "int", nullable: false),
                    BrojPropustenih = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PacijentDnevneStatistike", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PacijentDnevneStatistike_Korisnici_PacijentId",
                        column: x => x.PacijentId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PacijentDnevneStatistike_PacijentId_Datum",
                table: "PacijentDnevneStatistike",
                columns: new[] { "PacijentId", "Datum" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PacijentDnevneStatistike");
        }
    }
}
