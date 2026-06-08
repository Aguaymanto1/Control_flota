using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Control_flota.Migrations
{
    /// <inheritdoc />
    public partial class AgregarKilometrajeUnidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KilometrajeActual",
                table: "Unidades",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KilometrajeActual",
                table: "Unidades");
        }
    }
}
