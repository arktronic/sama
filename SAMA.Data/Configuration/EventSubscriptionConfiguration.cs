using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMA.Data.Entities;

namespace SAMA.Data.Configuration;

public class EventSubscriptionConfiguration : IEntityTypeConfiguration<EventSubscription>
{
    public void Configure(EntityTypeBuilder<EventSubscription> builder)
    {
        builder.ToTable("EventSubscriptions");

        builder.HasKey(es => es.Id);

        builder.Property(es => es.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(es => es.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(es => es.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(es => es.NotificationChannel)
            .WithMany(nc => nc.EventSubscriptions)
            .HasForeignKey(es => es.NotificationChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(es => es.NotificationChannelId)
            .HasDatabaseName("IX_EventSubscriptions_NotificationChannelId");

        builder.HasIndex(es => es.EventType)
            .HasDatabaseName("IX_EventSubscriptions_EventType");

        builder.HasIndex(es => new { es.NotificationChannelId, es.EventType })
            .IsUnique()
            .HasDatabaseName("UX_EventSubscriptions_Subscription");
    }
}
