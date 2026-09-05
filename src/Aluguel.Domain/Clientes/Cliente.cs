using Aluguel.Domain.Common;
using Aluguel.Domain.ValueObjects;

namespace Aluguel.Domain.Clientes;

/// <summary>Cliente (locador / emitente da NFS-e) — Pessoa Física ou Jurídica (§5).</summary>
public class Cliente : AggregateRoot, ITenantOwned, IAuditable, ISoftDeletable
{
    public Guid TenantId { get; private set; }
    public TipoPessoa TipoPessoa { get; private set; }
    public Guid? PlanoId { get; private set; }
    public StatusCliente Status { get; private set; } = StatusCliente.Trial;

    // Pessoa Física
    public string? Nome { get; private set; }
    public string? Cpf { get; private set; }
    public DateOnly? DataNascimento { get; private set; }

    // Pessoa Jurídica
    public string? RazaoSocial { get; private set; }
    public string? NomeFantasia { get; private set; }
    public string? Cnpj { get; private set; }
    public string? InscricaoMunicipal { get; private set; }
    public string? CnaePrincipal { get; private set; }

    // Contato + endereço
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public Endereco Endereco { get; private set; } = Endereco.Vazio();

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; private set; }

    private Cliente() { }

    /// <summary>Nome exibível independente do tipo de pessoa.</summary>
    public string NomeExibicao => TipoPessoa == TipoPessoa.PF ? Nome ?? "" : RazaoSocial ?? "";

    public static Cliente CriarPessoaFisica(Guid tenantId, Guid? planoId, string nome, string cpf,
        DateOnly? dataNascimento, string? telefone, string? email, Endereco? endereco)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));
        if (string.IsNullOrWhiteSpace(cpf)) throw new ArgumentException("CPF é obrigatório.", nameof(cpf));

        return new Cliente
        {
            TenantId = tenantId,
            TipoPessoa = TipoPessoa.PF,
            PlanoId = planoId,
            Nome = nome,
            Cpf = cpf,
            DataNascimento = dataNascimento,
            Telefone = telefone,
            Email = email,
            Endereco = endereco ?? Endereco.Vazio(),
        };
    }

    public static Cliente CriarPessoaJuridica(Guid tenantId, Guid? planoId, string razaoSocial, string? nomeFantasia,
        string cnpj, string? inscricaoMunicipal, string? cnaePrincipal, string? telefone, string? email, Endereco? endereco)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial)) throw new ArgumentException("Razão social é obrigatória.", nameof(razaoSocial));
        if (string.IsNullOrWhiteSpace(cnpj)) throw new ArgumentException("CNPJ é obrigatório.", nameof(cnpj));

        return new Cliente
        {
            TenantId = tenantId,
            TipoPessoa = TipoPessoa.PJ,
            PlanoId = planoId,
            RazaoSocial = razaoSocial,
            NomeFantasia = nomeFantasia,
            Cnpj = cnpj,
            InscricaoMunicipal = inscricaoMunicipal,
            CnaePrincipal = cnaePrincipal,
            Telefone = telefone,
            Email = email,
            Endereco = endereco ?? Endereco.Vazio(),
        };
    }

    public void DefinirPlano(Guid? planoId)
    {
        PlanoId = planoId;
        Touch();
    }

    /// <summary>Atualiza os dados cadastrais de um cliente Pessoa Física (CPF e tipo são imutáveis).</summary>
    public void AtualizarDadosPessoaFisica(string nome, DateOnly? dataNascimento,
        string? telefone, string? email, Endereco? endereco)
    {
        if (TipoPessoa != TipoPessoa.PF)
            throw new InvalidOperationException("Cliente não é Pessoa Física.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        Nome = nome;
        DataNascimento = dataNascimento;
        Telefone = telefone;
        Email = email;
        if (endereco is not null) Endereco = endereco;
        Touch();
    }

    /// <summary>Atualiza os dados cadastrais de um cliente Pessoa Jurídica (CNPJ e tipo são imutáveis).</summary>
    public void AtualizarDadosPessoaJuridica(string razaoSocial, string? nomeFantasia,
        string? inscricaoMunicipal, string? cnaePrincipal, string? telefone, string? email, Endereco? endereco)
    {
        if (TipoPessoa != TipoPessoa.PJ)
            throw new InvalidOperationException("Cliente não é Pessoa Jurídica.");
        if (string.IsNullOrWhiteSpace(razaoSocial))
            throw new ArgumentException("Razão social é obrigatória.", nameof(razaoSocial));

        RazaoSocial = razaoSocial;
        NomeFantasia = nomeFantasia;
        InscricaoMunicipal = inscricaoMunicipal;
        CnaePrincipal = cnaePrincipal;
        Telefone = telefone;
        Email = email;
        if (endereco is not null) Endereco = endereco;
        Touch();
    }

    /// <summary>Exclusão lógica (soft delete) — preserva histórico e libera o índice único de CPF/CNPJ.</summary>
    public void Remover()
    {
        if (DeletedAt is not null) return;
        DeletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
