using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using Aluguel.Domain.ValueObjects;

namespace Aluguel.Domain.Inquilinos;

/// <summary>Inquilino (destinatário da NFS-e). Vincula-se ao imóvel antes do contrato (§7/§8: um por imóvel).</summary>
public class Inquilino : AggregateRoot, ITenantOwned, IAuditable, ISoftDeletable
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    /// <summary>Imóvel ao qual o inquilino está vinculado (um ativo por imóvel). O contrato reforça o vínculo comercial.</summary>
    public Guid? ImovelId { get; private set; }
    public TipoPessoa TipoPessoa { get; private set; }
    public string Nome { get; private set; } = default!;      // nome ou razão social
    public string Documento { get; private set; } = default!;  // CPF ou CNPJ
    public string? InscricaoMunicipal { get; private set; }
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public Endereco Endereco { get; private set; } = Endereco.Vazio();
    public StatusAtivoInativo Status { get; private set; } = StatusAtivoInativo.Ativo;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; private set; }

    private Inquilino() { }

    public Inquilino(Guid tenantId, Guid clienteId, TipoPessoa tipoPessoa, string nome, string documento,
        string? telefone = null, string? email = null, Endereco? endereco = null, string? inscricaoMunicipal = null,
        Guid? imovelId = null)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));
        if (string.IsNullOrWhiteSpace(documento)) throw new ArgumentException("Documento é obrigatório.", nameof(documento));

        TenantId = tenantId;
        ClienteId = clienteId;
        ImovelId = imovelId;
        TipoPessoa = tipoPessoa;
        Nome = nome;
        Documento = documento;
        Telefone = telefone;
        Email = email;
        Endereco = endereco ?? Endereco.Vazio();
        InscricaoMunicipal = inscricaoMunicipal;
    }

    /// <summary>Associa o inquilino a um imóvel (cadastro pré-contrato). Um imóvel ativo só admite um inquilino.</summary>
    public void VincularAoImovel(Guid imovelId)
    {
        if (imovelId == Guid.Empty) throw new ArgumentException("Imóvel é obrigatório.", nameof(imovelId));
        ImovelId = imovelId;
        Touch();
    }

    public void DesvincularDoImovel()
    {
        ImovelId = null;
        Touch();
    }

    /// <summary>Atualiza dados cadastrais. Tipo de pessoa e documento são imutáveis.</summary>
    public void Atualizar(string nome, string? inscricaoMunicipal, string? telefone, string? email, Endereco? endereco)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        Nome = nome;
        InscricaoMunicipal = inscricaoMunicipal;
        Telefone = telefone;
        Email = email;
        if (endereco is not null) Endereco = endereco;
        Touch();
    }

    public void Inativar()
    {
        Status = StatusAtivoInativo.Inativo;
        Touch();
    }

    public void Ativar()
    {
        Status = StatusAtivoInativo.Ativo;
        Touch();
    }

    /// <summary>Exclusão lógica (soft delete). Só deve ocorrer sem dependentes (§4).</summary>
    public void Remover()
    {
        if (DeletedAt is not null) return;
        DeletedAt = DateTimeOffset.UtcNow;
        Status = StatusAtivoInativo.Inativo;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
