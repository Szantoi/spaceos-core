using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Joinery.Domain.Rules;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Configurations;

public class ProcessTaskTemplateConfiguration : IEntityTypeConfiguration<ProcessTaskTemplate>
{
    public void Configure(EntityTypeBuilder<ProcessTaskTemplate> builder)
    {
        builder.ToTable("ProcessTaskTemplates");
        builder.HasKey(e => e.TaskId);
        builder.Property(e => e.TaskId).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ShortName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Department).HasMaxLength(100);
        builder.Property(e => e.ParentTaskId).HasMaxLength(50);
    }
}
