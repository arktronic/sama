using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMA.Data.Entities;

namespace SAMA.Data.Configuration;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.Description)
            .HasMaxLength(500);

        builder.Property(w => w.IsPublic)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(w => w.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(w => w.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(w => w.IsPublic)
            .HasDatabaseName("IX_Workspaces_IsPublic");
    }
}
