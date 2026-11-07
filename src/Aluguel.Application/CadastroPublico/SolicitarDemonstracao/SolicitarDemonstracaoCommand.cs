using MediatR;

namespace Aluguel.Application.CadastroPublico.SolicitarDemonstracao;

/// <summary>Lead comercial do plano Pro (sob consulta).</summary>
public sealed record SolicitarDemonstracaoCommand(
    string Nome,
    string Email,
    string Telefone,
    string? Mensagem) : IRequest<SolicitarDemonstracaoResultadoDto>;

public sealed record SolicitarDemonstracaoResultadoDto(bool Ok, string Mensagem);

public sealed class SolicitarDemonstracaoCommandHandler
    : IRequestHandler<SolicitarDemonstracaoCommand, SolicitarDemonstracaoResultadoDto>
{
    public Task<SolicitarDemonstracaoResultadoDto> Handle(
        SolicitarDemonstracaoCommand request, CancellationToken cancellationToken)
    {
        var nome = request.Nome?.Trim() ?? "";
        var email = request.Email?.Trim() ?? "";
        var telefone = request.Telefone?.Trim() ?? "";

        if (nome.Length < 3)
            throw new InvalidOperationException("Informe o nome completo.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new InvalidOperationException("Informe um e-mail válido.");
        if (telefone.Length < 8)
            throw new InvalidOperationException("Informe um telefone de contato.");

        return Task.FromResult(new SolicitarDemonstracaoResultadoDto(
            true,
            "Recebemos seu pedido. A equipe Lucrare entrará em contato em breve."));
    }
}
