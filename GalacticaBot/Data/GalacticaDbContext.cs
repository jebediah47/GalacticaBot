using Microsoft.EntityFrameworkCore;

namespace GalacticaBot.Data;

public sealed class GalacticaDbContext(DbContextOptions<GalacticaDbContext> options)
    : DbContext(options)
{
    public DbSet<GuildConfigs> GuildConfigs => Set<GuildConfigs>();
    public DbSet<LevelModel> LevelModels => Set<LevelModel>();
    public DbSet<BotConfig> BotConfigs => Set<BotConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // GuildConfigs mapping (from Prisma GuildConfigs)
        modelBuilder.Entity<GuildConfigs>(e =>
        {
            e.ToTable("GuildConfigs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();

            e.Property(x => x.GuildId)
                .HasColumnName("guildID")
                .HasColumnType("numeric(20,0)")
                .IsRequired();

            e.HasIndex(x => x.GuildId).IsUnique();

            e.Property(x => x.GuildName).HasColumnName("guildName").IsRequired();

            e.Property(x => x.DateJoined)
                .HasColumnName("dateJoined")
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp with time zone");

            e.Property(x => x.ModLogsIsEnabled)
                .HasColumnName("modLogsIsEnabled")
                .HasDefaultValue(false);

            e.Property(x => x.ModLogsChannelId)
                .HasColumnName("modLogsChannelID")
                .HasColumnType("numeric(20,0)");

            // Keep unique index (allows a single NULL in PostgreSQL); if you prefer to
            // exclude NULLs add a filtered index in a migration.
            e.HasIndex(x => x.ModLogsChannelId).IsUnique();

            e.Property(x => x.LastUpdated)
                .HasColumnName("lastUpdated")
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp with time zone");

            e.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
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

        // BotConfig mapping
        modelBuilder.Entity<BotConfig>(e =>
        {
            e.ToTable("BotConfig");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();

            // Store enums as ints
            e.Property(x => x.BotStatus)
                .HasColumnName("botStatus")
                .HasConversion<int>()
                .IsRequired();
            e.Property(x => x.BotActivity)
                .HasColumnName("botActivity")
                .HasConversion<int>()
                .IsRequired();

            e.Property(x => x.BotPresence)
                .HasColumnName("botPresence")
                .HasMaxLength(256)
                .HasDefaultValue("")
                .IsRequired();

            e.Property(x => x.LastUpdated)
                .HasColumnName("lastUpdated")
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp with time zone");

            // Ensure single-row table by constraining id to 1 (soft enforcement)
            e.ToTable(
                "BotConfig",
                t =>
                {
                    t.HasCheckConstraint("ck_botconfig_single", "id = 1");
                }
            );
        });
    }
}
