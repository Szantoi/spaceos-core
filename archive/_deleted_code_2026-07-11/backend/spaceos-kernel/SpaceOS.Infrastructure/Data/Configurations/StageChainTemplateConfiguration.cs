// SpaceOS.Infrastructure/Data/Configurations/StageChainTemplateConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>EF Core entity type configuration for <see cref="StageChainTemplate"/>.</summary>
internal sealed class StageChainTemplateConfiguration : IEntityTypeConfiguration<StageChainTemplate>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<StageChainTemplate> builder)
    {
        builder.ToTable("StageChainTemplates");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        builder.HasIndex(x => x.TenantId);

        // Navigation property — EF Core loads via the backing field _steps
        builder.HasMany(x => x.Steps)
               .WithOne()
               .HasForeignKey(s => s.ChainTemplateId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
