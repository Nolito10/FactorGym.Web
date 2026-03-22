using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactorGym.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClasesCanchasModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReservacionesCanchas_Canchas_CanchaId",
                table: "ReservacionesCanchas");

            migrationBuilder.DropTable(
                name: "Canchas");

            migrationBuilder.DropTable(
                name: "ReservacionesClases");

            migrationBuilder.DropTable(
                name: "ClasesZumba");

            migrationBuilder.DropIndex(
                name: "IX_ReservacionesCanchas_CanchaId",
                table: "ReservacionesCanchas");

            migrationBuilder.DropColumn(
                name: "EstadoListaEspera",
                table: "ReservacionesCanchas");

            migrationBuilder.DropColumn(
                name: "MontoPagado",
                table: "ReservacionesCanchas");

            migrationBuilder.RenameColumn(
                name: "CanchaId",
                table: "ReservacionesCanchas",
                newName: "NumeroPersonas");

            migrationBuilder.AddColumn<string>(
                name: "Notas",
                table: "ReservacionesCanchas",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TipoUso",
                table: "ReservacionesCanchas",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notas",
                table: "ReservacionesCanchas");

            migrationBuilder.DropColumn(
                name: "TipoUso",
                table: "ReservacionesCanchas");

            migrationBuilder.RenameColumn(
                name: "NumeroPersonas",
                table: "ReservacionesCanchas",
                newName: "CanchaId");

            migrationBuilder.AddColumn<bool>(
                name: "EstadoListaEspera",
                table: "ReservacionesCanchas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoPagado",
                table: "ReservacionesCanchas",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Canchas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Capacidad = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrecioHora = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Canchas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClasesZumba",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Capacidad = table.Column<int>(type: "int", nullable: false),
                    Horario = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Instructor = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoPlan = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClasesZumba", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReservacionesClases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClaseZumbaId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaReserva = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservacionesClases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservacionesClases_ClasesZumba_ClaseZumbaId",
                        column: x => x.ClaseZumbaId,
                        principalTable: "ClasesZumba",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservacionesClases_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ReservacionesCanchas_CanchaId",
                table: "ReservacionesCanchas",
                column: "CanchaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservacionesClases_ClaseZumbaId",
                table: "ReservacionesClases",
                column: "ClaseZumbaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservacionesClases_ClienteId",
                table: "ReservacionesClases",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservacionesClases_Fecha_Estado",
                table: "ReservacionesClases",
                columns: new[] { "FechaReserva", "Estado" });

            migrationBuilder.AddForeignKey(
                name: "FK_ReservacionesCanchas_Canchas_CanchaId",
                table: "ReservacionesCanchas",
                column: "CanchaId",
                principalTable: "Canchas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
