using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactorGym.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPinAccesoToClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PinAcceso",
                table: "Clientes",
                type: "varchar(4)",
                maxLength: 4,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_PinAcceso",
                table: "Clientes",
                column: "PinAcceso",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clientes_PinAcceso",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "PinAcceso",
                table: "Clientes");
        }
    }
}
