// SpaceOS.Infrastructure/Data/Configurations/StageDefinitionConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Infrastructure.Data.Configurations;

/// <summary>EF Core entity type configuration for <see cref="StageDefinition"/>.</summary>
internal sealed class StageDefinitionConfiguration : IEntityTypeConfiguration<StageDefinition>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<StageDefinition> builder)
    {
        builder.ToTable("StageDefinitions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.StageCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ModuleEndpoint).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.StageCode }).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}
