using Aluguel.Application.Autorizacao;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Tenancy;
using Aluguel.Domain.Usuarios;
using Aluguel.Infrastructure.Identity;
using Aluguel.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Api.Setup;

/// <summary>Cria as roles do Identity e, em Development, usuários demo para testes de ponta a ponta.</summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider sp, bool seedDemoUsers)
    {
        using var scope = sp.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var role in Perfis.Todos)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new AppRole { Name = role });

        if (!seedDemoUsers)
            return;

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Subdominio == "demo");
        if (tenant is null)
        {
            tenant = new Tenant("Imobiliária Demo", "demo");
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }

        var planoNfse = await db.Planos.FirstOrDefaultAsync(p => p.Codigo == PlanoCodigo.Intermediario);

        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Cpf == "39053344705");
        if (cliente is null)
        {
            cliente = Cliente.CriarPessoaFisica(tenant.Id, planoNfse?.Id, "Cliente Demo", "39053344705",
                null, "11999990000", "gestor@demo.local", null);
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync();
        }

        var extra = await db.Clientes.FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Cpf == "11144477735");
        if (extra is null)
        {
            extra = Cliente.CriarPessoaFisica(tenant.Id, planoNfse?.Id, "Cliente Extra", "11144477735",
                null, "11999990001", "extra@demo.local", null);
            db.Clientes.Add(extra);
            await db.SaveChangesAsync();
        }

        await GarantirUsuarioAsync(userManager, db, "admin@aluguel.local", "Admin@123456", "Administrador do Sistema",
            tenant.Id, clienteId: null, cpf: null, PerfilUsuario.Administrador, Perfis.Administrador);
        await GarantirUsuarioAsync(userManager, db, "gestor@demo.local", "Gestor@123456", "Gestor Demo",
            tenant.Id, cliente.Id, "39053344705", PerfilUsuario.Gestor, Perfis.Gestor);
        await GarantirUsuarioAsync(userManager, db, "analista@demo.local", "Analista@123456", "Analista Demo",
            tenant.Id, cliente.Id, "52998224725", PerfilUsuario.Analista, Perfis.Analista);
    }

    private static async Task GarantirUsuarioAsync(UserManager<AppUser> userManager, AppDbContext db, string email,
        string senha, string nome, Guid tenantId, Guid? clienteId, string? cpf, PerfilUsuario perfil, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Nome = nome,
                TenantId = tenantId,
                ClienteId = clienteId,
                Cpf = cpf,
                Perfil = perfil,
                Status = StatusUsuario.Ativo,
            };

            var result = await userManager.CreateAsync(user, senha);
            if (!result.Succeeded)
                return;
            await userManager.AddToRoleAsync(user, role);
        }
        else if (cpf is not null && user.Cpf != cpf)
        {
            var cpfEmUso = await db.Users.AnyAsync(u => u.Cpf == cpf && u.Id != user.Id);
            if (!cpfEmUso)
            {
                user.Cpf = cpf;
                await userManager.UpdateAsync(user);
            }
        }

        if (clienteId is { } cid && perfil is PerfilUsuario.Gestor or PerfilUsuario.Analista)
            await GarantirVinculoAsync(db, tenantId, user.Id, cid, perfil);
    }

    private static async Task GarantirVinculoAsync(AppDbContext db, Guid tenantId, Guid usuarioId, Guid clienteId,
        PerfilUsuario perfil)
    {
        var existe = await db.UsuariosClientes.AnyAsync(v => v.UsuarioId == usuarioId && v.ClienteId == clienteId);
        if (existe)
            return;

        db.UsuariosClientes.Add(new UsuarioCliente(tenantId, usuarioId, clienteId, perfil));
        await db.SaveChangesAsync();
    }
}
