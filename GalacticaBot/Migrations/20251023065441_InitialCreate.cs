using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GalacticaBot.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotConfig",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    botStatus = table.Column<int>(type: "integer", nullable: false),
                    botPresence = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: false,
                        defaultValue: ""
                    ),
                    botActivity = table.Column<int>(type: "integer", nullable: false),
                    lastUpdated = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "now()"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bot_config", x => x.id);
                    table.CheckConstraint("ck_botconfig_single", "id = 1");
                }
            );

            migrationBuilder.CreateTable(
                name: "GuildConfigs",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
                        ),
                    guildID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guildName = table.Column<string>(type: "text", nullable: false),
                    dateJoined = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "now()"
                    ),
                    modLogsIsEnabled = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: false
                    ),
                    modLogsChannelID = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    lastUpdated = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "now()"
                    ),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guild_configs", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "LevelModel",
                columns: table => new
                {
                    id = table.Column<string>(
                        type: "character varying(36)",
                        maxLength: 36,
                        nullable: false,
                        defaultValueSql: "gen_random_uuid()::text"
                    ),
                    userID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guildID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    xp = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    lastXpMsg = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "now()"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_level_model", x => x.id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_guild_configs_guild_id",
                table: "GuildConfigs",
                column: "guildID",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_guild_configs_mod_logs_channel_id",
                table: "GuildConfigs",
                column: "modLogsChannelID",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_level_model_user_id_guild_id",
                table: "LevelModel",
                columns: new[] { "userID", "guildID" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BotConfig");

            migrationBuilder.DropTable(name: "GuildConfigs");

            migrationBuilder.DropTable(name: "LevelModel");
        }
    }
}
