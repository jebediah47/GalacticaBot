using Microsoft.EntityFrameworkCore;

namespace GalacticaBot.Data;

public sealed class GalacticaDbContext(DbContextOptions<GalacticaDbContext> options)
    : DbContext(options)
{
    public DbSet<GuildConfigs> GuildConfigs => Set<GuildConfigs>();
    public DbSet<LevelModel> LevelModels => Set<LevelModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // GuildConfigs mapping (from Prisma GuildConfigs)
        modelBuilder.Entity<GuildConfigs>(e =>
        {
            e.ToTable("GuildConfigs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();

            e.Property(x => x.GuildID)
                .HasColumnName("guildID")
                .HasColumnType("numeric(20,0)")
                .IsRequired();

            e.HasIndex(x => x.GuildID).IsUnique();

            e.Property(x => x.GuildName).HasColumnName("guildName").IsRequired();

            e.Property(x => x.DateJoined)
                .HasColumnName("dateJoined")
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp with time zone");

            e.Property(x => x.ModLogsIsEnabled)
                .HasColumnName("modLogsIsEnabled")
                .HasDefaultValue(false);

            e.Property(x => x.ModLogsChannelID).HasColumnName("modLogsChannelID");

            e.HasIndex(x => x.ModLogsChannelID).IsUnique();
        });

        // LevelModel mapping (from Prisma LevelModel)
        modelBuilder.Entity<LevelModel>(e =>
        {
            e.ToTable("LevelModel");
            e.HasKey(x => x.Id);

            // In Prisma: id String @id @default(uuid())
            // In PostgreSQL we can generate it on the server using gen_random_uuid()::text (pgcrypto)
            e.Property(x => x.Id)
                .HasColumnName("id")
                .HasMaxLength(36)
                .HasDefaultValueSql("gen_random_uuid()::text");

            e.Property(x => x.UserID)
                .HasColumnName("userID")
                .HasColumnType("numeric(20,0)")
                .IsRequired();
            e.Property(x => x.GuildID)
                .HasColumnName("guildID")
                .HasColumnType("numeric(20,0)")
                .IsRequired();

            e.Property(x => x.Xp).HasColumnName("xp").HasColumnType("bigint").HasDefaultValue(0L);

            e.Property(x => x.Level).HasColumnName("level").HasDefaultValue(0);

            e.Property(x => x.LastXpMsg)
                .HasColumnName("lastXpMsg")
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp with time zone");

            // Unique composite index (userID, guildID)
            e.HasIndex(x => new { x.UserID, x.GuildID }).IsUnique();

            // Additional non-unique index is redundant when unique exists in PostgreSQL, so we omit it.
        });
    }
}
