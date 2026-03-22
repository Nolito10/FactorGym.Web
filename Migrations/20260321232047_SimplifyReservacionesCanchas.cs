using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactorGym.Web.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyReservacionesCanchas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReservacionesCanchas_Clientes_ClienteId",
                table: "ReservacionesCanchas");

            migrationBuilder.DropIndex(
                name: "IX_ReservacionesCanchas_ClienteId",
                table: "ReservacionesCanchas");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "ReservacionesCanchas");

            migrationBuilder.DropColumn(
                name: "Notas",
                table: "ReservacionesCanchas");

            migrationBuilder.DropColumn(
                name: "NumeroPersonas",
                table: "ReservacionesCanchas");

            migrationBuilder.AddColumn<string>(
                name: "NombreCliente",
                table: "ReservacionesCanchas",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombreCliente",
                table: "ReservacionesCanchas");

            migrationBuilder.AddColumn<int>(
                name: "ClienteId",
                table: "ReservacionesCanchas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Notas",
                table: "ReservacionesCanchas",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "NumeroPersonas",
                table: "ReservacionesCanchas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ReservacionesCanchas_ClienteId",
                table: "ReservacionesCanchas",
                column: "ClienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReservacionesCanchas_Clientes_ClienteId",
                table: "ReservacionesCanchas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
