using Aluguel.Application.Abstractions;
using Aluguel.Application.Usuarios;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Usuarios;
using Aluguel.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Identity;

/// <summary>
/// Gerencia os usuários do cliente sobre o ASP.NET Identity.
/// A identidade (CPF/e-mail/senha) é única no tenant; cada cliente tem uma vinculação com perfil próprio.
/// </summary>
public class UsuarioService(UserManager<AppUser> userManager, AppDbContext db, ICurrentTenant tenant)
    : IUsuarioService
{
    private Guid? TenantId => tenant.TenantId;

    public async Task<UsuarioDto?> ObterPorIdAsync(Guid id, Guid? clienteId, CancellationToken ct = default)
    {
        var user = await ConsultaUsuarios().AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return null;

        if (user.Perfil == PerfilUsuario.Administrador && clienteId is null)
            return user.ParaDtoAdministrador();

        var vinculoQuery = ConsultaVinculos().AsNoTracking().Where(v => v.UsuarioId == id);
        if (clienteId is { } cid)
            vinculoQuery = vinculoQuery.Where(v => v.ClienteId == cid);

        var vinculo = await vinculoQuery.FirstOrDefaultAsync(ct);
        return vinculo is null ? null : user.ParaDto(vinculo);
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(Guid clienteId, string? termo,
        int skip, int take, CancellationToken ct = default)
    {
        var query =
            from v in ConsultaVinculos().AsNoTracking()
            join u in ConsultaUsuarios().AsNoTracking() on v.UsuarioId equals u.Id
            where v.ClienteId == clienteId
            select new { User = u, Vinculo = v };

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var padrao = $"%{termo.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.User.Nome, padrao) ||
                (x.User.Email != null && EF.Functions.ILike(x.User.Email, padrao)));
        }

        var pares = await query
            .OrderBy(x => x.User.Nome)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return pares.Select(p => p.User.ParaDto(p.Vinculo)).ToList();
    }

    public async Task<int> ContarAsync(Guid clienteId, string? termo, CancellationToken ct = default)
    {
        var query =
            from v in ConsultaVinculos()
            join u in ConsultaUsuarios() on v.UsuarioId equals u.Id
            where v.ClienteId == clienteId
            select u.Id;

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var padrao = $"%{termo.Trim()}%";
            query =
                from v in ConsultaVinculos()
                join u in ConsultaUsuarios() on v.UsuarioId equals u.Id
                where v.ClienteId == clienteId
                      && (EF.Functions.ILike(u.Nome, padrao)
                          || (u.Email != null && EF.Functions.ILike(u.Email, padrao)))
                select u.Id;
        }

        return await query.CountAsync(ct);
    }

    public Task<bool> EmailEmUsoAsync(string email, Guid? ignorarId, CancellationToken ct = default)
    {
        var normalizado = userManager.NormalizeEmail(email);
        return db.Users.AnyAsync(u => u.NormalizedEmail == normalizado && (ignorarId == null || u.Id != ignorarId), ct);
    }

    public async Task<bool> CpfEmUsoNoClienteAsync(Guid clienteId, string cpf, Guid? ignorarId,
        CancellationToken ct = default)
    {
        return await (
            from v in ConsultaVinculos()
            join u in ConsultaUsuarios() on v.UsuarioId equals u.Id
            where v.ClienteId == clienteId && v.Status == StatusUsuario.Ativo
                  && u.Cpf == cpf && (ignorarId == null || u.Id != ignorarId)
            select v.Id).AnyAsync(ct);
    }

    public async Task<UsuarioPorCpfDto?> ObterPorCpfAsync(string cpf, Guid clienteId, CancellationToken ct = default)
    {
        var user = await ConsultaUsuarios().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Cpf == cpf, ct);
        if (user is null)
            return null;

        var jaNoCliente = await ConsultaVinculos()
            .AnyAsync(v => v.UsuarioId == user.Id && v.ClienteId == clienteId && v.Status == StatusUsuario.Ativo, ct);

        return new UsuarioPorCpfDto(user.Id, user.Nome, user.Email ?? string.Empty, user.Telefone, user.Cpf ?? cpf,
            jaNoCliente);
    }

    public async Task<bool> PodeAdicionarUsuarioAsync(Guid clienteId, CancellationToken ct = default)
    {
        var plano = await ObterPlanoDoClienteAsync(clienteId, ct);
        if (plano is null)
            return false;

        var ativos = await ContarAtivosAsync(clienteId, ct);
        return plano.PermiteMaisUsuarios(ativos);
    }

    public async Task<UsuarioDto> CriarAsync(NovoUsuario dados, CancellationToken ct = default)
    {
        if (!await PodeAdicionarUsuarioAsync(dados.ClienteId, ct))
            throw new InvalidOperationException("Limite de usuários do plano atingido.");

        if (string.IsNullOrWhiteSpace(dados.Cpf))
            throw new InvalidOperationException("CPF é obrigatório.");

        var existente = await ConsultaUsuarios().FirstOrDefaultAsync(u => u.Cpf == dados.Cpf, ct);
        if (existente is not null)
            return await VincularExistenteAsync(existente, dados, ct);

        return await CriarIdentidadeAsync(dados, ct);
    }

    public async Task<UsuarioDto?> AtualizarAsync(Guid id, Guid clienteId, AtualizacaoUsuario dados,
        CancellationToken ct = default)
    {
        var user = await ConsultaUsuarios().FirstOrDefaultAsync(u => u.Id == id, ct);
        var vinculo = await ConsultaVinculos()
            .FirstOrDefaultAsync(v => v.UsuarioId == id && v.ClienteId == clienteId, ct);
        if (user is null || vinculo is null)
            return null;

        if (dados.Status == StatusUsuario.Ativo && vinculo.Status != StatusUsuario.Ativo)
        {
            var plano = await ObterPlanoDoClienteAsync(clienteId, ct);
            var ativos = await ContarAtivosAsync(clienteId, ct);
            if (plano is null || !plano.PermiteMaisUsuarios(ativos))
                throw new InvalidOperationException("Limite de usuários do plano atingido.");
        }

        user.Nome = dados.Nome;
        user.Telefone = dados.Telefone;
        vinculo.Atualizar(dados.Perfil, dados.Status);

        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
            throw new InvalidOperationException(DescreverErros(update));

        await SincronizarStatusIdentidadeAsync(user, ct);
        await db.SaveChangesAsync(ct);
        return user.ParaDto(vinculo);
    }

    public async Task<bool> RemoverAsync(Guid id, Guid clienteId, CancellationToken ct = default)
    {
        var user = await ConsultaUsuarios().FirstOrDefaultAsync(u => u.Id == id, ct);
        var vinculo = await ConsultaVinculos()
            .FirstOrDefaultAsync(v => v.UsuarioId == id && v.ClienteId == clienteId, ct);
        if (user is null || vinculo is null)
            return false;

        vinculo.Inativar();
        await SincronizarStatusIdentidadeAsync(user, ct);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<UsuarioDto> VincularExistenteAsync(AppUser user, NovoUsuario dados, CancellationToken ct)
    {
        var vinculo = await ConsultaVinculos()
            .FirstOrDefaultAsync(v => v.UsuarioId == user.Id && v.ClienteId == dados.ClienteId, ct);
        if (vinculo is { Status: StatusUsuario.Ativo })
            throw new InvalidOperationException("Já existe um usuário com este CPF neste cliente.");

        if (vinculo is null)
        {
            vinculo = new UsuarioCliente(dados.TenantId, user.Id, dados.ClienteId, dados.Perfil);
            db.UsuariosClientes.Add(vinculo);
        }
        else
            vinculo.Atualizar(dados.Perfil, StatusUsuario.Ativo);

        if (user.Status != StatusUsuario.Ativo)
        {
            user.Status = StatusUsuario.Ativo;
            await userManager.UpdateAsync(user);
        }

        await db.SaveChangesAsync(ct);
        return user.ParaDto(vinculo);
    }

    private async Task<UsuarioDto> CriarIdentidadeAsync(NovoUsuario dados, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dados.Senha))
            throw new InvalidOperationException("Senha é obrigatória para o primeiro cadastro do usuário.");

        var user = new AppUser
        {
            UserName = dados.Email,
            Email = dados.Email,
            EmailConfirmed = true,
            TenantId = dados.TenantId,
            ClienteId = dados.ClienteId,
            Nome = dados.Nome,
            Cpf = dados.Cpf,
            Telefone = dados.Telefone,
            Perfil = dados.Perfil,
            Status = StatusUsuario.Ativo,
        };

        var resultado = await userManager.CreateAsync(user, dados.Senha);
        if (!resultado.Succeeded)
            throw new InvalidOperationException(DescreverErros(resultado));

        var role = dados.Perfil.ToString();
        var addRole = await userManager.AddToRoleAsync(user, role);
        if (!addRole.Succeeded)
            throw new InvalidOperationException(DescreverErros(addRole));

        var vinculo = new UsuarioCliente(dados.TenantId, user.Id, dados.ClienteId, dados.Perfil);
        db.UsuariosClientes.Add(vinculo);
        await db.SaveChangesAsync(ct);
        return user.ParaDto(vinculo);
    }

    private async Task SincronizarStatusIdentidadeAsync(AppUser user, CancellationToken ct)
    {
        if (user.Perfil == PerfilUsuario.Administrador)
            return;

        var temAtivo = await ConsultaVinculos()
            .AnyAsync(v => v.UsuarioId == user.Id && v.Status == StatusUsuario.Ativo, ct);
        var desejado = temAtivo ? StatusUsuario.Ativo : StatusUsuario.Inativo;
        if (user.Status == desejado)
            return;
        user.Status = desejado;
        await userManager.UpdateAsync(user);
    }

    private IQueryable<AppUser> ConsultaUsuarios() =>
        db.Users.Where(u => TenantId == null || u.TenantId == TenantId);

    private IQueryable<UsuarioCliente> ConsultaVinculos() =>
        db.UsuariosClientes.Where(v => TenantId == null || v.TenantId == TenantId);

    private Task<int> ContarAtivosAsync(Guid clienteId, CancellationToken ct) =>
        ConsultaVinculos().CountAsync(v => v.ClienteId == clienteId && v.Status == StatusUsuario.Ativo, ct);

    private async Task<Plano?> ObterPlanoDoClienteAsync(Guid clienteId, CancellationToken ct)
    {
        var planoId = await db.Clientes
            .Where(c => c.Id == clienteId)
            .Select(c => c.PlanoId)
            .FirstOrDefaultAsync(ct);

        return planoId is null ? null : await db.Planos.FirstOrDefaultAsync(p => p.Id == planoId, ct);
    }

    private static string DescreverErros(IdentityResult resultado) =>
        string.Join("; ", resultado.Errors.Select(e => e.Description));
}
