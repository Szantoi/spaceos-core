// SpaceOS.Modules.FlowManagement/Infrastructure/Configurations/FlowMilestoneConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Abstractions;
using SpaceOS.Modules.FlowManagement.Domain;

namespace SpaceOS.Modules.FlowManagement.Infrastructure.Configurations;

/// <summary>
/// EF Core entity type configuration for <see cref="FlowMilestone"/>.
/// </summary>
internal sealed class FlowMilestoneConfiguration : IEntityTypeConfiguration<FlowMilestone>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<FlowMilestone> builder)
    {
        builder.ToTable("FlowMilestones");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.TenantId)
            .IsRequired();

        builder.Property(m => m.ProjectId)
            .IsRequired();

        builder.Property(m => m.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.TargetDate);

        // Phase stored as string so enum renames do not require a migration.
        builder.Property(m => m.Phase)
            .HasConversion(
                phase => phase.ToString(),
                value => Enum.Parse<WorkflowPhase>(value))
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(m => m.ProjectId)
            .HasDatabaseName("IX_FlowMilestones_ProjectId");
    }
}
