using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aluguel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClienteCodigosFiscaisNfseIbsCbs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "classificacao_tributaria",
                schema: "app",
                table: "cliente",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_indicador_operacao",
                schema: "app",
                table: "cliente",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "classificacao_tributaria",
                schema: "app",
                table: "cliente");

            migrationBuilder.DropColumn(
                name: "codigo_indicador_operacao",
                schema: "app",
                table: "cliente");
        }
    }
}
