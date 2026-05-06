using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeForPill.Data.Migrations
{
    /// <inheritdoc />
    public partial class FinalFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifikacija_Terapija_TerapijaId",
                table: "Notifikacija");

            migrationBuilder.DropForeignKey(
                name: "FK_Pacijent_KontaktOsoba_KontaktOsobaId",
                table: "Pacijent");

            migrationBuilder.DropForeignKey(
                name: "FK_Pacijent_Ljekar_LjekarId",
                table: "Pacijent");

            migrationBuilder.DropForeignKey(
                name: "FK_Terapija_Lijek_LijekId",
                table: "Terapija");

            migrationBuilder.DropForeignKey(
                name: "FK_Terapija_Pacijent_PacijentId",
                table: "Terapija");

            migrationBuilder.DropForeignKey(
                name: "FK_Zahtjev_Terapija_TerapijaId",
                table: "Zahtjev");

            migrationBuilder.DropTable(
                name: "KontaktOsoba");

            migrationBuilder.DropIndex(
                name: "IX_Zahtjev_TerapijaId",
                table: "Zahtjev");

            migrationBuilder.DropIndex(
                name: "IX_Terapija_LijekId",
                table: "Terapija");

            migrationBuilder.DropIndex(
                name: "IX_Terapija_PacijentId",
                table: "Terapija");

            migrationBuilder.DropIndex(
                name: "IX_Pacijent_KontaktOsobaId",
                table: "Pacijent");

            migrationBuilder.DropIndex(
                name: "IX_Pacijent_LjekarId",
                table: "Pacijent");

            migrationBuilder.DropIndex(
                name: "IX_Notifikacija_TerapijaId",
                table: "Notifikacija");

            migrationBuilder.RenameColumn(
                name: "KontaktOsobaId",
                table: "Pacijent",
                newName: "TerapijaId");

            migrationBuilder.AddColumn<int>(
                name: "NotifikacijaID",
                table: "Terapija",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "datumImenovanja",
                table: "Administrator",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifikacijaID",
                table: "Terapija");

            migrationBuilder.DropColumn(
                name: "datumImenovanja",
                table: "Administrator");

            migrationBuilder.RenameColumn(
                name: "TerapijaId",
                table: "Pacijent",
                newName: "KontaktOsobaId");

            migrationBuilder.CreateTable(
                name: "KontaktOsoba",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrojTelefona = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prezime = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KontaktOsoba", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Zahtjev_TerapijaId",
                table: "Zahtjev",
                column: "TerapijaId");

            migrationBuilder.CreateIndex(
                name: "IX_Terapija_LijekId",
                table: "Terapija",
                column: "LijekId");

            migrationBuilder.CreateIndex(
                name: "IX_Terapija_PacijentId",
                table: "Terapija",
                column: "PacijentId");

            migrationBuilder.CreateIndex(
                name: "IX_Pacijent_KontaktOsobaId",
                table: "Pacijent",
                column: "KontaktOsobaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pacijent_LjekarId",
                table: "Pacijent",
                column: "LjekarId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifikacija_TerapijaId",
                table: "Notifikacija",
                column: "TerapijaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifikacija_Terapija_TerapijaId",
                table: "Notifikacija",
                column: "TerapijaId",
                principalTable: "Terapija",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pacijent_KontaktOsoba_KontaktOsobaId",
                table: "Pacijent",
                column: "KontaktOsobaId",
                principalTable: "KontaktOsoba",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pacijent_Ljekar_LjekarId",
                table: "Pacijent",
                column: "LjekarId",
                principalTable: "Ljekar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Terapija_Lijek_LijekId",
                table: "Terapija",
                column: "LijekId",
                principalTable: "Lijek",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Terapija_Pacijent_PacijentId",
                table: "Terapija",
                column: "PacijentId",
                principalTable: "Pacijent",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Zahtjev_Terapija_TerapijaId",
                table: "Zahtjev",
                column: "TerapijaId",
                principalTable: "Terapija",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
