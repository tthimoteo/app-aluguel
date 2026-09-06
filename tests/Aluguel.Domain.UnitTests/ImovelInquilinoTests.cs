using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using Aluguel.Domain.Imoveis;
using Aluguel.Domain.Inquilinos;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class ImovelInquilinoTests
{
    [Fact]
    public void Imovel_nasce_ativo()
    {
        var i = new Imovel(Guid.NewGuid(), Guid.NewGuid(), "Apto 101", TipoImovel.Residencial);
        i.Status.Should().Be(StatusAtivoInativo.Ativo);
    }

    [Fact]
    public void Imovel_sem_nome_lanca()
    {
        var act = () => new Imovel(Guid.NewGuid(), Guid.NewGuid(), "  ", TipoImovel.Comercial);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Imovel_inativar_e_ativar()
    {
        var i = new Imovel(Guid.NewGuid(), Guid.NewGuid(), "Loja 1", TipoImovel.Comercial);
        i.Inativar();
        i.Status.Should().Be(StatusAtivoInativo.Inativo);
        i.Ativar();
        i.Status.Should().Be(StatusAtivoInativo.Ativo);
    }

    [Fact]
    public void Imovel_remover_marca_deleted_e_inativa()
    {
        var i = new Imovel(Guid.NewGuid(), Guid.NewGuid(), "Sala 2", TipoImovel.Sala);
        i.Remover();
        i.DeletedAt.Should().NotBeNull();
        i.Status.Should().Be(StatusAtivoInativo.Inativo);
    }

    [Fact]
    public void Imovel_atualizar_troca_dados()
    {
        var i = new Imovel(Guid.NewGuid(), Guid.NewGuid(), "Antigo", TipoImovel.Residencial);
        i.Atualizar("Novo Nome", TipoImovel.Galpao, null, "IPTU-9", "MAT-9");
        i.Nome.Should().Be("Novo Nome");
        i.Tipo.Should().Be(TipoImovel.Galpao);
        i.NumeroIptu.Should().Be("IPTU-9");
    }

    [Fact]
    public void Inquilino_sem_documento_lanca()
    {
        var act = () => new Inquilino(Guid.NewGuid(), Guid.NewGuid(), TipoPessoa.PF, "João", "  ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Inquilino_atualizar_preserva_documento_e_tipo()
    {
        var i = new Inquilino(Guid.NewGuid(), Guid.NewGuid(), TipoPessoa.PF, "João", "39053344705");
        i.Atualizar("João Silva", "IM-1", "1133334444", "joao@demo.local", null);
        i.Nome.Should().Be("João Silva");
        i.TipoPessoa.Should().Be(TipoPessoa.PF);
        i.Documento.Should().Be("39053344705");
    }
}
