// SpaceOS.Modules.FlowManagement/Infrastructure/Configurations/FlowTaskConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Abstractions;
using SpaceOS.Modules.FlowManagement.Domain;

namespace SpaceOS.Modules.FlowManagement.Infrastructure.Configurations;

/// <summary>
/// EF Core entity type configuration for <see cref="FlowTask"/>.
/// </summary>
internal sealed class FlowTaskConfiguration : IEntityTypeConfiguration<FlowTask>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<FlowTask> builder)
    {
        builder.ToTable("FlowTasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.TenantId)
            .IsRequired();

        // UUID-only reference to Kernel FlowEpic — no FK constraint across module boundary.
        builder.Property(t => t.EpicKernelId)
            .IsRequired();

        builder.Property(t => t.MilestoneId);
        builder.Property(t => t.Description).HasMaxLength(2000);
        builder.Property(t => t.AssigneeId);
        builder.Property(t => t.DueDate);

        builder.Property(t => t.Status)
            .HasMaxLength(50)
            .IsRequired();

        // Phase stored as string so enum renames do not require a migration.
        builder.Property(t => t.Phase)
            .HasConversion(
                phase => phase.ToString(),
                value => Enum.Parse<WorkflowPhase>(value))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.IsSyncedToKernel)
            .IsRequired();

        builder.Property(t => t.LastSyncAt);

        // Efficient lookup of all tasks belonging to a given Kernel epic.
        builder.HasIndex(t => t.EpicKernelId)
            .HasDatabaseName("IX_FlowTasks_EpicKernelId");

        // Efficient lookup of all tasks grouped under a milestone.
        builder.HasIndex(t => t.MilestoneId)
            .HasDatabaseName("IX_FlowTasks_MilestoneId");
    }
}
