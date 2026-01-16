using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace SAMA.Data;

/// <summary>
/// Value generator that creates UUIDv7 (time-ordered) GUIDs for primary keys.
/// Uses the built-in Guid.CreateVersion7() method from .NET 9.
/// </summary>
public class UuidV7ValueGenerator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry)
    {
        return Guid.CreateVersion7();
    }
}
