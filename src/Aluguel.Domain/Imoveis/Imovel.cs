using Aluguel.Domain.Common;
using Aluguel.Domain.ValueObjects;

namespace Aluguel.Domain.Imoveis;

/// <summary>Imóvel do cliente disponível para locação (§7).</summary>
public class Imovel : AggregateRoot, ITenantOwned, IAuditable, ISoftDeletable
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public string Nome { get; private set; } = default!;
    public TipoImovel Tipo { get; private set; }
    public Endereco Endereco { get; private set; } = Endereco.Vazio();
    public string? NumeroIptu { get; private set; }
    public string? NumeroMatricula { get; private set; }
    public StatusAtivoInativo Status { get; private set; } = StatusAtivoInativo.Ativo;

    /// <summary>Competência inicial da cobrança no formato MM/AAAA.</summary>
    public string? CompetenciaInicial { get; private set; }
    public PropositoLocacao? PropositoLocacao { get; private set; }
    public int? DiaVencimentoCobranca { get; private set; }
    public decimal? DespesasCondominiais { get; private set; }
    /// <summary>Valor monetário do IPTU (distinto do número/cadastro municipal).</summary>
    public decimal? ValorIptu { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; private set; }

    private Imovel() { }

    public Imovel(Guid tenantId, Guid clienteId, string nome, TipoImovel tipo, Endereco? endereco = null,
        string? numeroIptu = null, string? numeroMatricula = null)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        TenantId = tenantId;
        ClienteId = clienteId;
        Nome = nome;
        Tipo = tipo;
        Endereco = endereco ?? Endereco.Vazio();
        NumeroIptu = numeroIptu;
        NumeroMatricula = numeroMatricula;
    }

    public void Atualizar(string nome, TipoImovel tipo, Endereco? endereco,
        string? numeroIptu, string? numeroMatricula)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        Nome = nome;
        Tipo = tipo;
        if (endereco is not null) Endereco = endereco;
        NumeroIptu = numeroIptu;
        NumeroMatricula = numeroMatricula;
        Touch();
    }

    /// <summary>Define os parâmetros de cobrança do imóvel (competência, propósito, vencimento e encargos).</summary>
    public void DefinirCobranca(
        string? competenciaInicial,
        PropositoLocacao? propositoLocacao,
        int? diaVencimentoCobranca,
        decimal? despesasCondominiais,
        decimal? valorIptu)
    {
        if (competenciaInicial is not null)
        {
            if (!EhCompetenciaValida(competenciaInicial))
                throw new ArgumentException("Competência inicial deve estar no formato MM/AAAA.", nameof(competenciaInicial));
        }

        if (diaVencimentoCobranca is { } dia && dia is < 1 or > 31)
            throw new ArgumentException("Dia de vencimento da cobrança deve estar entre 1 e 31.", nameof(diaVencimentoCobranca));

        if (despesasCondominiais is { } condo && condo < 0)
            throw new ArgumentException("Despesas condominiais não podem ser negativas.", nameof(despesasCondominiais));

        if (valorIptu is { } iptu && iptu < 0)
            throw new ArgumentException("Valor do IPTU não pode ser negativo.", nameof(valorIptu));

        CompetenciaInicial = competenciaInicial;
        PropositoLocacao = propositoLocacao;
        DiaVencimentoCobranca = diaVencimentoCobranca;
        DespesasCondominiais = despesasCondominiais;
        ValorIptu = valorIptu;
        Touch();
    }

    /// <summary>Inativa o imóvel (ex.: enquadramento em downgrade de plano — CASO 2).</summary>
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

    private static bool EhCompetenciaValida(string competencia)
    {
        if (competencia.Length != 7 || competencia[2] != '/') return false;
        if (!int.TryParse(competencia.AsSpan(0, 2), out var mes) || mes is < 1 or > 12) return false;
        return int.TryParse(competencia.AsSpan(3, 4), out var ano) && ano is >= 2000 and <= 2100;
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
