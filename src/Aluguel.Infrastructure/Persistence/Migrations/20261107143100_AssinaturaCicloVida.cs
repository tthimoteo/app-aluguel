using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aluguel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssinaturaCicloVida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelamento_solicitado_em",
                schema: "app",
                table: "assinatura",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "plano_agendado_id",
                schema: "app",
                table: "assinatura",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_assinatura_cancelamento_solicitado",
                schema: "app",
                table: "assinatura",
                column: "cancelamento_solicitado_em",
                filter: "cancelamento_solicitado_em IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_assinatura_plano_agendado",
                schema: "app",
                table: "assinatura",
                column: "plano_agendado_id",
                filter: "plano_agendado_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_assinatura_planos_plano_agendado_id",
                schema: "app",
                table: "assinatura",
                column: "plano_agendado_id",
                principalSchema: "app",
                principalTable: "plano",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_assinatura_planos_plano_agendado_id",
                schema: "app",
                table: "assinatura");

            migrationBuilder.DropIndex(
                name: "ix_assinatura_cancelamento_solicitado",
                schema: "app",
                table: "assinatura");

            migrationBuilder.DropIndex(
                name: "ix_assinatura_plano_agendado",
                schema: "app",
                table: "assinatura");

            migrationBuilder.DropColumn(
                name: "cancelamento_solicitado_em",
                schema: "app",
                table: "assinatura");

            migrationBuilder.DropColumn(
                name: "plano_agendado_id",
                schema: "app",
                table: "assinatura");
        }
    }
}
