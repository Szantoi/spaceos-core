// SpaceOS.Infrastructure/Migrations/20260406200000_Migration_0013_AddRefreshTokens.cs

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0013 — Adds the RefreshTokens table for opaque refresh token storage (BE-P15-02).
/// Tokens are stored as SHA-256 hex hashes — the plaintext token is never persisted.
/// </summary>
public partial class Migration_0013_AddRefreshTokens : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id        = table.Column<Guid>(type: "uuid", nullable: false),
                UserId    = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_ExpiresAt",
            table: "RefreshTokens",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            table: "RefreshTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "UQ_RefreshTokens_TokenHash",
            table: "RefreshTokens",
            column: "TokenHash",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RefreshTokens");
    }
}
