using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeForPill.Data.Migrations
{
    /// <inheritdoc />
    public partial class AppValidationAndPersistenceFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Zahtjev] SET [Sadrzaj] = LEFT([Sadrzaj], 1000) WHERE LEN([Sadrzaj]) > 1000");
            migrationBuilder.Sql("UPDATE [Zahtjev] SET [Naziv] = LEFT([Naziv], 100) WHERE LEN([Naziv]) > 100");
            migrationBuilder.Sql("UPDATE [Terapija] SET [Naziv] = LEFT([Naziv], 100) WHERE LEN([Naziv]) > 100");
            migrationBuilder.Sql("UPDATE [Notifikacija] SET [Poruka] = LEFT([Poruka], 500) WHERE LEN([Poruka]) > 500");
            migrationBuilder.Sql("UPDATE [Notifikacija] SET [Naziv] = LEFT([Naziv], 100) WHERE LEN([Naziv]) > 100");
            migrationBuilder.Sql("UPDATE [Lijek] SET [Slika] = LEFT([Slika], 260) WHERE LEN([Slika]) > 260");
            migrationBuilder.Sql("UPDATE [Lijek] SET [Naziv] = LEFT([Naziv], 100) WHERE LEN([Naziv]) > 100");
            migrationBuilder.Sql("UPDATE [Lijek] SET [Kategorija] = LEFT([Kategorija], 80) WHERE LEN([Kategorija]) > 80");
            migrationBuilder.Sql("UPDATE [Korisnik] SET [Prezime] = LEFT([Prezime], 50) WHERE LEN([Prezime]) > 50");
            migrationBuilder.Sql("UPDATE [Korisnik] SET [Lozinka] = LEFT([Lozinka], 100) WHERE LEN([Lozinka]) > 100");
            migrationBuilder.Sql("UPDATE [Korisnik] SET [Ime] = LEFT([Ime], 50) WHERE LEN([Ime]) > 50");
            migrationBuilder.Sql("UPDATE [Korisnik] SET [Email] = LEFT([Email], 120) WHERE LEN([Email]) > 120");

            migrationBuilder.AlterColumn<int>(
                name: "TerapijaId",
                table: "Zahtjev",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Sadrzaj",
                table: "Zahtjev",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Naziv",
                table: "Zahtjev",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "PacijentId",
                table: "Terapija",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "NotifikacijaID",
                table: "Terapija",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Naziv",
                table: "Terapija",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "LijekId",
                table: "Terapija",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "TerapijaId",
                table: "Pacijent",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "LjekarId",
                table: "Pacijent",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "KontaktOsobaId",
                table: "Pacijent",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TerapijaId",
                table: "Notifikacija",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Poruka",
                table: "Notifikacija",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Naziv",
                table: "Notifikacija",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Slika",
                table: "Lijek",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Naziv",
                table: "Lijek",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Kategorija",
                table: "Lijek",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Prezime",
                table: "Korisnik",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Lozinka",
                table: "Korisnik",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Ime",
                table: "Korisnik",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Korisnik",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "KontaktOsoba",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ime = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Prezime = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BrojTelefona = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KontaktOsoba", x => x.Id);
                });

            migrationBuilder.Sql("UPDATE [Zahtjev] SET [TerapijaId] = NULL WHERE [TerapijaId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Terapija] WHERE [Terapija].[Id] = [Zahtjev].[TerapijaId])");
            migrationBuilder.Sql("UPDATE [Terapija] SET [LijekId] = NULL WHERE [LijekId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Lijek] WHERE [Lijek].[Id] = [Terapija].[LijekId])");
            migrationBuilder.Sql("UPDATE [Terapija] SET [PacijentId] = NULL WHERE [PacijentId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Pacijent] WHERE [Pacijent].[Id] = [Terapija].[PacijentId])");
            migrationBuilder.Sql("UPDATE [Pacijent] SET [LjekarId] = NULL WHERE [LjekarId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Ljekar] WHERE [Ljekar].[Id] = [Pacijent].[LjekarId])");
            migrationBuilder.Sql("UPDATE [Notifikacija] SET [TerapijaId] = NULL WHERE [TerapijaId] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Terapija] WHERE [Terapija].[Id] = [Notifikacija].[TerapijaId])");

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
                onDelete: ReferentialAction.SetNull);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "KontaktOsobaId",
                table: "Pacijent");

            migrationBuilder.AlterColumn<int>(
                name: "TerapijaId",
                table: "Zahtjev",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Sadrzaj",
                table: "Zahtjev",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Naziv",
                table: "Zahtjev",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "PacijentId",
                table: "Terapija",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "NotifikacijaID",
                table: "Terapija",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Naziv",
                table: "Terapija",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "LijekId",
                table: "Terapija",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TerapijaId",
                table: "Pacijent",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LjekarId",
                table: "Pacijent",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TerapijaId",
                table: "Notifikacija",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Poruka",
                table: "Notifikacija",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Naziv",
                table: "Notifikacija",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Slika",
                table: "Lijek",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(260)",
                oldMaxLength: 260);

            migrationBuilder.AlterColumn<string>(
                name: "Naziv",
                table: "Lijek",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Kategorija",
                table: "Lijek",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "Prezime",
                table: "Korisnik",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Lozinka",
                table: "Korisnik",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Ime",
                table: "Korisnik",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Korisnik",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);
        }
    }
}
