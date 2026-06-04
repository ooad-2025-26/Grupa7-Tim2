using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeForPill.Migrations
{
    /// <inheritdoc />
    public partial class AdminAkcije : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminAkcije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdministratorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AdministratorNaziv = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    VrstaAkcije = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TipRacuna = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RacunId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RacunNaziv = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DatumAkcije = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAkcije", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAkcije");
        }
    }
}
