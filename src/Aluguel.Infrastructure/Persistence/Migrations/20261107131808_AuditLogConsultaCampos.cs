using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aluguel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditLogConsultaCampos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "cliente_id",
                schema: "app",
                table: "audit_log",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resumo",
                schema: "app",
                table: "audit_log",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "usuario_nome",
                schema: "app",
                table: "audit_log",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_cliente_id_data_hora",
                schema: "app",
                table: "audit_log",
                columns: new[] { "cliente_id", "data_hora" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_data_hora",
                schema: "app",
                table: "audit_log",
                column: "data_hora");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_audit_log_cliente_id_data_hora",
                schema: "app",
                table: "audit_log");

            migrationBuilder.DropIndex(
                name: "ix_audit_log_data_hora",
                schema: "app",
                table: "audit_log");

            migrationBuilder.DropColumn(
                name: "cliente_id",
                schema: "app",
                table: "audit_log");

            migrationBuilder.DropColumn(
                name: "resumo",
                schema: "app",
                table: "audit_log");

            migrationBuilder.DropColumn(
                name: "usuario_nome",
                schema: "app",
                table: "audit_log");
        }
    }
}
