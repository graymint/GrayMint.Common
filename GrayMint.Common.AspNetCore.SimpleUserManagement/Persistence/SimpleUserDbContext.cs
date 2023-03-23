using GrayMint.Common.AspNetCore.SimpleUserManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Persistence;

// ReSharper disable once PartialTypeWithSinglePart
public partial class SimpleUserDbContext : DbContext
{
    public const string Schema = "smuser";

    internal virtual DbSet<UserModel> Users { get; set; } = default!;
    internal virtual DbSet<RoleModel> Roles { get; set; } = default!;
    internal virtual DbSet<UserRoleModel> UserRoles { get; set; } = default!;

    public SimpleUserDbContext()
    {
    }

    public SimpleUserDbContext(DbContextOptions<SimpleUserDbContext> options)
        : base(options)
    {
    }

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>()
            .HaveMaxLength(400);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<UserModel>(entity =>
        {
            entity.HasKey(x => x.UserId);

            entity.Property(e => e.UserId)
                .HasMaxLength(50);

            entity.Property(e => e.IsDisabled)
                .HasDefaultValue(false);

            entity.HasKey(e => e.UserId);

            entity.HasIndex(e => e.Email)
                .IsUnique();
        });

        modelBuilder.Entity<RoleModel>(entity =>
        {
            entity.HasKey(e => e.RoleId);

            entity.HasIndex(e => e.RoleName)
                .IsUnique();

            entity.Property(e => e.RoleId)
                .HasMaxLength(50);
        });

        modelBuilder.Entity<UserRoleModel>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId, e.AppId });

            entity.HasOne(e => e.Role)
                .WithMany(d=>d.UserRoles)
                .HasForeignKey(e => e.RoleId);

            entity.HasOne(e => e.User)
                .WithMany(d=>d.UserRoles)
                .HasForeignKey(e => e.UserId);
        });

        // ReSharper disable once InvocationIsSkipped
        OnModelCreatingPartial(modelBuilder);
    }

    // ReSharper disable once PartialMethodWithSinglePart
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}