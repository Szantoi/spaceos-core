using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Joinery.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Migration_0002_SeedStaticData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // spaceos is the table owner but has had INSERT revoked from itself (per DB-02).
        // The owner can always re-grant its own privileges → seed → revoke.
        migrationBuilder.Sql(@"GRANT INSERT ON spaceos_joinery.""GlobalConstants"" TO spaceos;");
        migrationBuilder.Sql(@"
            INSERT INTO spaceos_joinery.""GlobalConstants"" (""Key"", ""Value"")
            VALUES
                ('CuttingOversize',  1.0),
                ('CladdingOverhang', 0.2),
                ('MatyiWidth',       4.6)
            ON CONFLICT (""Key"") DO NOTHING;
        ");
        migrationBuilder.Sql(@"REVOKE INSERT, UPDATE, DELETE ON spaceos_joinery.""GlobalConstants"" FROM spaceos;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"GRANT INSERT ON spaceos_joinery.""GlobalConstants"" TO spaceos;");
        migrationBuilder.Sql(@"
            DELETE FROM spaceos_joinery.""GlobalConstants""
            WHERE ""Key"" IN ('CuttingOversize', 'CladdingOverhang', 'MatyiWidth');
        ");
        migrationBuilder.Sql(@"REVOKE INSERT, UPDATE, DELETE ON spaceos_joinery.""GlobalConstants"" FROM spaceos;");
    }
}
