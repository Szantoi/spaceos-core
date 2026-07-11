using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SpaceOS.Modules.Sales.Infrastructure.Persistence;

#nullable disable
#pragma warning disable CS8981

namespace SpaceOS.Modules.Sales.Infrastructure.Persistence.Migrations;

/// <summary>
/// EF Core model snapshot. Raw SQL migrations (S-0001/S-0002/S-0003) manage the schema;
/// this snapshot is intentionally empty to satisfy the EF tooling contract.
/// </summary>
[DbContext(typeof(SalesDbContext))]
partial class SalesDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        // Raw SQL migrations are used — EF model snapshot not auto-generated.
    }
}
