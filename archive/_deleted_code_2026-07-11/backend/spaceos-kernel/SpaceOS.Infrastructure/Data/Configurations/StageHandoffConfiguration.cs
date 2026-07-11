// SpaceOS.Infrastructure/Data/Configurations/StageHandoffConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Infrastructure.Data.Configurations;



/// <summary>EF Core entity type configuration for <see cref="StageHandoff"/>.</summary>
internal sealed class StageHandoffConfiguration : IEntityTypeConfiguration<StageHandoff>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<StageHandoff> builder)
    {
        builder.ToTable("StageHandoffs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FlowEpicId).IsRequired();
        builder.Property(x => x.SourceStageCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.TargetStageCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.IdempotencyKey).IsRequired();
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.PayloadHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.HashAlgorithm).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        // Compound unique: no two handoffs share the same version for a given epic+source+target
        builder.HasIndex(x => new { x.FlowEpicId, x.SourceStageCode, x.TargetStageCode, x.Version }).IsUnique();
        // SEC-05: idempotency key unique per epic
        builder.HasIndex(x => new { x.FlowEpicId, x.IdempotencyKey }).IsUnique();

        builder.HasIndex(x => x.FlowEpicId);
        builder.HasIndex(x => new { x.TenantId, x.SourceStageCode });
        builder.HasIndex(x => new { x.TenantId, x.TargetStageCode });

        // DB-07: SourceActorId / TargetActorId are soft references to Tenants.
        // EF Core cannot map Guid? → TenantId (VO primary key) without an explicit
        // value-object principal key — the FK constraint is enforced at the DB level
        // via raw DDL in Migration_0028 instead.
        builder.Property(x => x.SourceActorId).IsRequired(false);
        builder.Property(x => x.TargetActorId).IsRequired(false);
    }
}
