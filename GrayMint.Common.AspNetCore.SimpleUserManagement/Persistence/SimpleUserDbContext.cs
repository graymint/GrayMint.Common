using GrayMint.Common.AspNetCore.SimpleUserManagement.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Persistence;

// ReSharper disable once PartialTypeWithSinglePart
public partial class SimpleUserDbContext : DbContext
{
    public const string Schema = "smuser";

    public virtual DbSet<User> Users { get; set; } = default!;
    public virtual DbSet<Role> Roles { get; set; } = default!;
    public virtual DbSet<UserRole> UserRoles { get; set; } = default!;

    public SimpleUserDbContext()
    {
    }

    public SimpleUserDbContext(DbContextOptions<SimpleUserDbContext> options)
        : base(options)
    {
    }

    public async Task EnsureTablesCreated()
    {
        try
        {
            var databaseCreator = (RelationalDatabaseCreator)Database.GetService<IDatabaseCreator>();
            await databaseCreator.CreateTablesAsync();

        }
        catch (SqlException ex) when(ex.Number==2714) // already exists exception
        { 
            // ignore
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.UserId)
                .HasMaxLength(50);

            entity.HasKey(e => e.UserId);

            entity.HasIndex(e => e.Email)
                .IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(e => e.RoleId)
                .HasMaxLength(50);

            entity.HasKey(e => e.RoleId);

            entity.HasIndex(e => e.RoleName)
                .IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
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