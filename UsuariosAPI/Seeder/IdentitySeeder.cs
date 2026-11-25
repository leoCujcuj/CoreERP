using System;
using Microsoft.AspNetCore.Identity;
using UsuariosAPI.Models;

namespace UsuariosAPI.Seeder;

public class IdentitySeeder
{
    /// <summary>
    /// Seeds default roles and an admin user into the identity database if they do not exist.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve identity services.</param>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var roles = new List<ApplicationRole>
        {
            new ApplicationRole { Name = "Admin" },
            new ApplicationRole { Name = "User" }
        };

        foreach (var role in roles)
        {
            var roleExists = await roleManager.RoleExistsAsync(role.Name!);
            if (!roleExists)
            {
                await roleManager.CreateAsync(role);
            }
        }

        var adminEmail = "admin@core.com";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin123*!";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                Nombre = "Administrador del Sistema",
                EmailConfirmed = true,
                Estado = true,
                FechaCreacion = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Error al crear el usuario administrador '{adminEmail}': {errors}");
            }
        }
    }
}
