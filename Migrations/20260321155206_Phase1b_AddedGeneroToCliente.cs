using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactorGym.Web.Migrations
{
    /// <inheritdoc />
    public partial class Phase1b_AddedGeneroToCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Foto",
                table: "Clientes");

            migrationBuilder.AddColumn<string>(
                name: "Genero",
                table: "Clientes",
                type: "varchar(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Genero",
                table: "Clientes");

            migrationBuilder.AddColumn<string>(
                name: "Foto",
                table: "Clientes",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
