// SpaceOS.Modules.FlowManagement/Infrastructure/Configurations/FlowProgramConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Abstractions;
using SpaceOS.Modules.FlowManagement.Domain;

namespace SpaceOS.Modules.FlowManagement.Infrastructure.Configurations;

/// <summary>
/// EF Core entity type configuration for <see cref="FlowProgram"/>.
/// </summary>
internal sealed class FlowProgramConfiguration : IEntityTypeConfiguration<FlowProgram>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<FlowProgram> builder)
    {
        builder.ToTable("FlowPrograms");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.TenantId)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        // Phase stored as string so enum renames do not require a migration.
        builder.Property(p => p.Phase)
            .HasConversion(
                phase => phase.ToString(),
                value => Enum.Parse<WorkflowPhase>(value))
            .HasMaxLength(50)
            .IsRequired();

        // Programs are owned per-tenant — a tenant may have many programs.
        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("IX_FlowPrograms_TenantId");
    }
}
