using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMA.Data.Entities;

namespace SAMA.Data.Configuration;

public class UserWorkspaceConfiguration : IEntityTypeConfiguration<UserWorkspace>
{
    public void Configure(EntityTypeBuilder<UserWorkspace> builder)
    {
        builder.ToTable("UserWorkspaces");

        builder.HasKey(uw => new { uw.UserId, uw.WorkspaceId });

        builder.Property(uw => uw.Role)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(uw => uw.Source)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Manual");

        builder.Property(uw => uw.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(uw => uw.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(uw => uw.User)
            .WithMany(u => u.UserWorkspaces)
            .HasForeignKey(uw => uw.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uw => uw.Workspace)
            .WithMany(w => w.UserWorkspaces)
            .HasForeignKey(uw => uw.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(uw => uw.WorkspaceId)
            .HasDatabaseName("IX_UserWorkspaces_WorkspaceId");

        builder.HasIndex(uw => uw.Source)
            .HasDatabaseName("IX_UserWorkspaces_Source");
    }
}
