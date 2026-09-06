using Aluguel.Application.Abstractions;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using Aluguel.Domain.Contratos;
using Aluguel.Domain.Imoveis;
using Aluguel.Domain.Inquilinos;

namespace Aluguel.Application.UnitTests.Fakes;

public sealed class FakeImovelRepository : IImovelRepository
{
    public List<Imovel> Itens { get; } = [];
    public HashSet<Guid> ImoveisComContratoAtivo { get; } = [];
    public bool PodeAdicionar { get; set; } = true;
    public int SalvouVezes { get; private set; }

    public Task<Imovel?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Itens.FirstOrDefault(i => i.Id == id));

    public Task<IReadOnlyList<Imovel>> ListarAsync(Guid? clienteId, string? termo, TipoImovel? tipo,
        StatusAtivoInativo? status, int skip, int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Imovel>>(
            Itens.Where(i => clienteId == null || i.ClienteId == clienteId).Skip(skip).Take(take).ToList());

    public Task<int> ContarAsync(Guid? clienteId, string? termo, TipoImovel? tipo,
        StatusAtivoInativo? status, CancellationToken ct = default) =>
        Task.FromResult(Itens.Count(i => clienteId == null || i.ClienteId == clienteId));

    public Task<bool> PodeAdicionarImovelAsync(Guid clienteId, CancellationToken ct = default) =>
        Task.FromResult(PodeAdicionar);

    public Task<bool> PossuiContratoAtivoAsync(Guid imovelId, CancellationToken ct = default) =>
        Task.FromResult(ImoveisComContratoAtivo.Contains(imovelId));

    public void Adicionar(Imovel imovel) => Itens.Add(imovel);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default)
    {
        SalvouVezes++;
        return Task.FromResult(1);
    }
}

public sealed class FakeInquilinoRepository : IInquilinoRepository
{
    public List<Inquilino> Itens { get; } = [];
    public HashSet<Guid> InquilinosComContratoAtivo { get; } = [];
    public int SalvouVezes { get; private set; }

    public Task<Inquilino?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Itens.FirstOrDefault(i => i.Id == id));

    public Task<IReadOnlyList<Inquilino>> ListarAsync(Guid? clienteId, string? termo, TipoPessoa? tipoPessoa,
        StatusAtivoInativo? status, int skip, int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Inquilino>>(
            Itens.Where(i => clienteId == null || i.ClienteId == clienteId).Skip(skip).Take(take).ToList());

    public Task<int> ContarAsync(Guid? clienteId, string? termo, TipoPessoa? tipoPessoa,
        StatusAtivoInativo? status, CancellationToken ct = default) =>
        Task.FromResult(Itens.Count(i => clienteId == null || i.ClienteId == clienteId));

    public Task<bool> PossuiContratoAtivoAsync(Guid inquilinoId, CancellationToken ct = default) =>
        Task.FromResult(InquilinosComContratoAtivo.Contains(inquilinoId));

    public void Adicionar(Inquilino inquilino) => Itens.Add(inquilino);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default)
    {
        SalvouVezes++;
        return Task.FromResult(1);
    }
}

public sealed class FakeContratoRepository : IContratoRepository
{
    public List<Contrato> Itens { get; } = [];
    public int SalvouVezes { get; private set; }

    public Task<Contrato?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Itens.FirstOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<Contrato>> ListarAsync(Guid? clienteId, Guid? imovelId, Guid? inquilinoId,
        StatusContrato? status, int skip, int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Contrato>>(
            Itens.Where(c => clienteId == null || c.ClienteId == clienteId).Skip(skip).Take(take).ToList());

    public Task<int> ContarAsync(Guid? clienteId, Guid? imovelId, Guid? inquilinoId,
        StatusContrato? status, CancellationToken ct = default) =>
        Task.FromResult(Itens.Count(c => clienteId == null || c.ClienteId == clienteId));

    public Task<bool> ImovelPossuiContratoAtivoAsync(Guid imovelId, Guid? ignorarId, CancellationToken ct = default) =>
        Task.FromResult(Itens.Any(c => c.ImovelId == imovelId && c.Status == StatusContrato.Ativo
            && (ignorarId == null || c.Id != ignorarId)));

    public void Adicionar(Contrato contrato) => Itens.Add(contrato);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default)
    {
        SalvouVezes++;
        return Task.FromResult(1);
    }
}
