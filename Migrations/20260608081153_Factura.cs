using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Control_flota.Migrations
{
    /// <inheritdoc />
    public partial class Factura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Facturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrdenId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroFactura = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Cliente = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CodigoOrden = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MontoBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SobrecostosJson = table.Column<string>(type: "text", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RutaPdf = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_SolicitudServicioId",
                table: "Ordenes",
                column: "SolicitudServicioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ordenes_SolicitudesServicio_SolicitudServicioId",
                table: "Ordenes",
                column: "SolicitudServicioId",
                principalTable: "SolicitudesServicio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ordenes_SolicitudesServicio_SolicitudServicioId",
                table: "Ordenes");

            migrationBuilder.DropTable(
                name: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_Ordenes_SolicitudServicioId",
                table: "Ordenes");
        }
    }
}
