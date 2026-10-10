using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aluguel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClienteStatusOperacional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Status do cliente passa a ser operacional (Ativo/Inativo/Bloqueado).
            // O ciclo comercial Trial/Ativa/Suspensa permanece em app.assinatura.
            migrationBuilder.Sql("""
                UPDATE app.cliente
                SET status = CASE status
                    WHEN 'Trial' THEN 'Ativo'
                    WHEN 'PendentePagamento' THEN 'Ativo'
                    WHEN 'Ativa' THEN 'Ativo'
                    WHEN 'Suspensa' THEN 'Bloqueado'
                    WHEN 'Cancelada' THEN 'Inativo'
                    WHEN 'Ativo' THEN 'Ativo'
                    WHEN 'Inativo' THEN 'Inativo'
                    WHEN 'Bloqueado' THEN 'Bloqueado'
                    ELSE 'Ativo'
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE app.cliente
                SET status = CASE status
                    WHEN 'Ativo' THEN 'Trial'
                    WHEN 'Bloqueado' THEN 'Suspensa'
                    WHEN 'Inativo' THEN 'Cancelada'
                    ELSE 'Trial'
                END;
                """);
        }
    }
}
