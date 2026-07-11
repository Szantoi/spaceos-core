// SpaceOS.Modules.FlowManagement/Infrastructure/Configurations/FlowProjectConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Abstractions;
using SpaceOS.Modules.FlowManagement.Domain;

namespace SpaceOS.Modules.FlowManagement.Infrastructure.Configurations;

/// <summary>
/// EF Core entity type configuration for <see cref="FlowProject"/>.
/// </summary>
internal sealed class FlowProjectConfiguration : IEntityTypeConfiguration<FlowProject>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<FlowProject> builder)
    {
        builder.ToTable("FlowProjects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.TenantId)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.StartDate);
        builder.Property(p => p.EndDate);

        // Projects may optionally belong to a program — UUID-only reference, no FK.
        builder.Property(p => p.ProgramId);

        // Phase stored as string so enum renames do not require a migration.
        builder.Property(p => p.Phase)
            .HasConversion(
                phase => phase.ToString(),
                value => Enum.Parse<WorkflowPhase>(value))
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(p => p.ProgramId)
            .HasDatabaseName("IX_FlowProjects_ProgramId");
    }
}
