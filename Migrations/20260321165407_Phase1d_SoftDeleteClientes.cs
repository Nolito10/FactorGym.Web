using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactorGym.Web.Migrations
{
    /// <inheritdoc />
    public partial class Phase1d_SoftDeleteClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActivo",
                table: "Clientes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActivo",
                table: "Clientes");
        }
    }
}
