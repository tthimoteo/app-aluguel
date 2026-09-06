using Aluguel.Application.Abstractions;
using Aluguel.Application.Usuarios;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Usuarios;
using Aluguel.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Identity;

/// <summary>
/// Gerencia os usuários do cliente sobre o ASP.NET Identity (senha via PasswordHasher, roles via UserManager).
/// Todas as leituras são escopadas pelo tenant corrente; o limite de usuários segue o plano do cliente (UC002).
/// </summary>
public class UsuarioService(UserManager<AppUser> userManager, AppDbContext db, ICurrentTenant tenant)
    : IUsuarioService
{
    private Guid? TenantId => tenant.TenantId;

    public async Task<UsuarioDto?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await ConsultaNoTenant().AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        return user?.ParaDto();
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(Guid clienteId, string? termo,
        int skip, int take, CancellationToken ct = default)
    {
        var itens = await Filtrar(clienteId, termo)
            .AsNoTracking()
            .OrderBy(u => u.Nome)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return itens.Select(u => u.ParaDto()).ToList();
    }

    public Task<int> ContarAsync(Guid clienteId, string? termo, CancellationToken ct = default) =>
        Filtrar(clienteId, termo).CountAsync(ct);

    public Task<bool> EmailEmUsoAsync(string email, Guid? ignorarId, CancellationToken ct = default)
    {
        var normalizado = userManager.NormalizeEmail(email);
        return db.Users.AnyAsync(u => u.NormalizedEmail == normalizado && (ignorarId == null || u.Id != ignorarId), ct);
    }

    public Task<bool> CpfEmUsoNoClienteAsync(Guid clienteId, string cpf, Guid? ignorarId, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.ClienteId == clienteId && u.Cpf == cpf && (ignorarId == null || u.Id != ignorarId), ct);

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
        // Revalidação defensiva do limite do plano (a validação de entrada roda no pipeline do MediatR).
        if (!await PodeAdicionarUsuarioAsync(dados.ClienteId, ct))
            throw new InvalidOperationException("Limite de usuários do plano atingido.");

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

        return user.ParaDto();
    }

    public async Task<UsuarioDto?> AtualizarAsync(Guid id, AtualizacaoUsuario dados, CancellationToken ct = default)
    {
        var user = await ConsultaNoTenant().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return null;

        // Reativar um usuário também respeita o limite do plano.
        if (dados.Status == StatusUsuario.Ativo && user.Status != StatusUsuario.Ativo && user.ClienteId is { } cid)
        {
            var plano = await ObterPlanoDoClienteAsync(cid, ct);
            var ativos = await ContarAtivosAsync(cid, ct);
            if (plano is null || !plano.PermiteMaisUsuarios(ativos))
                throw new InvalidOperationException("Limite de usuários do plano atingido.");
        }

        var perfilAnterior = user.Perfil;

        user.Nome = dados.Nome;
        user.Telefone = dados.Telefone;
        user.Perfil = dados.Perfil;
        user.Status = dados.Status;

        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
            throw new InvalidOperationException(DescreverErros(update));

        if (perfilAnterior != dados.Perfil)
        {
            await userManager.RemoveFromRoleAsync(user, perfilAnterior.ToString());
            await userManager.AddToRoleAsync(user, dados.Perfil.ToString());
        }

        return user.ParaDto();
    }

    public async Task<bool> RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var user = await ConsultaNoTenant().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return false;

        if (user.Status != StatusUsuario.Inativo)
        {
            user.Status = StatusUsuario.Inativo;
            await db.SaveChangesAsync(ct);
        }

        return true;
    }

    private IQueryable<AppUser> ConsultaNoTenant() =>
        db.Users.Where(u => TenantId == null || u.TenantId == TenantId);

    private IQueryable<AppUser> Filtrar(Guid clienteId, string? termo)
    {
        var query = ConsultaNoTenant().Where(u => u.ClienteId == clienteId);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var padrao = $"%{termo.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Nome, padrao) ||
                (u.Email != null && EF.Functions.ILike(u.Email, padrao)));
        }

        return query;
    }

    private Task<int> ContarAtivosAsync(Guid clienteId, CancellationToken ct) =>
        db.Users.CountAsync(u => u.ClienteId == clienteId && u.Status == StatusUsuario.Ativo, ct);

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
