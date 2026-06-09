using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Control_flota.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEntregaEpp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntregasEpp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConductorId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaEntrega = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EntregoCasco = table.Column<bool>(type: "INTEGER", nullable: false),
                    EntregoBotas = table.Column<bool>(type: "INTEGER", nullable: false),
                    EntregoChaleco = table.Column<bool>(type: "INTEGER", nullable: false),
                    FirmaDigitalBase64 = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregasEpp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregasEpp_Conductores_ConductorId",
                        column: x => x.ConductorId,
                        principalTable: "Conductores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntregasEpp_ConductorId",
                table: "EntregasEpp",
                column: "ConductorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntregasEpp");
        }
    }
}
