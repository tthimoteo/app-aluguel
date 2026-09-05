# 05 — Entidades do Entity Framework Core

EF Core 9 + Npgsql. Mapeamento *code-first* com `IEntityTypeConfiguration<T>`, *global query filters* (tenant + soft delete) e interceptors. Nomes de domínio em português; abstrações técnicas em inglês.

## 1. Tipos base (`Aluguel.Domain/Common`)

```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class AggregateRoot : Entity;

public interface ITenantOwned  { Guid TenantId { get; } }
public interface ISoftDeletable { DateTimeOffset? DeletedAt { get; } }

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset UpdatedAt { get; }
    Guid? CreatedBy { get; }
    Guid? UpdatedBy { get; }
}

public interface IDomainEvent { }
```

## 2. Enums (`Aluguel.Domain`)

```csharp
public enum TipoPessoa { PF = 1, PJ = 2 }
public enum StatusCliente { Trial = 1, PendentePagamento = 2, Ativa = 3, Suspensa = 4, Cancelada = 5 }
public enum PerfilUsuario { Admin = 1, Operador = 2, Financeiro = 3, Leitura = 4 }
public enum StatusAssinatura { Trial = 1, PendentePagamento = 2, Ativa = 3, Suspensa = 4, Cancelada = 5 }
public enum StatusPagamento { Pendente = 1, Pago = 2, Falhou = 3, Estornado = 4 }
public enum MetodoPagamento { PIX = 1, Cartao = 2, Boleto = 3 }
public enum EventoAssinatura {
    InicioTrial, Contratacao, Renovacao, Pagamento, FalhaPagamento,
    Suspensao, Reativacao, Upgrade, Downgrade, Cancelamento
}
public enum TipoImovel { Residencial = 1, Comercial = 2 }
public enum StatusImovel { Disponivel = 1, Alugado = 2, Manutencao = 3 }
public enum StatusContrato { Ativo = 1, Encerrado = 2, Rescindido = 3 }
public enum StatusRecebimento { Pendente = 1, Pago = 2, Atrasado = 3, Cancelado = 4 }
public enum StatusNfse { Pendente = 1, Processando = 2, Autorizada = 3, Rejeitada = 4, Cancelada = 5 }
public enum TipoDocumentoFiscal { XML = 1, PDF = 2, RPS = 3 }
```

> Enums são persistidos como `varchar` via `HasConversion<string>()` nas configurations (coerente com os `CHECK` do banco, doc 03).

## 3. Value Objects (exemplo)

```csharp
public sealed record Cpf
{
    public string Valor { get; }
    private Cpf(string valor) => Valor = valor;

    public static Cpf Criar(string entrada)
    {
        var digitos = new string(entrada.Where(char.IsDigit).ToArray());
        if (!Validar(digitos))
            throw new DomainException("CPF inválido.");
        return new Cpf(digitos);
    }
    private static bool Validar(string cpf) { /* dígitos verificadores */ return cpf.Length == 11; }
}

public sealed record Endereco(
    string? Logradouro, string? Numero, string? Complemento,
    string? Bairro, string? Cidade, string? Uf, string? Cep);
```

`Endereco` é mapeado como *owned type* (`OwnsOne`), embutindo colunas na tabela dona.

## 4. Catálogo e tenant

```csharp
public class Plano : Entity
{
    public string Codigo { get; private set; } = default!;   // TRIAL, BASICO, ...
    public string Nome { get; private set; } = default!;
    public int? MaxImoveis { get; private set; }
    public int? MaxUsuarios { get; private set; }
    public bool PermiteNfse { get; private set; }
    public int TrialDias { get; private set; }
    public decimal? ValorMensal { get; private set; }
    public bool Ativo { get; private set; } = true;
}

public class Tenant : Entity
{
    public string Nome { get; private set; } = default!;
    public string Subdominio { get; private set; } = default!;
    public bool Ativo { get; private set; } = true;
}
```

## 5. Cliente e Usuário

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
    public string? Cnae { get; private set; }

    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public Endereco Endereco { get; private set; } = new(null,null,null,null,null,null,null);

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private readonly List<Usuario> _usuarios = new();
    public IReadOnlyCollection<Usuario> Usuarios => _usuarios.AsReadOnly();
    private readonly List<Imovel> _imoveis = new();
    public IReadOnlyCollection<Imovel> Imoveis => _imoveis.AsReadOnly();

    private Cliente() { }   // EF
}

public class Usuario : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public PerfilUsuario Perfil { get; private set; } = PerfilUsuario.Operador;
    public string Nome { get; private set; } = default!;
    public string? Cpf { get; private set; }
    public string Email { get; private set; } = default!;
    public Guid? AuthUserId { get; private set; }   // vínculo com Supabase Auth (sem senha local)
    public bool Ativo { get; private set; } = true;
    public DateTimeOffset? UltimoLogin { get; private set; }
    private Usuario() { }
}
```

## 6. Assinatura (agregado com máquina de estados)

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

    public static Assinatura IniciarTrial(Guid tenantId, Guid clienteId, Plano plano, DateTimeOffset agora)
    {
        var a = new Assinatura {
            TenantId = tenantId, ClienteId = clienteId, PlanoId = plano.Id,
            Status = StatusAssinatura.Trial, DataInicio = agora,
            DataFimTrial = agora.AddDays(plano.TrialDias), ValorMensal = 0
        };
        a.Raise(new TrialIniciadoEvent(a.Id, clienteId));
        return a;
    }

    public void Contratar(Plano novoPlano, DateTimeOffset agora) {
        GarantirTransicao(StatusAssinatura.PendentePagamento);
        PlanoId = novoPlano.Id;
        ValorMensal = novoPlano.ValorMensal ?? 0;
        Status = StatusAssinatura.PendentePagamento;
        Raise(new PlanoContratadoEvent(Id, novoPlano.Id));
    }

    public void ConfirmarPagamento(DateTimeOffset agora, int diasCiclo = 30) {
        Status = StatusAssinatura.Ativa;
        InicioTolerancia = null;
        ProximaCobranca = agora.AddDays(diasCiclo);
        Raise(new AssinaturaAtivadaEvent(Id, ClienteId));
    }

    public void RegistrarFalhaPagamento(DateTimeOffset agora) {
        Status = StatusAssinatura.PendentePagamento;
        InicioTolerancia ??= agora;          // início dos 7 dias de tolerância
        Raise(new FalhaPagamentoEvent(Id));
    }

    public void SuspenderPorInadimplencia() {
        Status = StatusAssinatura.Suspensa;
        Raise(new AssinaturaSuspensaEvent(Id, ClienteId));
    }

    public void Cancelar(DateTimeOffset agora) {
        Status = StatusAssinatura.Cancelada;
        CanceladaEm = agora;
        FimCicloPago = ProximaCobranca;      // permanece ativa até o fim do ciclo pago
        Raise(new AssinaturaCanceladaEvent(Id));
    }

    private void GarantirTransicao(StatusAssinatura destino) { /* valida transições permitidas */ }
}
```

```csharp
public class PagamentoPlano : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid AssinaturaId { get; private set; }
    public decimal Valor { get; private set; }
    public DateTimeOffset DataVencimento { get; private set; }
    public DateTimeOffset? DataPagamento { get; private set; }
    public StatusPagamento Status { get; private set; } = StatusPagamento.Pendente;
    public MetodoPagamento? MetodoPagamento { get; private set; }
    public string? MercadoPagoPaymentId { get; private set; }
    public string? Observacao { get; private set; }
    private PagamentoPlano() { }
}

public class AuditoriaAssinatura : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid? AssinaturaId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public EventoAssinatura Evento { get; private set; }
    public DateTimeOffset DataHora { get; private set; }
    public string? Ip { get; private set; }
    public string? PlanoAnterior { get; private set; }
    public string? NovoPlano { get; private set; }
    public decimal? Valor { get; private set; }
    public string? Descricao { get; private set; }
    private AuditoriaAssinatura() { }
}
```

## 7. Imóveis, inquilinos, contratos, recebimentos

```csharp
public class Imovel : Entity, ITenantOwned, ISoftDeletable
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public string Codigo { get; private set; } = default!;
    public TipoImovel Tipo { get; private set; }
    public Endereco Endereco { get; private set; } = new(null,null,null,null,null,null,null);
    public decimal ValorAluguel { get; private set; }
    public StatusImovel Status { get; private set; } = StatusImovel.Disponivel;
    public DateTimeOffset? DeletedAt { get; private set; }
    private Imovel() { }
}

public class Inquilino : Entity, ITenantOwned, ISoftDeletable
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public TipoPessoa TipoPessoa { get; private set; }
    public string Nome { get; private set; } = default!;
    public string Documento { get; private set; } = default!;
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    private Inquilino() { }
}

public class Contrato : AggregateRoot, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid ImovelId { get; private set; }
    public Guid InquilinoId { get; private set; }
    public decimal ValorAluguel { get; private set; }
    public int DiaVencimento { get; private set; }
    public DateOnly DataInicio { get; private set; }
    public DateOnly? DataFim { get; private set; }
    public StatusContrato Status { get; private set; } = StatusContrato.Ativo;

    private readonly List<Recebimento> _recebimentos = new();
    public IReadOnlyCollection<Recebimento> Recebimentos => _recebimentos.AsReadOnly();
    private Contrato() { }
}

public class Recebimento : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid ContratoId { get; private set; }
    public string Competencia { get; private set; } = default!;  // "MM/AAAA"
    public decimal Valor { get; private set; }
    public DateOnly DataVencimento { get; private set; }
    public DateOnly? DataPagamento { get; private set; }
    public StatusRecebimento Status { get; private set; } = StatusRecebimento.Pendente;
    private Recebimento() { }
}
```

## 8. Módulo fiscal

```csharp
public class CertificadoDigital : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public string StoragePath { get; private set; } = default!;   // PFX no Supabase Storage
    public string Thumbprint { get; private set; } = default!;
    public DateTimeOffset Validade { get; private set; }
    public byte[] SenhaCifrada { get; private set; } = default!;   // nunca em texto puro
    public bool Ativo { get; private set; } = true;
    private CertificadoDigital() { }
}

public class NotaFiscalServico : AggregateRoot, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid? RecebimentoId { get; private set; }
    public long? NumeroRps { get; private set; }
    public string? SerieRps { get; private set; }
    public long? NumeroNfse { get; private set; }
    public string? CodigoVerificacao { get; private set; }
    public string? Protocolo { get; private set; }
    public decimal ValorServico { get; private set; }
    public decimal? ValorIss { get; private set; }
    public StatusNfse Status { get; private set; } = StatusNfse.Pendente;
    public string? MotivoRejeicao { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public DateTimeOffset? EmitidaEm { get; private set; }
    public DateTimeOffset? CanceladaEm { get; private set; }

    private readonly List<DocumentoFiscal> _documentos = new();
    public IReadOnlyCollection<DocumentoFiscal> Documentos => _documentos.AsReadOnly();
    private NotaFiscalServico() { }
}

public class DocumentoFiscal : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid NfseId { get; private set; }
    public TipoDocumentoFiscal Tipo { get; private set; }
    public string StoragePath { get; private set; } = default!;
    public string? ContentHash { get; private set; }
    public long? TamanhoBytes { get; private set; }
    private DocumentoFiscal() { }
}
```

## 9. Outbox / Inbox

```csharp
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public string Tipo { get; set; } = default!;
    public string Conteudo { get; set; } = default!;   // jsonb
    public DateTimeOffset OcorridoEm { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessadoEm { get; set; }
    public int Tentativas { get; set; }
    public string? Erro { get; set; }
}

public class InboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Origem { get; set; } = default!;
    public string ChaveExterna { get; set; } = default!;
    public DateTimeOffset RecebidoEm { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessadoEm { get; set; }
    public string Payload { get; set; } = default!;    // jsonb
}
```

## 10. DbContext

```csharp
public class AppDbContext : DbContext
{
    private readonly ICurrentTenant _tenant;
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant tenant)
        : base(options) => _tenant = tenant;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plano> Planos => Set<Plano>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Assinatura> Assinaturas => Set<Assinatura>();
    public DbSet<PagamentoPlano> Pagamentos => Set<PagamentoPlano>();
    public DbSet<AuditoriaAssinatura> AuditoriasAssinatura => Set<AuditoriaAssinatura>();
    public DbSet<Imovel> Imoveis => Set<Imovel>();
    public DbSet<Inquilino> Inquilinos => Set<Inquilino>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Recebimento> Recebimentos => Set<Recebimento>();
    public DbSet<CertificadoDigital> Certificados => Set<CertificadoDigital>();
    public DbSet<NotaFiscalServico> NotasFiscais => Set<NotaFiscalServico>();
    public DbSet<DocumentoFiscal> DocumentosFiscais => Set<DocumentoFiscal>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filters: tenant + soft delete aplicados a todas as entidades marcadas.
        foreach (var et in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantOwned).IsAssignableFrom(et.ClrType))
                et.AddTenantFilter(_tenant);     // extensão: tenant_id == _tenant.TenantId
            if (typeof(ISoftDeletable).IsAssignableFrom(et.ClrType))
                et.AddSoftDeleteFilter();         // extensão: DeletedAt == null
        }
    }
}
```

## 11. Exemplo de configuration (`Cliente`)

```csharp
public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("cliente");
        b.HasKey(x => x.Id);
        b.Property(x => x.TipoPessoa).HasConversion<string>().HasMaxLength(2);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Cpf).HasMaxLength(11);
        b.Property(x => x.Cnpj).HasMaxLength(14);
        b.Property(x => x.Email).HasColumnType("citext");

        b.OwnsOne(x => x.Endereco, e =>
        {
            e.Property(p => p.Logradouro).HasColumnName("logradouro").HasMaxLength(150);
            e.Property(p => p.Numero).HasColumnName("numero").HasMaxLength(15);
            e.Property(p => p.Complemento).HasColumnName("complemento").HasMaxLength(60);
            e.Property(p => p.Bairro).HasColumnName("bairro").HasMaxLength(80);
            e.Property(p => p.Cidade).HasColumnName("cidade").HasMaxLength(80);
            e.Property(p => p.Uf).HasColumnName("uf").HasMaxLength(2);
            e.Property(p => p.Cep).HasColumnName("cep").HasMaxLength(8);
        });

        b.HasIndex(x => new { x.TenantId, x.Cpf }).IsUnique()
            .HasFilter("cpf IS NOT NULL AND deleted_at IS NULL");
        b.HasMany(x => x.Usuarios).WithOne().HasForeignKey(u => u.ClienteId);
        b.HasQueryFilter(x => x.DeletedAt == null);   // combinado ao filtro de tenant
    }
}
```

## 12. Interceptors (Infrastructure)

| Interceptor | Função |
|---|---|
| `TenantSaveInterceptor` | Preenche `TenantId` em entidades `ITenantOwned` recém-adicionadas. |
| `AuditableInterceptor` | Preenche `CreatedAt/UpdatedAt/CreatedBy/UpdatedBy`. |
| `SoftDeleteInterceptor` | Converte `Remove()` em `DeletedAt = now` para `ISoftDeletable`. |
| `OutboxInterceptor` | Serializa `DomainEvents` das entidades em `OutboxMessage` na mesma transação. |
| `RlsConnectionInterceptor` | Executa `SET app.tenant_id = '<guid>'` ao abrir a conexão (reforço RLS). |

## 13. Pacotes NuGet principais

```text
Microsoft.EntityFrameworkCore            9.*
Npgsql.EntityFrameworkCore.PostgreSQL    9.*
EFCore.NamingConventions                 9.*   (snake_case automático)
MediatR                                  12.*
FluentValidation                         11.*
Mapster                                   7.*
```

Próximo: [06 — Estratégia Multi-tenant](06-Estrategia-Multi-Tenant.md).
