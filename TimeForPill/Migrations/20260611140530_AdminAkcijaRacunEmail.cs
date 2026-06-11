using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeForPill.Migrations
{
    /// <inheritdoc />
    public partial class AdminAkcijaRacunEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RacunEmail",
                table: "AdminAkcije",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RacunEmail",
                table: "AdminAkcije");
        }
    }
}
