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

        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Cpf == "39053344705");
        if (cliente is null)
        {
            var planoNfse = await db.Planos.FirstOrDefaultAsync(p => p.Codigo == PlanoCodigo.Intermediario);
            cliente = Cliente.CriarPessoaFisica(tenant.Id, planoNfse?.Id, "Cliente Demo", "39053344705",
                null, "11999990000", "gestor@demo.local", null);
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync();
        }

        await GarantirUsuarioAsync(userManager, "admin@aluguel.local", "Admin@123456", "Administrador do Sistema",
            tenant.Id, clienteId: null, PerfilUsuario.Administrador, Perfis.Administrador);
        await GarantirUsuarioAsync(userManager, "gestor@demo.local", "Gestor@123456", "Gestor Demo",
            tenant.Id, cliente.Id, PerfilUsuario.Gestor, Perfis.Gestor);
        await GarantirUsuarioAsync(userManager, "analista@demo.local", "Analista@123456", "Analista Demo",
            tenant.Id, cliente.Id, PerfilUsuario.Analista, Perfis.Analista);
    }

    private static async Task GarantirUsuarioAsync(UserManager<AppUser> userManager, string email, string senha,
        string nome, Guid tenantId, Guid? clienteId, PerfilUsuario perfil, string role)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Nome = nome,
            TenantId = tenantId,
            ClienteId = clienteId,
            Perfil = perfil,
            Status = StatusUsuario.Ativo,
        };

        var result = await userManager.CreateAsync(user, senha);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }
}
