using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeForPill.Migrations
{
    /// <inheritdoc />
    public partial class NuspojavePacijenata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nuspojave",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PacijentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TerapijaId = table.Column<int>(type: "int", nullable: true),
                    LijekId = table.Column<int>(type: "int", nullable: true),
                    NazivLijeka = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kategorija = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Slika = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    Opis = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BezNuspojava = table.Column<bool>(type: "bit", nullable: false),
                    DatumPrijave = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nuspojave", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nuspojave_Korisnici_PacijentId",
                        column: x => x.PacijentId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nuspojave_Lijekovi_LijekId",
                        column: x => x.LijekId,
                        principalTable: "Lijekovi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Nuspojave_Terapije_TerapijaId",
                        column: x => x.TerapijaId,
                        principalTable: "Terapije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nuspojave_LijekId",
                table: "Nuspojave",
                column: "LijekId");

            migrationBuilder.CreateIndex(
                name: "IX_Nuspojave_PacijentId",
                table: "Nuspojave",
                column: "PacijentId");

            migrationBuilder.CreateIndex(
                name: "IX_Nuspojave_TerapijaId",
                table: "Nuspojave",
                column: "TerapijaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Nuspojave");
        }
    }
}
