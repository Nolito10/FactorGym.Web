using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactorGym.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddInitialAdminSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Apellido", "Email", "FechaRegistro", "Nombre", "PasswordHash", "Rol", "Username" },
                values: new object[] { 1, "Sistema", "admin@factorgym.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Administrador", "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=", "Administrador", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
