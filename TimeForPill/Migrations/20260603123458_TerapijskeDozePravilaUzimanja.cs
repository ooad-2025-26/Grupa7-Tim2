using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeForPill.Migrations
{
    /// <inheritdoc />
    public partial class TerapijskeDozePravilaUzimanja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IntervalSati",
                table: "Terapije",
                type: "int",
                nullable: false,
                defaultValue: 24);

            migrationBuilder.AddColumn<int>(
                name: "UkupanBrojDoza",
                table: "Terapije",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "TerapijskeDoze",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TerapijaId = table.Column<int>(type: "int", nullable: false),
                    RedniBroj = table.Column<int>(type: "int", nullable: false),
                    VrijemeUzimanja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VrijemePodsjetnika = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VrijemeEvidentiranja = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailPodsjetnikPoslan = table.Column<bool>(type: "bit", nullable: false),
                    KontaktObavijestPoslana = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerapijskeDoze", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerapijskeDoze_Terapije_TerapijaId",
                        column: x => x.TerapijaId,
                        principalTable: "Terapije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TerapijskeDoze_TerapijaId",
                table: "TerapijskeDoze",
                column: "TerapijaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TerapijskeDoze");

            migrationBuilder.DropColumn(
                name: "IntervalSati",
                table: "Terapije");

            migrationBuilder.DropColumn(
                name: "UkupanBrojDoza",
                table: "Terapije");
        }
    }
}
