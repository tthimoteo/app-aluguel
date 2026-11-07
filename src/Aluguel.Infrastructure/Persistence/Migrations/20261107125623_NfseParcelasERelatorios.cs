using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aluguel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NfseParcelasERelatorios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "valor_aluguel",
                schema: "app",
                table: "nota_fiscal_servico",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_condominio",
                schema: "app",
                table: "nota_fiscal_servico",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_iptu",
                schema: "app",
                table: "nota_fiscal_servico",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "valor_aluguel",
                schema: "app",
                table: "nota_fiscal_servico");

            migrationBuilder.DropColumn(
                name: "valor_condominio",
                schema: "app",
                table: "nota_fiscal_servico");

            migrationBuilder.DropColumn(
                name: "valor_iptu",
                schema: "app",
                table: "nota_fiscal_servico");
        }
    }
}
