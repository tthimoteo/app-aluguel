# 05 — Entidades do Entity Framework Core

EF Core (.NET 9) + Npgsql + **ASP.NET Core Identity**. Mapeamento *code-first* com `IEntityTypeConfiguration<T>`, *global query filters* (tenant + soft delete) e interceptors.

## 1. Tipos base (`Domain/Common`)

```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void Raise(IDomainEvent e) => _domainEvents.Add(e);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
public abstract class AggregateRoot : Entity;

public interface ITenantOwned   { Guid TenantId { get; } }
public interface ISoftDeletable { DateTimeOffset? DeletedAt { get; } }
public interface IAuditable {
    DateTimeOffset CreatedAt { get; } DateTimeOffset UpdatedAt { get; }
    Guid? CreatedBy { get; } Guid? UpdatedBy { get; }
}
public interface IDomainEvent { }
```

## 2. Enums

```csharp
public enum TipoPessoa { PF = 1, PJ = 2 }
public enum StatusCliente { Trial = 1, PendentePagamento = 2, Ativa = 3, Suspensa = 4, Cancelada = 5 }

// Usuário do cliente (§6)
public enum PerfilUsuario { Gestor = 1, Analista = 2, AdminSistema = 9 }
public enum StatusUsuario { Ativo = 1, Inativo = 2, Bloqueado = 3 }

public enum StatusAssinatura { Trial = 1, PendentePagamento = 2, Ativa = 3, Suspensa = 4, Cancelada = 5 }
public enum StatusPagamentoPlano { Pendente = 1, Pago = 2, Falhou = 3, Estornado = 4 }
public enum MetodoPagamento { PIX = 1, Cartao = 2 }
public enum EventoAssinatura {
    InicioTrial, Contratacao, Renovacao, Pagamento, FalhaPagamento,
    Suspensao, Reativacao, Upgrade, Downgrade, Cancelamento
}

public enum TipoImovel { Residencial = 1, Comercial = 2, Galpao = 3, Sala = 4, Outro = 9 }
public enum StatusImovel { Ativo = 1, Inativo = 2 }
public enum StatusContrato { Ativo = 1, Encerrado = 2, Cancelado = 3 }

// NFS-e (§11)
public enum StatusNfse {
    Rascunho = 1, EmProcessamento = 2, Emitida = 3,
    Rejeitada = 4, CancelamentoSolicitado = 5, Cancelada = 6
}
public enum TipoDocumentoFiscal { XML = 1, PDF = 2, XMLCancelamento = 3 }
public enum TipoDespesa { IPTU = 1, Outra = 2 }
```

Enums persistidos como `varchar` via `HasConversion<string>()` (coerente com os `CHECK` do doc 03).

## 3. Value Objects (exemplo)

```csharp
public sealed record Cpf { public string Valor { get; } /* validação de dígitos */ }
public sealed record Cnpj { public string Valor { get; } }
public sealed record Competencia    // "MM/AAAA"
{
    public int Mes { get; } public int Ano { get; }
    public override string ToString() => $"{Mes:00}/{Ano}";
}
public sealed record Endereco(
    string? Logradouro, string? Numero, string? Complemento,
    string? Bairro, string? Cidade, string? Uf, string? Cep);   // OwnsOne
```

## 4. Usuário via Identity (`AppUser`)

O "Usuário do cliente" (§6) é modelado estendendo `IdentityUser<Guid>` — a senha é o `PasswordHash` do Identity.

```csharp
public class AppUser : IdentityUser<Guid>   // tabela identity.asp_net_users
{
    public Guid TenantId { get; set; }
    public Guid ClienteId { get; set; }
    public string Nome { get; set; } = default!;
    public string? Cpf { get; set; }
    public string? Telefone { get; set; }
    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Gestor;
    public StatusUsuario Status { get; set; } = StatusUsuario.Ativo;
    public DateTimeOffset? UltimoLogin { get; set; }
    // Email/PasswordHash/PhoneNumber/TwoFactorEnabled vêm de IdentityUser
}
public class AppRole : IdentityRole<Guid> { }
```

## 5. Cliente e Certificado

```csharp
public class Cliente : AggregateRoot, ITenantOwned, IAuditable, ISoftDeletable
{
    public Guid TenantId { get; private set; }
    public TipoPessoa TipoPessoa { get; private set; }
    public Guid? PlanoId { get; private set; }
    public StatusCliente Status { get; private set; } = StatusCliente.Trial;
    // PF
    public string? Nome { get; private set; }
    public string? Cpf { get; private set; }
    public DateOnly? DataNascimento { get; private set; }
    // PJ
    public string? RazaoSocial { get; private set; }
    public string? NomeFantasia { get; private set; }
    public string? Cnpj { get; private set; }
    public string? InscricaoMunicipal { get; private set; }
    public string? CnaePrincipal { get; private set; }
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public Endereco Endereco { get; private set; } = new(null,null,null,null,null,null,null);
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    private Cliente() { }
}

public class CertificadoDigital : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public string StoragePath { get; private set; } = default!;   // bucket 'certificates'
    public string Thumbprint { get; private set; } = default!;
    public DateTimeOffset Validade { get; private set; }
    public byte[] SenhaCifrada { get; private set; } = default!;
    public bool Ativo { get; private set; } = true;
    private CertificadoDigital() { }
}
```

## 6. Assinatura (agregado + máquina de estados)

```csharp
public class Assinatura : AggregateRoot, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid PlanoId { get; private set; }
    public StatusAssinatura Status { get; private set; } = StatusAssinatura.Trial;
    public DateTimeOffset DataInicio { get; private set; }
    public DateTimeOffset? DataFimTrial { get; private set; }
    public DateTimeOffset? ProximaCobranca { get; private set; }
    public DateTimeOffset? InicioTolerancia { get; private set; }
    public DateTimeOffset? CanceladaEm { get; private set; }
    public DateTimeOffset? FimCicloPago { get; private set; }
    public decimal ValorMensal { get; private set; }
    public string? MercadoPagoSubscriptionId { get; private set; }
    private readonly List<PagamentoPlano> _pagamentos = new();
    public IReadOnlyCollection<PagamentoPlano> Pagamentos => _pagamentos.AsReadOnly();
    private Assinatura() { }

    public static Assinatura IniciarTrial(Guid tenant, Guid cliente, Plano plano, DateTimeOffset agora) { /* ... */ return new(); }
    public void Contratar(Plano novo, DateTimeOffset agora) { /* -> PendentePagamento */ }
    public void ConfirmarPagamento(DateTimeOffset agora, int diasCiclo = 30) { Status = StatusAssinatura.Ativa; InicioTolerancia = null; ProximaCobranca = agora.AddDays(diasCiclo); }
    public void RegistrarFalhaPagamento(DateTimeOffset agora) { Status = StatusAssinatura.PendentePagamento; InicioTolerancia ??= agora; }
    public void SuspenderPorInadimplencia() { Status = StatusAssinatura.Suspensa; }
    public void Cancelar(DateTimeOffset agora) { Status = StatusAssinatura.Cancelada; CanceladaEm = agora; FimCicloPago = ProximaCobranca; }
}

public class PagamentoPlano : Entity, ITenantOwned { /* AssinaturaId, Valor, DataVencimento, DataPagamento, Status(StatusPagamentoPlano), MetodoPagamento, MercadoPagoPaymentId, Observacao */ }
public class AuditoriaAssinatura : Entity, ITenantOwned { /* ClienteId, AssinaturaId?, UsuarioId?, Evento, DataHora, Ip, PlanoAnterior, NovoPlano, Valor, Descricao */ }
public class Plano : Entity { public string Codigo=default!; public int? MaxImoveis; public int? MaxUsuarios; public bool PermiteNfse; public int TrialDias; public decimal? ValorMensal; }
```

## 7. Imóvel, Inquilino, Contrato

```csharp
public class Imovel : Entity, ITenantOwned, ISoftDeletable
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public string Nome { get; private set; } = default!;
    public TipoImovel Tipo { get; private set; }
    public Endereco Endereco { get; private set; } = new(null,null,null,null,null,null,null);
    public string? NumeroIptu { get; private set; }
    public string? NumeroMatricula { get; private set; }
    public StatusImovel Status { get; private set; } = StatusImovel.Ativo;
    public DateTimeOffset? DeletedAt { get; private set; }
    private Imovel() { }
}

public class Inquilino : Entity, ITenantOwned, ISoftDeletable
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public TipoPessoa TipoPessoa { get; private set; }
    public string Nome { get; private set; } = default!;     // nome ou razão social
    public string Documento { get; private set; } = default!; // CPF/CNPJ
    public string? InscricaoMunicipal { get; private set; }
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public Endereco Endereco { get; private set; } = new(null,null,null,null,null,null,null);
    public StatusImovel Status { get; private set; } = StatusImovel.Ativo;  // Ativo/Inativo
    public DateTimeOffset? DeletedAt { get; private set; }
    private Inquilino() { }
}

public class Contrato : AggregateRoot, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid ImovelId { get; private set; }
    public Guid InquilinoId { get; private set; }
    public string NumeroContrato { get; private set; } = default!;
    public StatusContrato Status { get; private set; } = StatusContrato.Ativo;
    public DateOnly DataInicio { get; private set; }
    public DateOnly? DataFimPrevista { get; private set; }
    public int DiaVencimento { get; private set; }
    public decimal ValorAluguel { get; private set; }
    public decimal? JurosAtrasoPct { get; private set; }
    public decimal? MultaAtrasoPct { get; private set; }
    public string? AnexoPath { get; private set; }   // bucket 'contracts'
    private Contrato() { }
}
```

## 8. Fiscal: Faturamento (NFS-e) e Documentos

```csharp
public class NotaFiscalServico : AggregateRoot, ITenantOwned   // "Faturamento"
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid ImovelId { get; private set; }
    public Guid? ContratoId { get; private set; }
    public Guid? InquilinoId { get; private set; }
    public string Competencia { get; private set; } = default!;   // "MM/AAAA"
    public long? Numero { get; private set; }
    public string? Serie { get; private set; }
    public string? ChaveAcesso { get; private set; }
    public StatusNfse Status { get; private set; } = StatusNfse.Rascunho;
    public decimal ValorServico { get; private set; }
    public decimal Desconto { get; private set; }
    public decimal Multa { get; private set; }
    public decimal Juros { get; private set; }
    public decimal ValorFaturado { get; private set; }
    public DateTimeOffset? DataEmissao { get; private set; }
    public Guid? UsuarioEmissorId { get; private set; }
    public DateTimeOffset? SolicitadoEm { get; private set; }
    public string? MotivoRejeicao { get; private set; }
    // cancelamento (§12)
    public string? MotivoCancelamento { get; private set; }
    public string? ProtocoloCancelamento { get; private set; }
    public DateTimeOffset? DataCancelamento { get; private set; }
    private readonly List<DocumentoFiscal> _documentos = new();
    public IReadOnlyCollection<DocumentoFiscal> Documentos => _documentos.AsReadOnly();
    private NotaFiscalServico() { }

    public void MarcarEmitida(long numero, string serie, string chave, DateTimeOffset agora) { /* ... */ }
    public void SolicitarCancelamento(string motivo) { Status = StatusNfse.CancelamentoSolicitado; MotivoCancelamento = motivo; }
    public void ConfirmarCancelamento(string protocolo, DateTimeOffset agora) { Status = StatusNfse.Cancelada; ProtocoloCancelamento = protocolo; DataCancelamento = agora; }
}

public class DocumentoFiscal : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid NfseId { get; private set; }
    public TipoDocumentoFiscal Tipo { get; private set; }   // XML / PDF / XMLCancelamento
    public string StoragePath { get; private set; } = default!;
    public string? ContentHash { get; private set; }
    public long? TamanhoBytes { get; private set; }
    private DocumentoFiscal() { }
}
```

## 9. Financeiro: Pagamento e Despesa

```csharp
public class Pagamento : Entity, ITenantOwned   // pagamento de aluguel (§12)
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid ImovelId { get; private set; }
    public Guid? ContratoId { get; private set; }
    public Guid? NfseId { get; private set; }          // opcional
    public string Competencia { get; private set; } = default!;
    public decimal ValorPago { get; private set; }
    public DateOnly DataPagamento { get; private set; }
    private Pagamento() { }
}

public class Despesa : Entity, ITenantOwned   // IPTU e outras (§12)
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid ImovelId { get; private set; }
    public TipoDespesa Tipo { get; private set; }
    public string? Descricao { get; private set; }
    public string? Categoria { get; private set; }
    public string? Fornecedor { get; private set; }
    public string Competencia { get; private set; } = default!;
    public decimal Valor { get; private set; }
    public DateOnly? DataPagamento { get; private set; }
    private Despesa() { }
}
```

## 10. Auditoria e mensageria

```csharp
public class AuditLog                       // §13
{
    public long Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UsuarioId { get; set; }
    public string Acao { get; set; } = default!;   // Login, Emissao, Cancelamento, Pagamento, Despesa, AlteracaoCadastral
    public string? Tabela { get; set; }
    public Guid? RegistroId { get; set; }
    public string? ValoresAntes { get; set; }      // jsonb
    public string? ValoresDepois { get; set; }     // jsonb
    public DateTimeOffset DataHora { get; set; } = DateTimeOffset.UtcNow;
    public string? Ip { get; set; }
}
public class OutboxMessage { public Guid Id { get; set; } public Guid? TenantId { get; set; } public string Tipo=default!; public string Conteudo=default!; public DateTimeOffset OcorridoEm { get; set; } public DateTimeOffset? ProcessadoEm { get; set; } public int Tentativas { get; set; } }
public class InboxMessage  { public Guid Id { get; set; } public string Origem=default!; public string ChaveExterna=default!; public string Payload=default!; public DateTimeOffset? ProcessadoEm { get; set; } }
```

## 11. DbContext (Identity + domínio)

```csharp
public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    private readonly ICurrentTenant _tenant;
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant tenant)
        : base(options) => _tenant = tenant;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plano> Planos => Set<Plano>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Assinatura> Assinaturas => Set<Assinatura>();
    public DbSet<PagamentoPlano> PagamentosPlano => Set<PagamentoPlano>();
    public DbSet<AuditoriaAssinatura> AuditoriasAssinatura => Set<AuditoriaAssinatura>();
    public DbSet<Imovel> Imoveis => Set<Imovel>();
    public DbSet<Inquilino> Inquilinos => Set<Inquilino>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<NotaFiscalServico> NotasFiscais => Set<NotaFiscalServico>();
    public DbSet<DocumentoFiscal> DocumentosFiscais => Set<DocumentoFiscal>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
    public DbSet<Despesa> Despesas => Set<Despesa>();
    public DbSet<CertificadoDigital> Certificados => Set<CertificadoDigital>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);                 // mapeia Identity
        b.HasDefaultSchema("app");
        b.Entity<AppUser>().ToTable("asp_net_users", "identity");
        b.Entity<AppRole>().ToTable("asp_net_roles", "identity");
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var et in b.Model.GetEntityTypes())
        {
            if (typeof(ITenantOwned).IsAssignableFrom(et.ClrType))   et.AddTenantFilter(_tenant);
            if (typeof(ISoftDeletable).IsAssignableFrom(et.ClrType)) et.AddSoftDeleteFilter();
        }
    }
}
```

## 12. Configuration de exemplo (`Contrato` — CASO 3)

```csharp
public class ContratoConfiguration : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> b)
    {
        b.ToTable("contrato");
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(12);
        b.Property(x => x.ValorAluguel).HasColumnType("numeric(14,2)");
        // 1 contrato Ativo por imóvel:
        b.HasIndex(x => x.ImovelId).IsUnique().HasFilter("status = 'Ativo'");
        b.HasOne<Imovel>().WithMany().HasForeignKey(x => x.ImovelId);
        b.HasOne<Inquilino>().WithMany().HasForeignKey(x => x.InquilinoId);
    }
}
```

## 13. Interceptors

| Interceptor | Função |
|---|---|
| `TenantSaveInterceptor` | Preenche `TenantId` em `ITenantOwned`. |
| `AuditableInterceptor` | `CreatedAt/UpdatedAt/By`. |
| `SoftDeleteInterceptor` | `Remove()` → `DeletedAt`/`Status=Inativo`. |
| `AuditLogInterceptor` | Gera `AuditLog` (valores antes/depois em jsonb) — §13. |
| `OutboxInterceptor` | Serializa domain events em `OutboxMessage` na mesma transação. |
| `RlsConnectionInterceptor` | `SET app.tenant_id` por transação (RLS). |

Próximo: [06 — Multi-tenant](06-Estrategia-Multi-Tenant.md).
