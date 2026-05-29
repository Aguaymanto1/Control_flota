using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Control_flota.Migrations
{
    /// <inheritdoc />
    public partial class AgregarReceptorEnSolicitud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DniReceptor",
                table: "SolicitudesServicio",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreReceptor",
                table: "SolicitudesServicio",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DniReceptor",
                table: "SolicitudesServicio");

            migrationBuilder.DropColumn(
                name: "NombreReceptor",
                table: "SolicitudesServicio");
        }
    }
}
