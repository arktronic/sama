namespace SAMA.Data.Entities;

public class GlobalSetting
{
    public required string Key { get; set; }

    public required string Value { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
