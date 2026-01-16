using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMA.Data.Entities;

namespace SAMA.Data.Configuration;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.TriggerOnWarn)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.TriggerOnDown)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.FailureThreshold)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(a => a.SendRecoveryNotification)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.Enabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(a => a.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(a => a.Check)
            .WithMany(c => c.Alerts)
            .HasForeignKey(a => a.CheckId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-many with NotificationChannels
        builder.HasMany(a => a.NotificationChannels)
            .WithMany(nc => nc.Alerts)
            .UsingEntity<Dictionary<string, object>>(
                "NotificationChannelMappings",
                j => j.HasOne<NotificationChannel>().WithMany().HasForeignKey("NotificationChannelId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Alert>().WithMany().HasForeignKey("AlertId").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("AlertId", "NotificationChannelId");
                    j.ToTable("NotificationChannelMappings");
                });

        // Indexes
        builder.HasIndex(a => a.CheckId)
            .HasDatabaseName("IX_Alerts_CheckId");

        builder.HasIndex(a => a.Enabled)
            .HasDatabaseName("IX_Alerts_Enabled");
    }
}
