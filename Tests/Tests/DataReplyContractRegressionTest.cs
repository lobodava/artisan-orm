using System.Text.Json;
using System.Text.Json.Serialization;
using Artisan.Orm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class DataReplyContractRegressionTest
{
    [TestMethod]
    [DataRow("999")]
    [DataRow("-1")]
    [DataRow("Fail, Missing")]
    [DataRow("Ok, Ok")]
    [DataRow("0, 1")]
    [DataRow("Unknown")]
    public void ParseStatusRejectsUndefinedAndCombinedValues(string input)
    {
        Assert.ThrowsExactly<InvalidCastException>(() => DataReply.ParseStatus(input));
    }

    [TestMethod]
    public void ParseStatusAcceptsEveryDefinedNameAndNumericValue()
    {
        foreach (var status in Enum.GetValues<DataReplyStatus>())
        {
            Assert.AreEqual(status, DataReply.ParseStatus(" " + status.ToString().ToLowerInvariant() + " "));
            Assert.AreEqual(status, DataReply.ParseStatus(((int)status).ToString()));
        }
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" \t\r\n")]
    public void ParseStatusPreservesBlankInput(string input)
    {
        Assert.IsNull(DataReply.ParseStatus(input));
    }

    [TestMethod]
    public void StatusOnlyExceptionsHaveUsefulMessages()
    {
        foreach (var status in Enum.GetValues<DataReplyStatus>())
        {
            StringAssert.Contains(new DataReplyException(status).Message, status.ToString());
            StringAssert.Contains(new DataReplyException(status, Array.Empty<DataReplyMessage>()).Message,
                status.ToString());
        }
        StringAssert.Contains(new DataReplyException().Message, "Error");
    }

    [TestMethod]
    public void DetailedExceptionMessagesRetainTheirExistingFormat()
    {
        var exception = new DataReplyException(DataReplyStatus.Validation, "NameRequired", "Enter a name", 42L, "");
        Assert.AreEqual("{Code: NameRequired, Text: Enter a name, Id: 42, Value: }", exception.Message);
    }

    [TestMethod]
    public void DocumentedJsonSettingsPreservePublicMessageFields()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        var reply = new DataReply<string>(DataReplyStatus.Validation,
            new[] { new DataReplyMessage { Code = "NameRequired", Text = "Enter a name", Id = -1, Value = "" } });
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(reply, options));
        var root = document.RootElement;
        Assert.AreEqual("validation", root.GetProperty("status").GetString());
        Assert.IsFalse(root.TryGetProperty("data", out _));
        var message = root.GetProperty("messages")[0];
        Assert.AreEqual("NameRequired", message.GetProperty("code").GetString());
        Assert.AreEqual("Enter a name", message.GetProperty("text").GetString());
        Assert.AreEqual(-1L, message.GetProperty("id").GetInt64());
        Assert.AreEqual("", message.GetProperty("value").GetString());
        using var statusOnly = JsonDocument.Parse(JsonSerializer.Serialize(new DataReply(DataReplyStatus.Missing), options));
        Assert.IsFalse(statusOnly.RootElement.TryGetProperty("messages", out _));
    }
}
