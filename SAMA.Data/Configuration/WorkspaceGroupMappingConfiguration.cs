using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMA.Data.Entities;

namespace SAMA.Data.Configuration;

public class WorkspaceGroupMappingConfiguration : IEntityTypeConfiguration<WorkspaceGroupMapping>
{
    public void Configure(EntityTypeBuilder<WorkspaceGroupMapping> builder)
    {
        builder.ToTable("WorkspaceGroupMappings");

        builder.HasKey(wgm => wgm.Id);

        builder.Property(wgm => wgm.IdentityProvider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(wgm => wgm.ExternalGroupId)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(wgm => wgm.Role)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(wgm => wgm.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(wgm => wgm.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(wgm => wgm.Workspace)
            .WithMany(w => w.WorkspaceGroupMappings)
            .HasForeignKey(wgm => wgm.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(wgm => wgm.WorkspaceId)
            .HasDatabaseName("IX_WorkspaceGroupMappings_WorkspaceId");

        builder.HasIndex(wgm => new { wgm.WorkspaceId, wgm.IdentityProvider, wgm.ExternalGroupId })
            .IsUnique()
            .HasDatabaseName("UX_WorkspaceGroupMappings_Mapping");
    }
}
