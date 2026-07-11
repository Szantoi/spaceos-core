using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="FlowEpic"/> aggregate root.
/// </summary>
internal sealed class FlowEpicConfiguration : IEntityTypeConfiguration<FlowEpic>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<FlowEpic> builder)
    {
        builder.ToTable("FlowEpics");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasConversion(
                id => id.Value,
                value => FlowEpicId.From(value));

        builder.Property(f => f.Title)
            .HasConversion(
                title => title.Value,
                value => FlowEpicTitle.From(value))
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.TargetFacilityId)
            .HasConversion(
                id => id.Value,
                value => FacilityId.From(value))
            .IsRequired();

        builder.HasIndex(f => f.TargetFacilityId);

        // FSM WorkflowPhase → string for readability (Discovery / Delivery)
        builder.Property(f => f.Phase)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // B2BHandshake is a nullable owned reference type (sealed record).
        // OwnsOne with nullable navigation gives null when no delegation data exists.
        builder.OwnsOne(f => f.Handshake, handshake =>
        {
            handshake.Property(h => h.GuestTenantId)
                .HasConversion(id => id.Value, value => TenantId.From(value))
                .HasColumnName("Handshake_GuestTenantId");
            handshake.Property(h => h.DelegatedOn)
                .HasColumnName("Handshake_DelegatedOn");

            // Sprint C nullable extensions — stored as plain columns for portability.
            // JSONB column type for anchors is applied in the PostgreSQL migration.
            handshake.Property(h => h.InitiatorAnchorJson)
                .HasColumnName("Handshake_InitiatorAnchorJson");
            handshake.Property(h => h.ResponsibleAnchorJson)
                .HasColumnName("Handshake_ResponsibleAnchorJson");
            handshake.Property(h => h.VisibilityScope)
                .HasColumnName("Handshake_VisibilityScope")
                .HasMaxLength(50);
            handshake.Property(h => h.ContractHash)
                .HasColumnName("Handshake_ContractHash")
                .HasMaxLength(64);
        });

        builder.Property(f => f.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.From(value))
            .IsRequired();

        builder.HasIndex(f => f.TenantId);

        builder.Property(f => f.ProofUrl)
            .HasMaxLength(2048);

        builder.Property(f => f.ProofHash)
            .HasMaxLength(64);

        // FlowEpicScope → nullable string for readability and backward compatibility.
        builder.Property(f => f.Scope)
            .HasConversion<string?>()
            .HasMaxLength(50);

        builder.Property(f => f.RequiredSkillLevel)
            .HasMaxLength(50);

        // RequiredResources — owned collection stored in child table FlowEpicRequiredResources.
        builder.OwnsMany(f => f.RequiredResources, resource =>
        {
            resource.ToTable("FlowEpicRequiredResources");
            resource.WithOwner().HasForeignKey("FlowEpicId");
            resource.HasKey(r => r.Id);
            resource.Property(r => r.ResourceType).HasMaxLength(100).IsRequired();
            resource.Property(r => r.ResourceName).HasMaxLength(200).IsRequired();
            resource.Property(r => r.Quantity).IsRequired();
        });
    }
}
