using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Control_flota.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConductorIdAOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConductorId",
                table: "Ordenes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_ConductorId",
                table: "Ordenes",
                column: "ConductorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ordenes_Conductores_ConductorId",
                table: "Ordenes",
                column: "ConductorId",
                principalTable: "Conductores",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ordenes_Conductores_ConductorId",
                table: "Ordenes");

            migrationBuilder.DropIndex(
                name: "IX_Ordenes_ConductorId",
                table: "Ordenes");

            migrationBuilder.DropColumn(
                name: "ConductorId",
                table: "Ordenes");
        }
    }
}
