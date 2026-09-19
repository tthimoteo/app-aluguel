using Aluguel.Domain.Usuarios;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class UsuarioClienteTests
{
    [Fact]
    public void Vinculo_nasce_ativo_com_perfil_do_cliente()
    {
        var vinculo = new UsuarioCliente(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PerfilUsuario.Gestor);
        vinculo.Status.Should().Be(StatusUsuario.Ativo);
        vinculo.Perfil.Should().Be(PerfilUsuario.Gestor);
    }

    [Fact]
    public void Perfil_administrador_nao_e_permitido_na_vinculacao()
    {
        var act = () => new UsuarioCliente(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PerfilUsuario.Administrador);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Inativar_nao_apaga_a_identidade_apenas_o_vinculo()
    {
        var vinculo = new UsuarioCliente(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PerfilUsuario.Analista);
        vinculo.Inativar();
        vinculo.Status.Should().Be(StatusUsuario.Inativo);
        vinculo.UsuarioId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Atualizar_troca_perfil_por_cliente()
    {
        var vinculo = new UsuarioCliente(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PerfilUsuario.Analista);
        vinculo.Atualizar(PerfilUsuario.Gestor, StatusUsuario.Ativo);
        vinculo.Perfil.Should().Be(PerfilUsuario.Gestor);
    }
}
