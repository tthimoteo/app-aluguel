using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aluguel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssinaturaPixBancoInter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "observacao",
                schema: "app",
                table: "pagamento_plano",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pix_copia_cola",
                schema: "app",
                table: "pagamento_plano",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pix_end_to_end_id",
                schema: "app",
                table: "pagamento_plano",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pix_location",
                schema: "app",
                table: "pagamento_plano",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pix_qr_code_base64",
                schema: "app",
                table: "pagamento_plano",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pix_tx_id",
                schema: "app",
                table: "pagamento_plano",
                type: "character varying(35)",
                maxLength: 35,
                nullable: true);

            // Plano Trial seed — usado só para backfill de linhas legadas antes da FK.
            var planoTrialId = new Guid("11111111-1111-1111-1111-111111111101");

            migrationBuilder.AddColumn<Guid>(
                name: "plano_destino_id",
                schema: "app",
                table: "pagamento_plano",
                type: "uuid",
                nullable: false,
                defaultValue: planoTrialId);

            migrationBuilder.Sql("""
                UPDATE app.pagamento_plano pp
                SET plano_destino_id = a.plano_id
                FROM app.assinatura a
                WHERE a.id = pp.assinatura_id;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_pagamento_plano_plano_destino_id",
                schema: "app",
                table: "pagamento_plano",
                column: "plano_destino_id");

            migrationBuilder.CreateIndex(
                name: "ux_pagamento_plano_pix_e2e",
                schema: "app",
                table: "pagamento_plano",
                column: "pix_end_to_end_id",
                unique: true,
                filter: "pix_end_to_end_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_pagamento_plano_pix_txid",
                schema: "app",
                table: "pagamento_plano",
                column: "pix_tx_id",
                unique: true,
                filter: "pix_tx_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_pagamento_plano_planos_plano_destino_id",
                schema: "app",
                table: "pagamento_plano",
                column: "plano_destino_id",
                principalSchema: "app",
                principalTable: "plano",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pagamento_plano_planos_plano_destino_id",
                schema: "app",
                table: "pagamento_plano");

            migrationBuilder.DropIndex(
                name: "ix_pagamento_plano_plano_destino_id",
                schema: "app",
                table: "pagamento_plano");

            migrationBuilder.DropIndex(
                name: "ux_pagamento_plano_pix_e2e",
                schema: "app",
                table: "pagamento_plano");

            migrationBuilder.DropIndex(
                name: "ux_pagamento_plano_pix_txid",
                schema: "app",
                table: "pagamento_plano");

            migrationBuilder.DropColumn(
                name: "pix_copia_cola",
                schema: "app",
                table: "pagamento_plano");

            migrationBuilder.DropColumn(
                name: "pix_end_to_end_id",
                schema: "app",
                table: "pagamento_plano");

            migrationBuilder.DropColumn(
                name: "pix_location",
                schema: "app",
                table: "pagamento_plano");

            migrationBuilder.DropColumn(
                name: "pix_qr_code_base64",
                schema: "app",
                table: "pagamento_plano");

            migrationBuilder.DropColumn(
                name: "pix_tx_id",
                schema: "app",
                table: "pagamento_plano");

            migrationBuilder.DropColumn(
                name: "plano_destino_id",
                schema: "app",
                table: "pagamento_plano");

            migrationBuilder.AlterColumn<string>(
                name: "observacao",
                schema: "app",
                table: "pagamento_plano",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
