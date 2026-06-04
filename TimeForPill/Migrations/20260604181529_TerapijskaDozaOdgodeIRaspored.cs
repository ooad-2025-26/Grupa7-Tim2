using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeForPill.Migrations
{
    /// <inheritdoc />
    public partial class TerapijskaDozaOdgodeIRaspored : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BrojOdgoda",
                table: "TerapijskeDoze",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "OriginalnoVrijemeUzimanja",
                table: "TerapijskeDoze",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE TerapijskeDoze SET OriginalnoVrijemeUzimanja = VrijemeUzimanja WHERE OriginalnoVrijemeUzimanja IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrojOdgoda",
                table: "TerapijskeDoze");

            migrationBuilder.DropColumn(
                name: "OriginalnoVrijemeUzimanja",
                table: "TerapijskeDoze");
        }
    }
}
