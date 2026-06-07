using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeForPill.Migrations
{
    /// <inheritdoc />
    public partial class BrojDozaPoObnoviTerapije : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BrojDozaPoObnovi",
                table: "Terapije",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE Terapije SET BrojDozaPoObnovi = CASE WHEN UkupanBrojDoza > 0 THEN UkupanBrojDoza ELSE 1 END WHERE BrojDozaPoObnovi = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrojDozaPoObnovi",
                table: "Terapije");
        }
    }
}
