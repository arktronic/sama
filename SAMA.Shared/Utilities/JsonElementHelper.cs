using System.Text.Json;

namespace SAMA.Shared.Utilities;

/// <summary>
/// Utility class for working with JsonElement dictionaries.
/// Provides safe type extraction and conversion methods.
/// </summary>
public static class JsonElementHelper
{
    /// <summary>
    /// Safely extracts a string value from a JsonElement dictionary.
    /// </summary>
    /// <param name="dict">The dictionary to search</param>
    /// <param name="key">The key to look up</param>
    /// <returns>The string value, or null if not found or not a string</returns>
    public static string? GetString(Dictionary<string, JsonElement> dict, string key)
    {
        if (!dict.TryGetValue(key, out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    /// <summary>
    /// Safely extracts a string value from a JsonElement dictionary with a default fallback.
    /// </summary>
    /// <param name="dict">The dictionary to search</param>
    /// <param name="key">The key to look up</param>
    /// <param name="defaultValue">Default value to return if key not found or value is invalid</param>
    /// <returns>The string value, or the default value if not found or not a string</returns>
    public static string GetString(Dictionary<string, JsonElement> dict, string key, string defaultValue)
    {
        return GetString(dict, key) ?? defaultValue;
    }

    /// <summary>
    /// Safely extracts an int value from a JsonElement dictionary.
    /// </summary>
    /// <param name="dict">The dictionary to search</param>
    /// <param name="key">The key to look up</param>
    /// <returns>The int value, or null if not found or not a number</returns>
    public static int? GetInt32(Dictionary<string, JsonElement> dict, string key)
    {
        if (!dict.TryGetValue(key, out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value) ? value : null;
    }

    /// <summary>
    /// Safely extracts an int value from a JsonElement dictionary with a default fallback.
    /// </summary>
    /// <param name="dict">The dictionary to search</param>
    /// <param name="key">The key to look up</param>
    /// <param name="defaultValue">Default value to return if key not found or value is invalid</param>
    /// <returns>The int value, or the default value if not found or not a number</returns>
    public static int GetInt32(Dictionary<string, JsonElement> dict, string key, int defaultValue)
    {
        return GetInt32(dict, key) ?? defaultValue;
    }

    /// <summary>
    /// Safely extracts a bool value from a JsonElement dictionary.
    /// </summary>
    /// <param name="dict">The dictionary to search</param>
    /// <param name="key">The key to look up</param>
    /// <returns>The bool value, or null if not found or not a boolean</returns>
    public static bool? GetBoolean(Dictionary<string, JsonElement> dict, string key)
    {
        if (!dict.TryGetValue(key, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    /// <summary>
    /// Safely extracts a bool value from a JsonElement dictionary with a default fallback.
    /// </summary>
    /// <param name="dict">The dictionary to search</param>
    /// <param name="key">The key to look up</param>
    /// <param name="defaultValue">Default value to return if key not found or value is invalid</param>
    /// <returns>The bool value, or the default value if not found or not a boolean</returns>
    public static bool GetBoolean(Dictionary<string, JsonElement> dict, string key, bool defaultValue)
    {
        return GetBoolean(dict, key) ?? defaultValue;
    }

    /// <summary>
    /// Safely extracts a string array from a JsonElement dictionary.
    /// </summary>
    /// <param name="dict">The dictionary to search</param>
    /// <param name="key">The key to look up</param>
    /// <returns>The string array, or null if not found or not an array</returns>
    public static string[]? GetStringArray(Dictionary<string, JsonElement> dict, string key)
    {
        if (!dict.TryGetValue(key, out var element))
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var list = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var str = item.GetString();
                if (str != null)
                {
                    list.Add(str);
                }
            }
        }

        return [.. list];
    }

    /// <summary>
    /// Safely extracts a string array from a JsonElement dictionary with a default fallback.
    /// </summary>
    /// <param name="dict">The dictionary to search</param>
    /// <param name="key">The key to look up</param>
    /// <param name="defaultValue">Default value to return if key not found or value is invalid</param>
    /// <returns>The string array, or the default value if not found or not an array</returns>
    public static string[] GetStringArray(Dictionary<string, JsonElement> dict, string key, string[] defaultValue)
    {
        return GetStringArray(dict, key) ?? defaultValue;
    }

    /// <summary>
    /// Safely extracts an int array from a JsonElement dictionary.
    /// </summary>
    /// <param name="dict">The dictionary to search</param>
    /// <param name="key">The key to look up</param>
    /// <returns>The int array, or null if not found or not an array</returns>
    public static int[]? GetInt32Array(Dictionary<string, JsonElement> dict, string key)
    {
        if (!dict.TryGetValue(key, out var element))
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var list = new List<int>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var value))
            {
                list.Add(value);
            }
        }

        return [.. list];
    }

    /// <summary>
    /// Safely extracts an int array from a JsonElement dictionary with a default fallback.
    /// </summary>
    /// <param name="dict">The dictionary to search</param>
    /// <param name="key">The key to look up</param>
    /// <param name="defaultValue">Default value to return if key not found or value is invalid</param>
    /// <returns>The int array, or the default value if not found or not an array</returns>
    public static int[] GetInt32Array(Dictionary<string, JsonElement> dict, string key, int[] defaultValue)
    {
        return GetInt32Array(dict, key) ?? defaultValue;
    }

    /// <summary>
    /// Converts a JsonElement to a regular .NET object for display purposes.
    /// </summary>
    /// <param name="element">The JsonElement to convert</param>
    /// <returns>The converted object</returns>
    public static object ConvertToDisplayObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.ToString(), // Return as string to preserve formatting
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertToDisplayObject)
                .ToArray(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertToDisplayObject(p.Value)),
            _ => element.ToString()
        };
    }

    /// <summary>
    /// Converts an entire Dictionary of JsonElements to a Dictionary of objects.
    /// Useful for preparing data for display in views.
    /// </summary>
    /// <param name="dict">The dictionary to convert</param>
    /// <returns>A new dictionary with all values converted to objects</returns>
    public static Dictionary<string, object> ConvertToDisplayObjectDictionary(Dictionary<string, JsonElement> dict)
    {
        var result = new Dictionary<string, object>();
        foreach (var kvp in dict)
        {
            result[kvp.Key] = ConvertToDisplayObject(kvp.Value);
        }
        return result;
    }
}
