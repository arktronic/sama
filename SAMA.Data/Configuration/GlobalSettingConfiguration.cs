using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMA.Data.Entities;

namespace SAMA.Data.Configuration;

public class GlobalSettingConfiguration : IEntityTypeConfiguration<GlobalSetting>
{
    public void Configure(EntityTypeBuilder<GlobalSetting> builder)
    {
        builder.ToTable("GlobalSettings");

        builder.HasKey(gs => gs.Key);

        builder.Property(gs => gs.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(gs => gs.Value)
            .IsRequired();

        builder.Property(gs => gs.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
    }
}
