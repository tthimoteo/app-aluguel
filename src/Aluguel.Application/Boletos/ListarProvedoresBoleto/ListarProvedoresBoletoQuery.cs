using MediatR;

namespace Aluguel.Application.Boletos.ListarProvedoresBoleto;

public sealed record ListarProvedoresBoletoQuery : IRequest<IReadOnlyList<ProvedorBoletoDto>>;

public sealed class ListarProvedoresBoletoQueryHandler
    : IRequestHandler<ListarProvedoresBoletoQuery, IReadOnlyList<ProvedorBoletoDto>>
{
    public Task<IReadOnlyList<ProvedorBoletoDto>> Handle(
        ListarProvedoresBoletoQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ProvedorBoletoDto> lista = ProvedorBoletoCatalogo.Todos
            .Select(p => new ProvedorBoletoDto(
                p.Codigo,
                p.Nome,
                p.CodigoBanco,
                p.Campos.Select(c => new CampoProvedorBoletoDto(
                    c.Chave,
                    c.Rotulo,
                    c.Tipo.ToString(),
                    c.Obrigatorio,
                    c.Placeholder,
                    c.Ajuda)).ToList()))
            .ToList();
        return Task.FromResult(lista);
    }
}
