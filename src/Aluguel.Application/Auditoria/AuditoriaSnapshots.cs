using Aluguel.Application.Clientes;
using Aluguel.Application.Cobrancas;
using Aluguel.Application.Contratos;
using Aluguel.Application.Imoveis;
using Aluguel.Application.Inquilinos;
using Aluguel.Application.Usuarios;
using Aluguel.Domain.Imoveis;

namespace Aluguel.Application.Auditoria;

/// <summary>Projeções enxutas para jsonb de auditoria (sem timestamps/segredos).</summary>
public static class AuditoriaSnapshots
{
    public static object DeImovel(ImovelDto d) => new
    {
        d.Id,
        d.ClienteId,
        d.Nome,
        d.Tipo,
        d.NumeroIptu,
        d.NumeroMatricula,
        d.Status,
        d.CompetenciaInicial,
        d.PropositoLocacao,
        d.DiaVencimentoCobranca,
        d.DespesasCondominiais,
        d.ValorIptu,
        Endereco = d.Endereco,
    };

    public static object DeCobrancaImovel(ImovelDto d) => new
    {
        ImovelId = d.Id,
        d.ClienteId,
        d.CompetenciaInicial,
        d.PropositoLocacao,
        d.DiaVencimentoCobranca,
        d.DespesasCondominiais,
        d.ValorIptu,
    };

    public static object DeCobrancaImovelEntidade(Imovel i) => new
    {
        ImovelId = i.Id,
        i.ClienteId,
        i.CompetenciaInicial,
        PropositoLocacao = i.PropositoLocacao?.ToString(),
        i.DiaVencimentoCobranca,
        i.DespesasCondominiais,
        i.ValorIptu,
    };

    public static object DeInquilino(InquilinoDto d) => new
    {
        d.Id,
        d.ClienteId,
        d.TipoPessoa,
        d.Nome,
        d.Documento,
        d.InscricaoMunicipal,
        d.Telefone,
        d.Email,
        d.Status,
        d.ImovelId,
        Endereco = d.Endereco,
    };

    public static object DeContrato(ContratoDto d) => new
    {
        d.Id,
        d.ClienteId,
        d.ImovelId,
        d.InquilinoId,
        d.NumeroContrato,
        d.DataInicio,
        d.DataFimPrevista,
        d.DiaVencimento,
        d.ValorAluguel,
        d.JurosAtrasoPct,
        d.MultaAtrasoPct,
        d.Status,
        d.AnexoPath,
    };

    public static object DeCliente(ClienteDto d) => new
    {
        d.Id,
        d.TipoPessoa,
        d.PlanoId,
        d.Nome,
        d.Cpf,
        d.DataNascimento,
        d.RazaoSocial,
        d.NomeFantasia,
        d.Cnpj,
        d.InscricaoMunicipal,
        d.CnaePrincipal,
        d.CodigoServico,
        d.CodigoIndicadorOperacao,
        d.ClassificacaoTributaria,
        d.RegimeTributario,
        d.Telefone,
        d.Email,
        d.Status,
        Endereco = d.Endereco,
    };

    public static object DeUsuario(UsuarioDto d) => new
    {
        d.Id,
        d.ClienteId,
        d.Nome,
        d.Email,
        d.Cpf,
        d.Telefone,
        d.Perfil,
        d.Status,
    };

    public static object DeCobranca(CobrancaDetalheDto d) => new
    {
        d.Id,
        d.ClienteId,
        d.ImovelId,
        d.Competencia,
        d.ValorFaturado,
        d.StatusNfse,
        d.NumeroNfse,
        d.TemPagamento,
        d.DataPagamento,
        d.ValorPago,
    };

    public static object DePagamento(Guid nfseId, Guid clienteId, Guid imovelId, string competencia,
        decimal valor, DateOnly data) => new
    {
        NfseId = nfseId,
        ClienteId = clienteId,
        ImovelId = imovelId,
        Competencia = competencia,
        ValorPago = valor,
        DataPagamento = data,
    };
}
