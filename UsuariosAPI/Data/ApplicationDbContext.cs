using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UsuariosAPI.Models;

namespace UsuariosAPI.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(u => u.Estado).HasDefaultValue(true);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(r => r.Estado).HasDefaultValue(true);
        });
    }
}
