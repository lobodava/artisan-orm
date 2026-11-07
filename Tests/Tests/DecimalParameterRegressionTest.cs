using System.Data;
using System.Globalization;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class DecimalParameterRegressionTest
{
    [TestMethod]
    [DataRow("0.25", 2, 2)]
    [DataRow("-0.25", 2, 2)]
    [DataRow("0", 1, 1)]
    [DataRow("0.99", 2, 2)]
    [DataRow("-0.99", 2, 2)]
    [DataRow("9.99", 3, 2)]
    [DataRow("1.2300", 3, 2)]
    [DataRow("0.000", 1, 0)]
    [DataRow("79228162514264337593543950335", 29, 0)]
    [DataRow("-79228162514264337593543950335", 38, 9)]
    [DataRow("0.0000000000000000000000000001", 38, 38)]
    public void ExactValuesFitWithoutRounding(string text, int precision, int scale)
    {
        var value = decimal.Parse(text, CultureInfo.InvariantCulture);
        using var command = new SqlCommand();
        command.AddDecimalParam("@Amount", (byte)precision, (byte)scale, value);
        var parameter = command.Parameters["@Amount"];
        Assert.AreEqual(value, parameter.Value);
        Assert.AreEqual((byte)precision, parameter.Precision);
        Assert.AreEqual((byte)scale, parameter.Scale);
        Assert.AreEqual(SqlDbType.Decimal, parameter.SqlDbType);
    }

    [TestMethod]
    [DataRow("1.239", "1.23", 3, 2)]
    [DataRow("-1.239", "-1.23", 3, 2)]
    [DataRow("0.999", "0.99", 2, 2)]
    [DataRow("-0.999", "-0.99", 2, 2)]
    [DataRow("9.9", "9", 1, 0)]
    [DataRow("-0.9", "0", 1, 0)]
    [DataRow("7922816251426433759354395033.5", "7922816251426433759354395033", 28, 0)]
    public void TruncateFractionDiscardsDigitsTowardZeroBeforeBinding(string input, string expected, int precision, int scale)
    {
        using var command = new SqlCommand();
        var value = decimal.Parse(input, CultureInfo.InvariantCulture);
        command.AddDecimalParam("@Amount", (byte)precision, (byte)scale, value, truncateFraction: true);
        command.AddDecimalParam("@Nullable", (byte)precision, (byte)scale, (decimal?)value, truncateFraction: true);
        Assert.AreEqual(decimal.Parse(expected, CultureInfo.InvariantCulture), command.Parameters["@Amount"].Value);
        Assert.AreEqual(command.Parameters["@Amount"].Value, command.Parameters["@Nullable"].Value);
    }

    [TestMethod]
    [DataRow("1.239")]
    [DataRow("-1.239")]
    public void DefaultRejectsLossOfFractionalPrecision(string text)
    {
        using var command = new SqlCommand();
        Assert.ThrowsExactly<ArgumentException>(() => command.AddDecimalParam("@Amount", 3, 2,
            decimal.Parse(text, CultureInfo.InvariantCulture)));
        Assert.AreEqual(0, command.Parameters.Count);
    }

    [TestMethod]
    [DataRow("1", 2, 2)]
    [DataRow("-1", 2, 2)]
    [DataRow("10.00", 3, 2)]
    [DataRow("1000", 3, 0)]
    public void IntegerOverflowIsRejectedEvenWhenTruncationIsEnabled(string text, int precision, int scale)
    {
        using var command = new SqlCommand();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => command.AddDecimalParam("@Amount", (byte)precision,
            (byte)scale, decimal.Parse(text, CultureInfo.InvariantCulture), truncateFraction: true));
        Assert.AreEqual(0, command.Parameters.Count);
    }

    [TestMethod]
    [DataRow(0, 0, "precision")]
    [DataRow(39, 0, "precision")]
    [DataRow(2, 3, "scale")]
    public void InvalidMetadataIsRejectedForValuesAndNull(int precision, int scale, string parameterName)
    {
        using var command = new SqlCommand();
        var valueError = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            command.AddDecimalParam("@Amount", (byte)precision, (byte)scale, 0m));
        var nullError = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            command.AddDecimalParam("@Amount", (byte)precision, (byte)scale, (decimal?)null));
        Assert.AreEqual(parameterName, valueError.ParamName);
        Assert.AreEqual(parameterName, nullError.ParamName);
        Assert.AreEqual(0, command.Parameters.Count);
    }

    [TestMethod]
    public void NullValuePreservesDeclaredMetadata()
    {
        using var command = new SqlCommand();
        command.AddDecimalParam("@Amount", 2, 2, (decimal?)null);
        Assert.AreEqual(DBNull.Value, command.Parameters["@Amount"].Value);
        Assert.AreEqual((byte)2, command.Parameters["@Amount"].Precision);
        Assert.AreEqual((byte)2, command.Parameters["@Amount"].Scale);
    }

    [TestMethod]
    public void SqlServerReceivesTruncatedValueAndPureFraction()
    {
        using var connection = new SqlConnection(new AppSettings().ConnectionStrings.DatabaseConnection);
        using var command = new SqlCommand("SELECT @Positive, @Negative, @Fraction", connection);
        command.AddDecimalParam("@Positive", 3, 2, 1.239m, truncateFraction: true);
        command.AddDecimalParam("@Negative", 3, 2, -1.239m, truncateFraction: true);
        command.AddDecimalParam("@Fraction", 2, 2, 0.25m);
        connection.Open();
        using var reader = command.ExecuteReader();
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(1.23m, reader.GetDecimal(0));
        Assert.AreEqual(-1.23m, reader.GetDecimal(1));
        Assert.AreEqual(0.25m, reader.GetDecimal(2));
    }
}
