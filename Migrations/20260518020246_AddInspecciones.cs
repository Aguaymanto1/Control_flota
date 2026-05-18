using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Control_flota.Migrations
{
    /// <inheritdoc />
    public partial class AddInspecciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inspecciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Placa = table.Column<string>(type: "TEXT", nullable: false),
                    Luces = table.Column<string>(type: "TEXT", nullable: false),
                    Llantas = table.Column<string>(type: "TEXT", nullable: false),
                    Frenos = table.Column<string>(type: "TEXT", nullable: false),
                    Fluidos = table.Column<string>(type: "TEXT", nullable: false),
                    EstadoVehiculo = table.Column<string>(type: "TEXT", nullable: false),
                    FechaInspeccion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspecciones", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inspecciones");
        }
    }
}
