using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMA.Data.Entities;

namespace SAMA.Data.Configuration;

public class AlertHistoryConfiguration : IEntityTypeConfiguration<AlertHistory>
{
    public void Configure(EntityTypeBuilder<AlertHistory> builder)
    {
        builder.ToTable("AlertHistories");

        builder.HasKey(ah => ah.Id);

        builder.Property(ah => ah.TriggerEventId)
            .IsRequired();

        builder.Property(ah => ah.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(ah => ah.Message)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(ah => ah.SentAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(ah => ah.Success)
            .IsRequired();

        builder.Property(ah => ah.ErrorMessage)
            .HasMaxLength(2000);

        // Relationships
        builder.HasOne(ah => ah.Alert)
            .WithMany(a => a.AlertHistories)
            .HasForeignKey(ah => ah.AlertId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ah => ah.NotificationChannel)
            .WithMany(nc => nc.AlertHistories)
            .HasForeignKey(ah => ah.NotificationChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ah => new { ah.AlertId, ah.SentAt })
            .HasDatabaseName("IX_AlertHistory_AlertId_SentAt")
            .IsDescending(false, true); // AlertId ASC, SentAt DESC

        builder.HasIndex(ah => ah.NotificationChannelId)
            .HasDatabaseName("IX_AlertHistory_NotificationChannelId");

        builder.HasIndex(ah => ah.TriggerEventId)
            .HasDatabaseName("IX_AlertHistory_TriggerEventId");
    }
}
