using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Control_flota.Migrations
{
    /// <inheritdoc />
    public partial class AddFechaPagoToFactura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPago",
                table: "Facturas",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaPago",
                table: "Facturas");
        }
    }
}
