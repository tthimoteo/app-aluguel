using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aluguel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UsuarioMultiCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_cliente_id_cpf",
                schema: "identity",
                table: "asp_net_users");

            migrationBuilder.AddColumn<Guid>(
                name: "cliente_id",
                schema: "identity",
                table: "refresh_token",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "usuario_cliente",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    perfil = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_cliente", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuario_cliente_asp_net_users_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "identity",
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_usuario_cliente_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "app",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_cliente_id",
                schema: "identity",
                table: "asp_net_users",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_tenant_id_cpf",
                schema: "identity",
                table: "asp_net_users",
                columns: new[] { "tenant_id", "cpf" },
                unique: true,
                filter: "cpf IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_usuario_cliente_cliente_id_status",
                schema: "app",
                table: "usuario_cliente",
                columns: new[] { "cliente_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_usuario_cliente_cliente_id_usuario_id",
                schema: "app",
                table: "usuario_cliente",
                columns: new[] { "cliente_id", "usuario_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuario_cliente_usuario_id",
                schema: "app",
                table: "usuario_cliente",
                column: "usuario_id");

            migrationBuilder.Sql(
                """
                INSERT INTO app.usuario_cliente (id, tenant_id, usuario_id, cliente_id, perfil, status, created_at, updated_at)
                SELECT gen_random_uuid(), tenant_id, id, cliente_id, perfil, COALESCE(status, 'Ativo'), now(), now()
                FROM identity.asp_net_users
                WHERE cliente_id IS NOT NULL
                  AND perfil IN ('Gestor', 'Analista');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuario_cliente",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_cliente_id",
                schema: "identity",
                table: "asp_net_users");

            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_tenant_id_cpf",
                schema: "identity",
                table: "asp_net_users");

            migrationBuilder.DropColumn(
                name: "cliente_id",
                schema: "identity",
                table: "refresh_token");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_cliente_id_cpf",
                schema: "identity",
                table: "asp_net_users",
                columns: new[] { "cliente_id", "cpf" },
                unique: true,
                filter: "cpf IS NOT NULL");
        }
    }
}
