using Microsoft.EntityFrameworkCore;
using PrivateRagnarok.Api.Models;
namespace PrivateRagnarok.Api.Data;
public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<PlayerAccount> Players => Set<PlayerAccount>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var p = modelBuilder.Entity<PlayerAccount>();
        p.HasKey(x => x.Id);
        p.HasIndex(x => x.NormalizedUsername).IsUnique();
        p.Property(x => x.Username).HasMaxLength(24).IsRequired();
        p.Property(x => x.NormalizedUsername).HasMaxLength(24).IsRequired();
        p.Property(x => x.PasswordHash).IsRequired();
    }
}
