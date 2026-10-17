using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aluguel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BoletoCobranca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "boleto",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    imovel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nfse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    competencia = table.Column<string>(type: "char(7)", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    data_vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    nosso_numero = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    linha_digitavel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    storage_path_pdf = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_boleto", x => x.id);
                    table.ForeignKey(
                        name: "fk_boleto_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "app",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_boleto_imoveis_imovel_id",
                        column: x => x.imovel_id,
                        principalSchema: "app",
                        principalTable: "imovel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_boleto_notas_fiscais_nfse_id",
                        column: x => x.nfse_id,
                        principalSchema: "app",
                        principalTable: "nota_fiscal_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_boleto_cliente_id",
                schema: "app",
                table: "boleto",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_boleto_nfse_id",
                schema: "app",
                table: "boleto",
                column: "nfse_id");

            migrationBuilder.CreateIndex(
                name: "ux_boleto_competencia",
                schema: "app",
                table: "boleto",
                columns: new[] { "imovel_id", "competencia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "boleto",
                schema: "app");
        }
    }
}
