using Microsoft.EntityFrameworkCore;
using PrivateRagnarok.Api.Models;

namespace PrivateRagnarok.Api.Data;

public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<PlayerAccount> Players => Set<PlayerAccount>();
    public DbSet<PlayerCharacter> Characters => Set<PlayerCharacter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var p = modelBuilder.Entity<PlayerAccount>();
        p.HasKey(x => x.Id);
        p.HasIndex(x => x.NormalizedUsername).IsUnique();
        p.Property(x => x.Username).HasMaxLength(24).IsRequired();
        p.Property(x => x.NormalizedUsername).HasMaxLength(24).IsRequired();
        p.Property(x => x.PasswordHash).IsRequired();

        var c = modelBuilder.Entity<PlayerCharacter>();
        c.ToTable("PlayerCharacters");
        c.HasKey(x => x.PlayerId);
        c.Property(x => x.CharacterName).HasMaxLength(24).IsRequired();
        c.Property(x => x.JobClass).HasMaxLength(24).IsRequired();
        c.Property(x => x.Level).HasDefaultValue(1);
        c.Property(x => x.BaseExperience).HasDefaultValue(0L);
        c.HasOne<PlayerAccount>().WithOne().HasForeignKey<PlayerCharacter>(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
