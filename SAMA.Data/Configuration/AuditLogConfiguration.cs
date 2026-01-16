using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMA.Data.Entities;

namespace SAMA.Data.Configuration;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(al => al.Id);

        builder.Property(al => al.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(al => al.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(al => al.EntityId)
            .IsRequired();

        builder.Property(al => al.Changes)
            .HasColumnType("text");

        builder.Property(al => al.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(al => al.IpAddress)
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(al => al.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(al => new { al.EntityType, al.EntityId })
            .HasDatabaseName("IX_AuditLogs_EntityType_EntityId");

        builder.HasIndex(al => al.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp")
            .IsDescending();
    }
}
