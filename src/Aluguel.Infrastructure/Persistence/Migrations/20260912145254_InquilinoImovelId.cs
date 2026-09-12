using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aluguel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InquilinoImovelId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "imovel_id",
                schema: "app",
                table: "inquilino",
                type: "uuid",
                nullable: true);

            // Legado: se o cliente tem exatamente 1 imóvel e 1 inquilino sem vínculo, associa automaticamente.
            migrationBuilder.Sql(
                """
                UPDATE app.inquilino i
                SET imovel_id = sub.imovel_id
                FROM (
                    SELECT c.id AS cliente_id,
                           (SELECT m.id FROM app.imovel m
                            WHERE m.cliente_id = c.id AND m.deleted_at IS NULL
                            ORDER BY m.created_at LIMIT 1) AS imovel_id,
                           (SELECT COUNT(*) FROM app.imovel m
                            WHERE m.cliente_id = c.id AND m.deleted_at IS NULL) AS n_imoveis,
                           (SELECT COUNT(*) FROM app.inquilino iq
                            WHERE iq.cliente_id = c.id AND iq.deleted_at IS NULL AND iq.imovel_id IS NULL) AS n_inq
                    FROM app.cliente c
                ) sub
                WHERE i.cliente_id = sub.cliente_id
                  AND i.imovel_id IS NULL
                  AND i.deleted_at IS NULL
                  AND sub.n_imoveis = 1
                  AND sub.n_inq = 1
                  AND sub.imovel_id IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_inquilino_imovel_id_ativo",
                schema: "app",
                table: "inquilino",
                column: "imovel_id",
                unique: true,
                filter: "imovel_id IS NOT NULL AND deleted_at IS NULL AND status = 'Ativo'");

            migrationBuilder.AddForeignKey(
                name: "fk_inquilino_imovel_imovel_id",
                schema: "app",
                table: "inquilino",
                column: "imovel_id",
                principalSchema: "app",
                principalTable: "imovel",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inquilino_imovel_imovel_id",
                schema: "app",
                table: "inquilino");

            migrationBuilder.DropIndex(
                name: "ix_inquilino_imovel_id_ativo",
                schema: "app",
                table: "inquilino");

            migrationBuilder.DropColumn(
                name: "imovel_id",
                schema: "app",
                table: "inquilino");
        }
    }
}
