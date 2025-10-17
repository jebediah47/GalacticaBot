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
                name: "GuildConfigs",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
                        ),
                    GuildID = table.Column<string>(type: "text", nullable: false),
                    GuildName = table.Column<string>(type: "text", nullable: false),
                    DateJoined = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "now()"
                    ),
                    ModLogsIsEnabled = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: false
                    ),
                    ModLogsChannelID = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfigs", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "LevelModel",
                columns: table => new
                {
                    Id = table.Column<string>(
                        type: "character varying(36)",
                        maxLength: 36,
                        nullable: false,
                        defaultValueSql: "gen_random_uuid()::text"
                    ),
                    UserID = table.Column<string>(type: "text", nullable: false),
                    GuildID = table.Column<string>(type: "text", nullable: false),
                    Xp = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    Level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastXpMsg = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "now()"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelModel", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_GuildConfigs_GuildID",
                table: "GuildConfigs",
                column: "GuildID",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_GuildConfigs_ModLogsChannelID",
                table: "GuildConfigs",
                column: "ModLogsChannelID",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_LevelModel_UserID_GuildID",
                table: "LevelModel",
                columns: new[] { "UserID", "GuildID" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "GuildConfigs");

            migrationBuilder.DropTable(name: "LevelModel");
        }
    }
}
