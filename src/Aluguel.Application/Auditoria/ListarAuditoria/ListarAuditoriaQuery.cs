using Aluguel.Application.Abstractions;
using Aluguel.Application.Clientes;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Auditoria.ListarAuditoria;

/// <summary>
/// Lista logs de auditoria do tenant. Administrador pode filtrar por cliente;
/// Gestor/Analista veem apenas o cliente do contexto.
/// </summary>
public sealed record ListarAuditoriaQuery(
    Guid? ClienteId = null,
    string? Tabela = null,
    string? Acao = null,
    DateTimeOffset? De = null,
    DateTimeOffset? Ate = null,
    int Skip = 0,
    int Take = 50) : IRequest<PaginaDto<AuditoriaDto>>;

public sealed class ListarAuditoriaQueryHandler(
    IAuditoriaService auditoria,
    ICurrentUser currentUser)
    : IRequestHandler<ListarAuditoriaQuery, PaginaDto<AuditoriaDto>>
{
    public async Task<PaginaDto<AuditoriaDto>> Handle(
        ListarAuditoriaQuery request, CancellationToken cancellationToken)
    {
        Guid? clienteFiltro;
        if (currentUser.EhAdministrador)
        {
            clienteFiltro = request.ClienteId;
        }
        else
        {
            var escopo = EscopoCliente.Resolver(currentUser, request.ClienteId)
                ?? throw new InvalidOperationException("Cliente do contexto não resolvido.");
            if (!EscopoCliente.PodeAcessar(currentUser, escopo))
                throw new InvalidOperationException("Sem permissão para consultar auditoria deste cliente.");
            clienteFiltro = escopo;
        }

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 100);

        var (itens, total) = await auditoria.ListarAsync(
            clienteFiltro,
            string.IsNullOrWhiteSpace(request.Tabela) ? null : request.Tabela.Trim().ToLowerInvariant(),
            string.IsNullOrWhiteSpace(request.Acao) ? null : request.Acao.Trim(),
            request.De,
            request.Ate,
            skip,
            take,
            cancellationToken);

        var dtos = itens.Select(a => new AuditoriaDto(
            a.Id,
            a.ClienteId,
            a.UsuarioId,
            a.UsuarioNome,
            a.Acao,
            a.Tabela,
            a.RegistroId,
            a.Resumo,
            a.ValoresAntes,
            a.ValoresDepois,
            a.DataHora,
            a.Ip)).ToList();

        return new PaginaDto<AuditoriaDto>(dtos, total, skip, take);
    }
}
