using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMA.Data.Entities;

namespace SAMA.Data.Configuration;

public class NotificationChannelConfiguration : IEntityTypeConfiguration<NotificationChannel>
{
    public void Configure(EntityTypeBuilder<NotificationChannel> builder)
    {
        builder.ToTable("NotificationChannels");

        builder.HasKey(ac => ac.Id);

        builder.Property(ac => ac.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ac => ac.ChannelType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ac => ac.ConfigurationJson)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(ac => ac.Enabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(ac => ac.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(ac => ac.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(ac => ac.Workspace)
            .WithMany(w => w.NotificationChannels)
            .HasForeignKey(ac => ac.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ac => ac.WorkspaceId)
            .HasDatabaseName("IX_NotificationChannels_WorkspaceId");

        builder.HasIndex(ac => ac.Enabled)
            .HasDatabaseName("IX_NotificationChannels_Enabled");
    }
}
