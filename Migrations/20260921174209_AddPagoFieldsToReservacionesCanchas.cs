using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactorGym.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPagoFieldsToReservacionesCanchas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstadoPago",
                table: "ReservacionesCanchas",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPago",
                table: "ReservacionesCanchas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "ReservacionesCanchas",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NumeroRecibo",
                table: "ReservacionesCanchas",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstadoPago",
                table: "ReservacionesCanchas");

            migrationBuilder.DropColumn(
                name: "FechaPago",
                table: "ReservacionesCanchas");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "ReservacionesCanchas");

            migrationBuilder.DropColumn(
                name: "NumeroRecibo",
                table: "ReservacionesCanchas");
        }
    }
}
