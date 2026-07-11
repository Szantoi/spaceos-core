using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Abstractions.Domain.Entities;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Persistence.Configurations;

public sealed class TemplateParameterConfiguration : IEntityTypeConfiguration<TemplateParameter>
{
    public void Configure(EntityTypeBuilder<TemplateParameter> builder)
    {
        builder.ToTable("TemplateParameters");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.TemplateId).IsRequired();
        builder.Property(p => p.Key).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Value).HasColumnType("decimal(10,4)").IsRequired();
        builder.Property(p => p.Description).HasMaxLength(200);

        builder.HasIndex(p => p.TemplateId).HasDatabaseName("IX_TemplateParameters_TemplateId");
        builder.HasIndex(p => new { p.TemplateId, p.Key }).IsUnique();
    }
}
