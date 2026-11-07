using System.Data;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class StringParameterTest
{
    public static IEnumerable<object[]> NormalizationCases
    {
        get
        {
            var cases = new (string Input, bool Trim, string Expected)[]
            {
                (null, false, null),
                ("", false, ""),
                (" \t\r\n ", false, " \t\r\n "),
                ("  text  ", false, "  text  "),
                (null, true, null),
                ("", true, null),
                (" \t\r\n ", true, null),
                (" \ttext\r\n ", true, "text")
            };

            foreach (var unicode in new[] { false, true })
            foreach (var max in new[] { false, true })
            foreach (var testCase in cases)
                yield return new object[] { unicode, max, testCase.Input, testCase.Trim, testCase.Expected };
        }
    }

    [TestMethod]
    [DynamicData(nameof(NormalizationCases))]
    public void StringParametersApplyTrimFlag(bool unicode, bool max, string value, bool trimToNull, string expected)
    {
        using var command = new SqlCommand();

        AddParameter(command, unicode, max, 20, value, trimToNull, truncate: false);

        AssertParameter(command, unicode, max ? -1 : 20, expected);
    }

    public static IEnumerable<object[]> SizedCases
    {
        get
        {
            var cases = new (string Input, bool Trim, bool Truncate, string Expected)[]
            {
                (null, false, true, null),
                ("", false, true, ""),
                ("ab", false, true, "ab"),
                ("abc", false, true, "abc"),
                ("abcdef", false, true, "abc"),
                ("\u0100\u0101\u0102\u0103", false, true, "\u0100\u0101\u0102"),
                ("  abc", false, true, "  a"),
                ("  abcdef  ", true, true, "abc"),
                (" \t\r\n ", true, true, null),
                ("  abc  ", true, false, "abc"),
                (" \t\r\n ", true, false, null)
            };

            foreach (var unicode in new[] { false, true })
            foreach (var testCase in cases)
                yield return new object[] { unicode, testCase.Input, testCase.Trim, testCase.Truncate, testCase.Expected };
        }
    }

    [TestMethod]
    [DynamicData(nameof(SizedCases))]
    public void SizedStringParametersTrimBeforeApplyingLengthLimit(
        bool unicode, string value, bool trimToNull, bool truncate, string expected)
    {
        using var command = new SqlCommand();

        AddParameter(command, unicode, max: false, 3, value, trimToNull, truncate);

        AssertParameter(command, unicode, 3, expected);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void SizedStringParametersRejectExcessLengthWhenTruncationIsDisabled(bool unicode, bool trimToNull)
    {
        using var command = new SqlCommand();

        Assert.ThrowsExactly<ArgumentException>(() =>
            AddParameter(command, unicode, max: false, 3, "  abcdef  ", trimToNull, truncate: false));

        Assert.AreEqual(0, command.Parameters.Count);
    }

    private static void AddParameter(
        SqlCommand command, bool unicode, bool max, int size, string value, bool trimToNull, bool truncate)
    {
        if (max)
        {
            if (unicode)
                command.AddNVarcharMaxParam("@Value", value, trimToNull);
            else
                command.AddVarcharMaxParam("@Value", value, trimToNull);
        }
        else if (unicode)
            command.AddNVarcharParam("@Value", size, value, trimToNull, truncate);
        else
            command.AddVarcharParam("@Value", size, value, trimToNull, truncate);
    }

    private static void AssertParameter(SqlCommand command, bool unicode, int size, string expected)
    {
        Assert.AreEqual(1, command.Parameters.Count);
        var parameter = command.Parameters[0];
        Assert.AreEqual("@Value", parameter.ParameterName);
        Assert.AreEqual(unicode ? SqlDbType.NVarChar : SqlDbType.VarChar, parameter.SqlDbType);
        Assert.AreEqual(size, parameter.Size);
        Assert.AreEqual(ParameterDirection.Input, parameter.Direction);
        Assert.AreEqual((object)expected ?? DBNull.Value, parameter.Value);
    }
}
