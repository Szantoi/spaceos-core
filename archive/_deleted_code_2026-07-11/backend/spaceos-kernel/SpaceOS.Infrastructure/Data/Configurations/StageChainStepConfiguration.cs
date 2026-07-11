// SpaceOS.Infrastructure/Data/Configurations/StageChainStepConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>EF Core entity type configuration for <see cref="StageChainStep"/>.</summary>
internal sealed class StageChainStepConfiguration : IEntityTypeConfiguration<StageChainStep>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<StageChainStep> builder)
    {
        builder.ToTable("StageChainSteps");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ChainTemplateId).IsRequired();
        builder.Property(x => x.StageDefinitionId).IsRequired();
        builder.Property(x => x.StageCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.IsOptional).IsRequired();

        builder.HasIndex(x => new { x.ChainTemplateId, x.StageCode }).IsUnique();
        builder.HasIndex(x => new { x.ChainTemplateId, x.SortOrder }).IsUnique();
        builder.HasIndex(x => x.ChainTemplateId);
        builder.HasIndex(x => x.StageDefinitionId);

        // DB-04: FK directly to StageDefinitions PK
        builder.HasOne<StageDefinition>()
               .WithMany()
               .HasForeignKey(x => x.StageDefinitionId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
