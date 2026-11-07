using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aluguel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanosNfseBoletoLimites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "permite_boleto",
                schema: "app",
                table: "plano",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                schema: "app",
                table: "plano",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "permite_boleto",
                value: false);

            migrationBuilder.UpdateData(
                schema: "app",
                table: "plano",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                columns: new[] { "permite_boleto", "permite_nfse" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "app",
                table: "plano",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "permite_boleto",
                value: false);

            migrationBuilder.UpdateData(
                schema: "app",
                table: "plano",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "permite_boleto",
                value: true);

            migrationBuilder.UpdateData(
                schema: "app",
                table: "plano",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "permite_boleto",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "permite_boleto",
                schema: "app",
                table: "plano");

            migrationBuilder.UpdateData(
                schema: "app",
                table: "plano",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "permite_nfse",
                value: false);
        }
    }
}
