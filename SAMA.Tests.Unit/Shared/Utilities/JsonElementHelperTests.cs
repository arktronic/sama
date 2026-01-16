using System.Text.Json;
using SAMA.Shared.Utilities;

namespace SAMA.Tests.Unit.Shared.Utilities;

[TestClass]
public class JsonElementHelperTests
{
    [TestMethod]
    public void GetStringShouldReturnValueForValidString()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("test-value")
        };

        var result = JsonElementHelper.GetString(dict, "name");

        Assert.AreEqual("test-value", result);
    }

    [TestMethod]
    public void GetStringShouldReturnNullForMissingKey()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("test-value")
        };

        var result = JsonElementHelper.GetString(dict, "nonexistent");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetStringShouldReturnNullForNonStringValue()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["number"] = JsonSerializer.SerializeToElement(42)
        };

        var result = JsonElementHelper.GetString(dict, "number");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetInt32ShouldReturnValueForValidInt()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["age"] = JsonSerializer.SerializeToElement(42)
        };

        var result = JsonElementHelper.GetInt32(dict, "age");

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void GetInt32ShouldReturnNullForMissingKey()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["age"] = JsonSerializer.SerializeToElement(42)
        };

        var result = JsonElementHelper.GetInt32(dict, "nonexistent");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetInt32ShouldReturnNullForNonNumberValue()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("not-a-number")
        };

        var result = JsonElementHelper.GetInt32(dict, "name");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetBooleanShouldReturnTrueForTrueValue()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["enabled"] = JsonSerializer.SerializeToElement(true)
        };

        var result = JsonElementHelper.GetBoolean(dict, "enabled");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void GetBooleanShouldReturnFalseForFalseValue()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["enabled"] = JsonSerializer.SerializeToElement(false)
        };

        var result = JsonElementHelper.GetBoolean(dict, "enabled");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetBooleanShouldReturnNullForMissingKey()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["enabled"] = JsonSerializer.SerializeToElement(true)
        };

        var result = JsonElementHelper.GetBoolean(dict, "nonexistent");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetBooleanShouldReturnNullForNonBooleanValue()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("not-a-boolean")
        };

        var result = JsonElementHelper.GetBoolean(dict, "name");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetStringArrayShouldReturnValuesForValidArray()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["tags"] = JsonSerializer.SerializeToElement(new[] { "tag1", "tag2", "tag3" })
        };

        var result = JsonElementHelper.GetStringArray(dict, "tags");

        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { "tag1", "tag2", "tag3" }, result);
    }

    [TestMethod]
    public void GetStringArrayShouldReturnNullForMissingKey()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["tags"] = JsonSerializer.SerializeToElement(new[] { "tag1" })
        };

        var result = JsonElementHelper.GetStringArray(dict, "nonexistent");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetStringArrayShouldReturnNullForNonArrayValue()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("not-an-array")
        };

        var result = JsonElementHelper.GetStringArray(dict, "name");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetStringArrayShouldReturnEmptyArrayForEmptyArray()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["tags"] = JsonSerializer.SerializeToElement(Array.Empty<string>())
        };

        var result = JsonElementHelper.GetStringArray(dict, "tags");

        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void GetInt32ArrayShouldReturnValuesForValidArray()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["codes"] = JsonSerializer.SerializeToElement(new[] { 200, 201, 204 })
        };

        var result = JsonElementHelper.GetInt32Array(dict, "codes");

        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 200, 201, 204 }, result);
    }

    [TestMethod]
    public void GetInt32ArrayShouldReturnNullForMissingKey()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["codes"] = JsonSerializer.SerializeToElement(new[] { 200 })
        };

        var result = JsonElementHelper.GetInt32Array(dict, "nonexistent");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetInt32ArrayShouldReturnNullForNonArrayValue()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["code"] = JsonSerializer.SerializeToElement(200)
        };

        var result = JsonElementHelper.GetInt32Array(dict, "code");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetInt32ArrayShouldReturnEmptyArrayForEmptyArray()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["codes"] = JsonSerializer.SerializeToElement(Array.Empty<int>())
        };

        var result = JsonElementHelper.GetInt32Array(dict, "codes");

        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ConvertToDisplayObjectShouldReturnStringForStringElement()
    {
        var element = JsonSerializer.SerializeToElement("test-value");

        var result = JsonElementHelper.ConvertToDisplayObject(element);

        Assert.AreEqual("test-value", result);
    }

    [TestMethod]
    public void ConvertToDisplayObjectShouldReturnStringForIntElement()
    {
        var element = JsonSerializer.SerializeToElement(42);

        var result = JsonElementHelper.ConvertToDisplayObject(element);

        Assert.AreEqual("42", result);
    }

    [TestMethod]
    public void ConvertToDisplayObjectShouldReturnStringForDoubleElement()
    {
        var element = JsonSerializer.SerializeToElement(3.14);

        var result = JsonElementHelper.ConvertToDisplayObject(element);

        Assert.AreEqual("3.14", result);
    }

    [TestMethod]
    public void ConvertToDisplayObjectShouldReturnTrueForTrueElement()
    {
        var element = JsonSerializer.SerializeToElement(true);

        var result = JsonElementHelper.ConvertToDisplayObject(element);

        Assert.IsTrue((bool?)result);
    }

    [TestMethod]
    public void ConvertToDisplayObjectShouldReturnFalseForFalseElement()
    {
        var element = JsonSerializer.SerializeToElement(false);

        var result = JsonElementHelper.ConvertToDisplayObject(element);

        Assert.IsFalse((bool?)result);
    }

    [TestMethod]
    public void ConvertToDisplayObjectShouldReturnObjectArrayForArrayElement()
    {
        var element = JsonSerializer.SerializeToElement(new[] { "a", "b", "c" });

        var result = JsonElementHelper.ConvertToDisplayObject(element);

        Assert.IsInstanceOfType<object[]>(result);
        var array = (object[])result;
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, array);
    }

    [TestMethod]
    public void ConvertToDisplayObjectShouldReturnDictionaryForNestedObjectElement()
    {
        var source = new Dictionary<string, object>
        {
            ["name"] = "test",
            ["age"] = 42
        };
        var element = JsonSerializer.SerializeToElement(source);

        var result = JsonElementHelper.ConvertToDisplayObject(element);

        Assert.IsInstanceOfType<Dictionary<string, object>>(result);
        var dict = (Dictionary<string, object>)result;
        Assert.AreEqual("test", dict["name"]);
        Assert.AreEqual("42", dict["age"]);
    }

    [TestMethod]
    public void ConvertToDisplayObjectDictionaryShouldConvertAllValues()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("test"),
            ["age"] = JsonSerializer.SerializeToElement(42),
            ["enabled"] = JsonSerializer.SerializeToElement(true),
            ["tags"] = JsonSerializer.SerializeToElement(new[] { "tag1", "tag2" })
        };

        var result = JsonElementHelper.ConvertToDisplayObjectDictionary(dict);

        Assert.HasCount(4, result);
        Assert.AreEqual("test", result["name"]);
        Assert.AreEqual("42", result["age"]);
        Assert.IsTrue((bool?)result["enabled"]);
        Assert.IsInstanceOfType(result["tags"], typeof(object[]));
    }

    [TestMethod]
    public void ConvertToDisplayObjectDictionaryShouldReturnEmptyDictionaryForEmptyDictionary()
    {
        var dict = new Dictionary<string, JsonElement>();

        var result = JsonElementHelper.ConvertToDisplayObjectDictionary(dict);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void GetStringWithDefaultShouldReturnDefaultForMissingKey()
    {
        var dict = new Dictionary<string, JsonElement>();

        var result = JsonElementHelper.GetString(dict, "nonexistent", "default-value");

        Assert.AreEqual("default-value", result);
    }

    [TestMethod]
    public void GetStringWithDefaultShouldReturnDefaultForNonStringValue()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["number"] = JsonSerializer.SerializeToElement(42)
        };

        var result = JsonElementHelper.GetString(dict, "number", "default-value");

        Assert.AreEqual("default-value", result);
    }

    [TestMethod]
    public void GetStringWithDefaultShouldReturnActualValueWhenPresent()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("actual-value")
        };

        var result = JsonElementHelper.GetString(dict, "name", "default-value");

        Assert.AreEqual("actual-value", result);
    }

    [TestMethod]
    public void GetInt32WithDefaultShouldReturnDefaultForMissingKey()
    {
        var dict = new Dictionary<string, JsonElement>();

        var result = JsonElementHelper.GetInt32(dict, "nonexistent", 42);

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void GetInt32WithDefaultShouldReturnDefaultForNonNumberValue()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("not-a-number")
        };

        var result = JsonElementHelper.GetInt32(dict, "name", 42);

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void GetInt32WithDefaultShouldReturnActualValueWhenPresent()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["age"] = JsonSerializer.SerializeToElement(25)
        };

        var result = JsonElementHelper.GetInt32(dict, "age", 42);

        Assert.AreEqual(25, result);
    }

    [TestMethod]
    public void GetBooleanWithDefaultShouldReturnDefaultForMissingKey()
    {
        var dict = new Dictionary<string, JsonElement>();

        var result = JsonElementHelper.GetBoolean(dict, "nonexistent", true);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void GetBooleanWithDefaultShouldReturnDefaultForNonBooleanValue()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("not-a-boolean")
        };

        var result = JsonElementHelper.GetBoolean(dict, "name", true);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void GetBooleanWithDefaultShouldReturnActualValueWhenPresent()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["enabled"] = JsonSerializer.SerializeToElement(false)
        };

        var result = JsonElementHelper.GetBoolean(dict, "enabled", true);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetStringArrayWithDefaultShouldReturnDefaultForMissingKey()
    {
        var dict = new Dictionary<string, JsonElement>();

        var result = JsonElementHelper.GetStringArray(dict, "nonexistent", new[] { "default1", "default2" });

        CollectionAssert.AreEqual(new[] { "default1", "default2" }, result);
    }

    [TestMethod]
    public void GetStringArrayWithDefaultShouldReturnDefaultForNonArrayValue()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("not-an-array")
        };

        var result = JsonElementHelper.GetStringArray(dict, "name", new[] { "default1", "default2" });

        CollectionAssert.AreEqual(new[] { "default1", "default2" }, result);
    }

    [TestMethod]
    public void GetStringArrayWithDefaultShouldReturnActualValueWhenPresent()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["tags"] = JsonSerializer.SerializeToElement(new[] { "tag1", "tag2" })
        };

        var result = JsonElementHelper.GetStringArray(dict, "tags", new[] { "default1", "default2" });

        CollectionAssert.AreEqual(new[] { "tag1", "tag2" }, result);
    }

    [TestMethod]
    public void GetInt32ArrayWithDefaultShouldReturnDefaultForMissingKey()
    {
        var dict = new Dictionary<string, JsonElement>();

        var result = JsonElementHelper.GetInt32Array(dict, "nonexistent", new[] { 100, 200 });

        CollectionAssert.AreEqual(new[] { 100, 200 }, result);
    }

    [TestMethod]
    public void GetInt32ArrayWithDefaultShouldReturnDefaultForNonArrayValue()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["code"] = JsonSerializer.SerializeToElement(200)
        };

        var result = JsonElementHelper.GetInt32Array(dict, "code", new[] { 100, 200 });

        CollectionAssert.AreEqual(new[] { 100, 200 }, result);
    }

    [TestMethod]
    public void GetInt32ArrayWithDefaultShouldReturnActualValueWhenPresent()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["codes"] = JsonSerializer.SerializeToElement(new[] { 200, 201, 204 })
        };

        var result = JsonElementHelper.GetInt32Array(dict, "codes", new[] { 100, 200 });

        CollectionAssert.AreEqual(new[] { 200, 201, 204 }, result);
    }
}
