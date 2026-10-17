using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aluguel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImovelCadastroCobranca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "competencia_inicial",
                schema: "app",
                table: "imovel",
                type: "char(7)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "despesas_condominiais",
                schema: "app",
                table: "imovel",
                type: "numeric(14,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "dia_vencimento_cobranca",
                schema: "app",
                table: "imovel",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "proposito_locacao",
                schema: "app",
                table: "imovel",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_iptu",
                schema: "app",
                table: "imovel",
                type: "numeric(14,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "competencia_inicial",
                schema: "app",
                table: "imovel");

            migrationBuilder.DropColumn(
                name: "despesas_condominiais",
                schema: "app",
                table: "imovel");

            migrationBuilder.DropColumn(
                name: "dia_vencimento_cobranca",
                schema: "app",
                table: "imovel");

            migrationBuilder.DropColumn(
                name: "proposito_locacao",
                schema: "app",
                table: "imovel");

            migrationBuilder.DropColumn(
                name: "valor_iptu",
                schema: "app",
                table: "imovel");
        }
    }
}
