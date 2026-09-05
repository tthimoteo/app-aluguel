using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aluguel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    acao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    tabela = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    registro_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valores_antes = table.Column<string>(type: "jsonb", nullable: true),
                    valores_depois = table.Column<string>(type: "jsonb", nullable: true),
                    data_hora = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cliente",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_pessoa = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    plano_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    data_nascimento = table.Column<DateOnly>(type: "date", nullable: true),
                    razao_social = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    nome_fantasia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    inscricao_municipal = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    cnae_principal = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    logradouro = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    numero = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    complemento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    bairro = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    cidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cliente", x => x.id);
                    table.ForeignKey(
                        name: "fk_cliente_planos_plano_id",
                        column: x => x.plano_id,
                        principalSchema: "app",
                        principalTable: "plano",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cliente_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "app",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assinatura",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plano_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data_fim_trial = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    proxima_cobranca = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    inicio_tolerancia = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fim_ciclo_pago = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    valor_mensal = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    mercado_pago_subscription_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assinatura", x => x.id);
                    table.ForeignKey(
                        name: "fk_assinatura_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "app",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assinatura_planos_plano_id",
                        column: x => x.plano_id,
                        principalSchema: "app",
                        principalTable: "plano",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "auditoria_assinatura",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assinatura_id = table.Column<Guid>(type: "uuid", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    data_hora = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip = table.Column<string>(type: "text", nullable: true),
                    plano_anterior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    novo_plano = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    valor = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    descricao = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditoria_assinatura", x => x.id);
                    table.ForeignKey(
                        name: "fk_auditoria_assinatura_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "app",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "certificado_digital",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_path = table.Column<string>(type: "text", nullable: false),
                    thumbprint = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    validade = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    senha_cifrada = table.Column<byte[]>(type: "bytea", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_certificado_digital", x => x.id);
                    table.ForeignKey(
                        name: "fk_certificado_digital_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "app",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "imovel",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    logradouro = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    numero = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    complemento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    bairro = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    cidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    numero_iptu = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    numero_matricula = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_imovel", x => x.id);
                    table.ForeignKey(
                        name: "fk_imovel_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "app",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inquilino",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_pessoa = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    documento = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    inscricao_municipal = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    logradouro = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    numero = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    complemento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    bairro = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    cidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inquilino", x => x.id);
                    table.ForeignKey(
                        name: "fk_inquilino_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "app",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagamento_plano",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assinatura_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    data_vencimento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data_pagamento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    metodo_pagamento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    mercado_pago_payment_id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    observacao = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pagamento_plano", x => x.id);
                    table.ForeignKey(
                        name: "fk_pagamento_plano_assinatura_assinatura_id",
                        column: x => x.assinatura_id,
                        principalSchema: "app",
                        principalTable: "assinatura",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "despesa",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    imovel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    categoria = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    fornecedor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    competencia = table.Column<string>(type: "char(7)", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    data_pagamento = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_despesa", x => x.id);
                    table.ForeignKey(
                        name: "fk_despesa_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "app",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_despesa_imoveis_imovel_id",
                        column: x => x.imovel_id,
                        principalSchema: "app",
                        principalTable: "imovel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contrato",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    imovel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inquilino_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_contrato = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    data_fim_prevista = table.Column<DateOnly>(type: "date", nullable: true),
                    dia_vencimento = table.Column<int>(type: "integer", nullable: false),
                    valor_aluguel = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    juros_atraso_pct = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    multa_atraso_pct = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    anexo_path = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contrato", x => x.id);
                    table.ForeignKey(
                        name: "fk_contrato_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "app",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contrato_imoveis_imovel_id",
                        column: x => x.imovel_id,
                        principalSchema: "app",
                        principalTable: "imovel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contrato_inquilinos_inquilino_id",
                        column: x => x.inquilino_id,
                        principalSchema: "app",
                        principalTable: "inquilino",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nota_fiscal_servico",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    imovel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contrato_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inquilino_id = table.Column<Guid>(type: "uuid", nullable: true),
                    competencia = table.Column<string>(type: "char(7)", nullable: false),
                    numero = table.Column<long>(type: "bigint", nullable: true),
                    serie = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    chave_acesso = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    valor_servico = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    desconto = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    multa = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    juros = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    valor_faturado = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    data_emissao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    usuario_emissor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    solicitado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    motivo_rejeicao = table.Column<string>(type: "text", nullable: true),
                    motivo_cancelamento = table.Column<string>(type: "text", nullable: true),
                    protocolo_cancelamento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    data_cancelamento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nota_fiscal_servico", x => x.id);
                    table.ForeignKey(
                        name: "fk_nota_fiscal_servico_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "app",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nota_fiscal_servico_contrato_contrato_id",
                        column: x => x.contrato_id,
                        principalSchema: "app",
                        principalTable: "contrato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nota_fiscal_servico_imovel_imovel_id",
                        column: x => x.imovel_id,
                        principalSchema: "app",
                        principalTable: "imovel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nota_fiscal_servico_inquilino_inquilino_id",
                        column: x => x.inquilino_id,
                        principalSchema: "app",
                        principalTable: "inquilino",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documento_fiscal",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nfse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    storage_path = table.Column<string>(type: "text", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    tamanho_bytes = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documento_fiscal", x => x.id);
                    table.ForeignKey(
                        name: "fk_documento_fiscal_nota_fiscal_servico_nfse_id",
                        column: x => x.nfse_id,
                        principalSchema: "app",
                        principalTable: "nota_fiscal_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pagamento",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    imovel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contrato_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nfse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    competencia = table.Column<string>(type: "char(7)", nullable: false),
                    valor_pago = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    data_pagamento = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pagamento", x => x.id);
                    table.ForeignKey(
                        name: "fk_pagamento_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "app",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pagamento_contrato_contrato_id",
                        column: x => x.contrato_id,
                        principalSchema: "app",
                        principalTable: "contrato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pagamento_imovel_imovel_id",
                        column: x => x.imovel_id,
                        principalSchema: "app",
                        principalTable: "imovel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pagamento_nota_fiscal_servico_nfse_id",
                        column: x => x.nfse_id,
                        principalSchema: "app",
                        principalTable: "nota_fiscal_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assinatura_plano_id",
                schema: "app",
                table: "assinatura",
                column: "plano_id");

            migrationBuilder.CreateIndex(
                name: "ix_assinatura_proxima_cobranca",
                schema: "app",
                table: "assinatura",
                column: "proxima_cobranca",
                filter: "status = 'Ativa'");

            migrationBuilder.CreateIndex(
                name: "ux_assinatura_cliente_vigente",
                schema: "app",
                table: "assinatura",
                column: "cliente_id",
                unique: true,
                filter: "status IN ('Trial','PendentePagamento','Ativa')");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_tenant_id_tabela_registro_id",
                schema: "app",
                table: "audit_log",
                columns: new[] { "tenant_id", "tabela", "registro_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_assinatura_cliente_id_data_hora",
                schema: "app",
                table: "auditoria_assinatura",
                columns: new[] { "cliente_id", "data_hora" });

            migrationBuilder.CreateIndex(
                name: "ix_certificado_digital_cliente_id",
                schema: "app",
                table: "certificado_digital",
                column: "cliente_id",
                filter: "ativo");

            migrationBuilder.CreateIndex(
                name: "ix_cliente_plano_id",
                schema: "app",
                table: "cliente",
                column: "plano_id");

            migrationBuilder.CreateIndex(
                name: "ix_cliente_tenant_id_cnpj",
                schema: "app",
                table: "cliente",
                columns: new[] { "tenant_id", "cnpj" },
                unique: true,
                filter: "cnpj IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cliente_tenant_id_cpf",
                schema: "app",
                table: "cliente",
                columns: new[] { "tenant_id", "cpf" },
                unique: true,
                filter: "cpf IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_contrato_cliente_id",
                schema: "app",
                table: "contrato",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_contrato_inquilino_id",
                schema: "app",
                table: "contrato",
                column: "inquilino_id");

            migrationBuilder.CreateIndex(
                name: "ux_contrato_imovel_ativo",
                schema: "app",
                table: "contrato",
                column: "imovel_id",
                unique: true,
                filter: "status = 'Ativo'");

            migrationBuilder.CreateIndex(
                name: "ix_despesa_cliente_id",
                schema: "app",
                table: "despesa",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_despesa_imovel_id_competencia",
                schema: "app",
                table: "despesa",
                columns: new[] { "imovel_id", "competencia" });

            migrationBuilder.CreateIndex(
                name: "ix_documento_fiscal_nfse_id",
                schema: "app",
                table: "documento_fiscal",
                column: "nfse_id");

            migrationBuilder.CreateIndex(
                name: "ix_imovel_cliente_id",
                schema: "app",
                table: "imovel",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_inquilino_cliente_id",
                schema: "app",
                table: "inquilino",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_nota_fiscal_servico_cliente_id_status",
                schema: "app",
                table: "nota_fiscal_servico",
                columns: new[] { "cliente_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_nota_fiscal_servico_contrato_id",
                schema: "app",
                table: "nota_fiscal_servico",
                column: "contrato_id");

            migrationBuilder.CreateIndex(
                name: "ix_nota_fiscal_servico_inquilino_id",
                schema: "app",
                table: "nota_fiscal_servico",
                column: "inquilino_id");

            migrationBuilder.CreateIndex(
                name: "ux_nfse_competencia_ativa",
                schema: "app",
                table: "nota_fiscal_servico",
                columns: new[] { "imovel_id", "competencia" },
                unique: true,
                filter: "status <> 'Cancelada'");

            migrationBuilder.CreateIndex(
                name: "ix_pagamento_cliente_id",
                schema: "app",
                table: "pagamento",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagamento_contrato_id",
                schema: "app",
                table: "pagamento",
                column: "contrato_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagamento_nfse_id",
                schema: "app",
                table: "pagamento",
                column: "nfse_id");

            migrationBuilder.CreateIndex(
                name: "ux_pagamento_competencia",
                schema: "app",
                table: "pagamento",
                columns: new[] { "imovel_id", "competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pagamento_plano_assinatura_id",
                schema: "app",
                table: "pagamento_plano",
                column: "assinatura_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagamento_plano_mercado_pago_payment_id",
                schema: "app",
                table: "pagamento_plano",
                column: "mercado_pago_payment_id",
                unique: true,
                filter: "mercado_pago_payment_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_users_clientes_cliente_id",
                schema: "identity",
                table: "asp_net_users",
                column: "cliente_id",
                principalSchema: "app",
                principalTable: "cliente",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_users_clientes_cliente_id",
                schema: "identity",
                table: "asp_net_users");

            migrationBuilder.DropTable(
                name: "audit_log",
                schema: "app");

            migrationBuilder.DropTable(
                name: "auditoria_assinatura",
                schema: "app");

            migrationBuilder.DropTable(
                name: "certificado_digital",
                schema: "app");

            migrationBuilder.DropTable(
                name: "despesa",
                schema: "app");

            migrationBuilder.DropTable(
                name: "documento_fiscal",
                schema: "app");

            migrationBuilder.DropTable(
                name: "pagamento",
                schema: "app");

            migrationBuilder.DropTable(
                name: "pagamento_plano",
                schema: "app");

            migrationBuilder.DropTable(
                name: "nota_fiscal_servico",
                schema: "app");

            migrationBuilder.DropTable(
                name: "assinatura",
                schema: "app");

            migrationBuilder.DropTable(
                name: "contrato",
                schema: "app");

            migrationBuilder.DropTable(
                name: "imovel",
                schema: "app");

            migrationBuilder.DropTable(
                name: "inquilino",
                schema: "app");

            migrationBuilder.DropTable(
                name: "cliente",
                schema: "app");
        }
    }
}
