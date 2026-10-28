using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aluguel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntegracaoBoletoBancario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "id_externo",
                schema: "app",
                table: "boleto",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provedor_codigo",
                schema: "app",
                table: "boleto",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "integracao_boleto_cliente",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provedor_codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    configuracao_publica_json = table.Column<string>(type: "jsonb", nullable: false),
                    segredos_cifrados = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integracao_boleto_cliente", x => x.id);
                    table.ForeignKey(
                        name: "fk_integracao_boleto_cliente_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "app",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_integracao_boleto_cliente_provedor_codigo",
                schema: "app",
                table: "integracao_boleto_cliente",
                column: "provedor_codigo");

            migrationBuilder.CreateIndex(
                name: "ux_integracao_boleto_cliente_ativo",
                schema: "app",
                table: "integracao_boleto_cliente",
                column: "cliente_id",
                unique: true,
                filter: "ativo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integracao_boleto_cliente",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "id_externo",
                schema: "app",
                table: "boleto");

            migrationBuilder.DropColumn(
                name: "provedor_codigo",
                schema: "app",
                table: "boleto");
        }
    }
}
