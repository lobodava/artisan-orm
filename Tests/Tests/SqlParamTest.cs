using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class SqlParamTest
{
    private const string OfflineConnectionString =
        "Server=localhost;Database=unused;Integrated Security=true;Connect Timeout=1;TrustServerCertificate=true";

    [TestMethod]
    public void FactoriesMirrorEveryTypedCommandHelperIncludingNamesAndDefaults()
    {
        var helpers = TypedHelpers();
        var factories = typeof(SqlParam).GetMethods(BindingFlags.Public | BindingFlags.Static);
        Assert.AreEqual(82, helpers.Length, "Update the reviewed API count when adding a typed helper.");
        Assert.AreEqual(helpers.Length, factories.Length);

        foreach (var helper in helpers)
        {
            var factory = FindFactory(helper);
            Assert.AreEqual(typeof(SqlParameter), factory.ReturnType, factory.ToString());
            var expected = helper.GetParameters().Skip(1).ToArray();
            var actual = factory.GetParameters();
            for (var i = 0; i < expected.Length; i++)
            {
                var context = factory + ": " + expected[i].Name;
                Assert.AreEqual(expected[i].Name, actual[i].Name, context);
                Assert.AreEqual(expected[i].IsOptional, actual[i].IsOptional, context);
                Assert.AreEqual(expected[i].HasDefaultValue, actual[i].HasDefaultValue, context);
                if (expected[i].HasDefaultValue)
                    Assert.AreEqual(expected[i].DefaultValue, actual[i].DefaultValue, context);
            }
        }

        var options = factories.SelectMany(m => m.GetParameters()).Where(p => p.IsOptional).ToArray();
        Assert.IsTrue(options.Length > 0);
        foreach (var option in options)
        {
            Assert.IsTrue(new[] { "trimToNull", "truncate", "truncateFraction" }.Contains(option.Name));
            Assert.AreEqual(false, option.DefaultValue, option.Name);
        }
    }

    [TestMethod]
    public void EveryFactoryCreatesFreshUnattachedParametersWithHelperParity()
    {
        foreach (var definition in TypedHelpers())
        {
            var helper = CloseGenericMethod(definition);
            var factory = CloseGenericMethod(FindFactory(definition));
            var arguments = factory.GetParameters().Select(p => SampleArgument(factory, p)).ToArray();
            var first = (SqlParameter)factory.Invoke(null, arguments);
            var second = (SqlParameter)factory.Invoke(null, arguments);
            Assert.IsNotNull(first, factory.ToString());
            Assert.AreNotSame(first, second, factory.ToString());

            using var firstCommand = new SqlCommand();
            using var secondCommand = new SqlCommand();
            firstCommand.Parameters.Add(first);
            secondCommand.Parameters.Add(second);
            using var helperCommand = new SqlCommand();
            helper.Invoke(null, new object[] { helperCommand }.Concat(arguments).ToArray());
            Assert.AreEqual(1, helperCommand.Parameters.Count, helper.ToString());
            AssertEquivalentParameter(first, helperCommand.Parameters[0], factory.ToString());
        }
    }

    [TestMethod]
    public void ScalarFactoriesUseDeclaredSqlTypesAndMapNullableValuesToDbNull()
    {
        var date = new DateTime(2026, 11, 8, 12, 34, 56);
        var offset = new DateTimeOffset(date, TimeSpan.FromHours(3));
        var id = new Guid("11111111-2222-3333-4444-555555555555");
        var families = new (string Name, Type Type, object Input, object Expected, SqlDbType SqlType)[]
        {
            ("Bit", typeof(bool), true, 1, SqlDbType.Bit),
            ("TinyInt", typeof(byte), (byte)255, (byte)255, SqlDbType.TinyInt),
            ("SmallInt", typeof(short), (short)-123, (short)-123, SqlDbType.SmallInt),
            ("Int", typeof(int), -123456, -123456, SqlDbType.Int),
            ("BigInt", typeof(long), 12345678901L, 12345678901L, SqlDbType.BigInt),
            ("SmallMoney", typeof(decimal), 123.45m, 123.45m, SqlDbType.SmallMoney),
            ("Money", typeof(decimal), 123456789.01m, 123456789.01m, SqlDbType.Money),
            ("Real", typeof(float), 1.25f, 1.25f, SqlDbType.Real),
            ("Float", typeof(double), 1.25d, 1.25d, SqlDbType.Float),
            ("Char", typeof(char), 'x', 'x', SqlDbType.Char),
            ("NChar", typeof(char), 'x', 'x', SqlDbType.NChar),
            ("Date", typeof(DateTime), date, date, SqlDbType.Date),
            ("Time", typeof(TimeSpan), TimeSpan.FromHours(2), TimeSpan.FromHours(2), SqlDbType.Time),
            ("SmallDateTime", typeof(DateTime), date, date, SqlDbType.SmallDateTime),
            ("DateTime", typeof(DateTime), date, date, SqlDbType.DateTime),
            ("DateTime2", typeof(DateTime), date, date, SqlDbType.DateTime2),
            ("DateTimeOffset", typeof(DateTimeOffset), offset, offset, SqlDbType.DateTimeOffset),
            ("Guid", typeof(Guid), id, id, SqlDbType.UniqueIdentifier),
            ("Date", typeof(DateOnly), new DateOnly(2026, 11, 8), date.Date, SqlDbType.Date),
            ("Time", typeof(TimeOnly), new TimeOnly(12, 34, 56), new TimeSpan(12, 34, 56), SqlDbType.Time),
            ("RowVersionFromInt64", typeof(long), 123L, BitConverter.GetBytes(123L), SqlDbType.Binary)
        };

        foreach (var family in families)
        foreach (var nullable in new[] { false, true })
        {
            var inputType = nullable ? typeof(Nullable<>).MakeGenericType(family.Type) : family.Type;
            var method = typeof(SqlParam).GetMethod(family.Name, new[] { typeof(string), inputType });
            Assert.IsNotNull(method, family.Name + ": " + inputType);
            var parameter = (SqlParameter)method.Invoke(null, new[] { "@Value", family.Input });
            AssertInput(parameter, family.SqlType, family.Expected);
            if (nullable)
                AssertInput((SqlParameter)method.Invoke(null, new object[] { "@Value", null }), family.SqlType, DBNull.Value);
        }
        AssertInput(SqlParam.Bit("@Value", false), SqlDbType.Bit, 0);
    }

    [TestMethod]
    public void BinaryXmlVariantAndJsonFactoriesPreserveValuesAndMetadata()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        foreach (var item in new (SqlParameter Parameter, SqlDbType Type)[]
        {
            (SqlParam.Binary("@Value", 8, bytes), SqlDbType.Binary),
            (SqlParam.Varbinary("@Value", 8, bytes), SqlDbType.VarBinary),
            (SqlParam.RowVersion("@Value", bytes), SqlDbType.Binary),
            (SqlParam.RowVersionFromBase64String("@Value", "AQIDBAUGBwg="), SqlDbType.Binary),
            (SqlParam.RowVersionFromInt64("@Value", BitConverter.ToInt64(bytes)), SqlDbType.Binary)
        })
        {
            Assert.AreEqual(8, item.Parameter.Size);
            AssertInput(item.Parameter, item.Type, bytes);
        }
        var blob = SqlParam.VarbinaryMax("@Value", bytes);
        AssertInput(blob, SqlDbType.VarBinary, bytes);
        Assert.AreEqual(-1, blob.Size);
        AssertInput(SqlParam.Binary("@Value", 8, null), SqlDbType.Binary, DBNull.Value);
        AssertInput(SqlParam.Varbinary("@Value", 8, null), SqlDbType.VarBinary, DBNull.Value);
        AssertInput(SqlParam.VarbinaryMax("@Value", null), SqlDbType.VarBinary, DBNull.Value);
        AssertInput(SqlParam.RowVersion("@Value", null), SqlDbType.Binary, DBNull.Value);
        AssertInput(SqlParam.RowVersionFromBase64String("@Value", null), SqlDbType.Binary, DBNull.Value);
        AssertInput(SqlParam.Xml("@Value", "<root />"), SqlDbType.Xml, "<root />");
        AssertInput(SqlParam.Xml("@Value", null), SqlDbType.Xml, DBNull.Value);
        AssertInput(SqlParam.SqlVariant("@Value", 123), SqlDbType.Variant, 123);
        AssertInput(SqlParam.SqlVariant("@Value", null), SqlDbType.Variant, DBNull.Value);

        var json = SqlParam.Json("@Value", new { Id = 7, Name = "sample" });
        Assert.AreEqual(SqlDbType.NVarChar, json.SqlDbType);
        Assert.AreEqual(-1, json.Size);
        using var document = JsonDocument.Parse((string)json.Value);
        Assert.AreEqual(7, document.RootElement.GetProperty("Id").GetInt32());
        Assert.AreEqual("sample", document.RootElement.GetProperty("Name").GetString());
        AssertInput(SqlParam.Json<object>("@Value", null), SqlDbType.NVarChar, DBNull.Value);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StringFactoriesHonorNormalizationAndTruncationFlags(bool unicode)
    {
        SqlParameter Sized(string value, bool trim = false, bool truncate = false) => unicode
            ? SqlParam.NVarchar("@Value", 3, value, trim, truncate)
            : SqlParam.Varchar("@Value", 3, value, trim, truncate);
        SqlParameter Max(string value, bool trim = false) => unicode
            ? SqlParam.NVarcharMax("@Value", value, trim)
            : SqlParam.VarcharMax("@Value", value, trim);
        var sqlType = unicode ? SqlDbType.NVarChar : SqlDbType.VarChar;

        AssertInput(Sized(" x "), sqlType, " x ");
        AssertInput(Sized(""), sqlType, "");
        AssertInput(Sized(null), sqlType, DBNull.Value);
        AssertInput(Sized(" \t ", trim: true), sqlType, DBNull.Value);
        AssertInput(Sized("  abc  ", trim: true), sqlType, "abc");
        AssertInput(Sized("  abcdef  ", trim: true, truncate: true), sqlType, "abc");
        AssertInput(Sized("  abcdef  ", truncate: true), sqlType, "  a");
        Assert.AreEqual(3, Sized("abc").Size);
        Assert.ThrowsExactly<ArgumentException>(() => Sized("abcd"));
        Assert.ThrowsExactly<ArgumentException>(() => Sized("  abcd  ", trim: true));
        AssertInput(Max("  sample  "), sqlType, "  sample  ");
        AssertInput(Max("  sample  ", trim: true), sqlType, "sample");
        AssertInput(Max(" \t ", trim: true), sqlType, DBNull.Value);
        AssertInput(Max(null), sqlType, DBNull.Value);
        Assert.AreEqual(-1, Max("sample").Size);
    }

    [TestMethod]
    public void DecimalFactoriesValidateRangeAndTruncateTowardZeroWithDeclaredMetadata()
    {
        foreach (var value in new[] { 1.239m, -1.239m })
        {
            Assert.ThrowsExactly<ArgumentException>(() => SqlParam.Decimal("@Value", 3, 2, value));
            Assert.ThrowsExactly<ArgumentException>(() => SqlParam.Decimal("@Value", 3, 2, (decimal?)value));
            var expected = value > 0 ? 1.23m : -1.23m;
            AssertInput(SqlParam.Decimal("@Value", 3, 2, value, truncateFraction: true), SqlDbType.Decimal, expected);
            AssertInput(SqlParam.Decimal("@Value", 3, 2, (decimal?)value, truncateFraction: true), SqlDbType.Decimal, expected);
        }
        AssertInput(SqlParam.Decimal("@Value", 2, 2, 0.25m), SqlDbType.Decimal, 0.25m);
        AssertInput(SqlParam.Decimal("@Value", 3, 2, 1.2300m), SqlDbType.Decimal, 1.23m);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SqlParam.Decimal("@Value", 3, 2, 10m, truncateFraction: true));
        foreach (var value in new decimal?[] { null, 1.25m })
        {
            var parameter = SqlParam.Decimal("@Value", 6, 2, value);
            Assert.AreEqual((byte)6, parameter.Precision);
            Assert.AreEqual((byte)2, parameter.Scale);
            AssertInput(parameter, SqlDbType.Decimal, (object)value ?? DBNull.Value);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SqlParam.Decimal("@Value", 0, 0, value));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SqlParam.Decimal("@Value", 39, 0, value));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SqlParam.Decimal("@Value", 2, 3, value));
        }
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SqlParam.SmallMoney("@Value", 214748.3648m));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SqlParam.Money("@Value", 922337203685477.5808m));
    }

    [TestMethod]
    public void OutputFactoriesDeclareDirectionsSizesAndDecimalMetadata()
    {
        var cases = new (SqlParameter Parameter, SqlDbType Type)[]
        {
            (SqlParam.BitOutput("@Value"), SqlDbType.Bit),
            (SqlParam.TinyIntOutput("@Value"), SqlDbType.TinyInt),
            (SqlParam.SmallIntOutput("@Value"), SqlDbType.SmallInt),
            (SqlParam.IntOutput("@Value"), SqlDbType.Int),
            (SqlParam.BigIntOutput("@Value"), SqlDbType.BigInt),
            (SqlParam.DecimalOutput("@Value", 19, 4), SqlDbType.Decimal),
            (SqlParam.GuidOutput("@Value"), SqlDbType.UniqueIdentifier),
            (SqlParam.DateTime2Output("@Value"), SqlDbType.DateTime2),
            (SqlParam.DateTimeOffsetOutput("@Value"), SqlDbType.DateTimeOffset),
            (SqlParam.VarcharOutput("@Value", 50), SqlDbType.VarChar),
            (SqlParam.NVarcharOutput("@Value", 50), SqlDbType.NVarChar)
        };
        foreach (var item in cases)
        {
            Assert.AreEqual("@Value", item.Parameter.ParameterName);
            Assert.AreEqual(ParameterDirection.Output, item.Parameter.Direction);
            Assert.AreEqual(item.Type, item.Parameter.SqlDbType);
            Assert.IsNull(item.Parameter.Value);
            if (item.Type is SqlDbType.VarChar or SqlDbType.NVarChar)
                Assert.AreEqual(50, item.Parameter.Size);
            if (item.Type == SqlDbType.Decimal)
            {
                Assert.AreEqual((byte)19, item.Parameter.Precision);
                Assert.AreEqual((byte)4, item.Parameter.Scale);
            }
        }
    }

    [TestMethod]
    public void TableFactoriesDistinguishNullEmptyRegisteredAndAutomaticMappings()
    {
        var rows = new[] { new SqlParamRow { Id = 7, Name = "sample" } };
        AssertTable(SqlParam.Table("@Rows", rows), "SqlParamRowType", new[] { "Key", "Label" }, new object[] { 107, "SAMPLE" });
        AssertTable(SqlParam.Table("@Rows", rows, "AlternateRowType"), "AlternateRowType", new[] { "Key", "Label" }, new object[] { 107, "SAMPLE" });
        AssertTable(SqlParam.Table("@Rows", rows, "AutomaticRowType", "Name,Id"), "AutomaticRowType", new[] { "Name", "Id" }, new object[] { "sample", 7 });
        AssertTable(SqlParam.TableRow("@Rows", rows[0]), "SqlParamRowType", new[] { "Key", "Label" }, new object[] { 107, "SAMPLE" });
        AssertTable(SqlParam.TableRow("@Rows", rows[0], "AutomaticRowType", "Name,Id"), "AutomaticRowType", new[] { "Name", "Id" }, new object[] { "sample", 7 });

        Assert.IsNull(SqlParam.Table("@Rows", (DataTable)null));
        Assert.IsNull(SqlParam.Table<SqlParamRow>("@Rows", null));
        Assert.IsNull(SqlParam.Table<SqlParamRow>("@Rows", null, "AlternateRowType"));
        Assert.IsNull(SqlParam.Table<SqlParamRow>("@Rows", null, "AutomaticRowType", "Id"));
        AssertTable(SqlParam.Table("@Rows", Array.Empty<SqlParamRow>()), "SqlParamRowType", new[] { "Key", "Label" }, null);
        AssertTable(SqlParam.TableRow<SqlParamRow>("@Rows", null), "SqlParamRowType", new[] { "Key", "Label" }, null);
        AssertTable(SqlParam.Table("@Rows", Array.Empty<SqlParamRow>(), "AutomaticRowType", "Id"), "AutomaticRowType", new[] { "Id" }, null);
        Assert.ThrowsExactly<ArgumentException>(() => SqlParam.TableRow("@Rows", new[] { 1, 2 }, "InvalidType", "Id"));

        using var table = new DataTable("ExplicitTableType");
        table.Columns.Add("Id", typeof(int));
        var explicitParameter = SqlParam.Table("@Rows", table);
        AssertTable(explicitParameter, "ExplicitTableType", new[] { "Id" }, null);
        Assert.AreSame(table, explicitParameter.Value);
    }

    [TestMethod]
    public void IntegerIdTableAndTableRowOverloadsPreserveCanonicalTypesAndOmitAllNullCollections()
    {
        var cases = new (Type Type, string TableName, object Id)[]
        {
            (typeof(byte), "TinyIntIdTableType", (byte)7),
            (typeof(short), "SmallIntIdTableType", (short)7),
            (typeof(int), "IntIdTableType", 7),
            (typeof(long), "BigIntIdTableType", 7L)
        };
        foreach (var item in cases)
        {
            var collectionType = typeof(IEnumerable<>).MakeGenericType(item.Type);
            var tableFactory = typeof(SqlParam).GetMethod("Table", new[] { typeof(string), collectionType });
            var rowFactory = typeof(SqlParam).GetMethod("TableRow", new[] { typeof(string), item.Type });
            var helper = typeof(SqlCommandExtensions).GetMethod("AddTableParam", new[] { typeof(SqlCommand), typeof(string), collectionType });
            var values = Array.CreateInstance(item.Type, 1);
            values.SetValue(item.Id, 0);
            var table = (SqlParameter)tableFactory.Invoke(null, new object[] { "@Rows", values });
            AssertTable(table, item.TableName, new[] { "Id" }, new[] { item.Id });
            Assert.AreEqual(item.Type, ((DataTable)table.Value).Columns[0].DataType);
            AssertTable((SqlParameter)rowFactory.Invoke(null, new[] { "@Rows", item.Id }), item.TableName, new[] { "Id" }, new[] { item.Id });
            AssertTable((SqlParameter)tableFactory.Invoke(null, new object[] { "@Rows", Array.CreateInstance(item.Type, 0) }), item.TableName, new[] { "Id" }, null);
            Assert.IsNull(tableFactory.Invoke(null, new object[] { "@Rows", null }), item.Type.Name);
            using var command = new SqlCommand();
            helper.Invoke(null, new object[] { command, "@Rows", null });
            Assert.AreEqual(0, command.Parameters.Count, item.Type.Name + " null collection must be omitted.");
        }
    }

    [TestMethod]
    public void RepositoryParameterArrayOmitsNullEntriesAndRetainsEmptyTvpsWithoutConnecting()
    {
        using var repository = new RepositoryBase(OfflineConnectionString);
        var scalar = SqlParam.Int("@Value", 7);
        var empty = SqlParam.Table("@Rows", Array.Empty<short>());
        using var command = repository.CreateCommand("SELECT @Value", null,
            SqlParam.Table("@Missing", (IEnumerable<short>)null), scalar, empty, null);
        Assert.AreEqual(ConnectionState.Closed, repository.Connection.State);
        Assert.AreEqual(2, command.Parameters.Count);
        Assert.AreSame(scalar, command.Parameters[0]);
        Assert.AreSame(empty, command.Parameters[1]);
        AssertTable(empty, "SmallIntIdTableType", new[] { "Id" }, null);
    }

    [TestMethod]
    public void ReturnValueFactoryIsFreshAndCommandHelpersReuseExistingCustomNamedReturn()
    {
        var first = SqlParam.ReturnValue();
        var second = SqlParam.ReturnValue();
        Assert.AreNotSame(first, second);
        Assert.AreEqual("@ReturnValue", first.ParameterName);
        Assert.AreEqual(SqlDbType.Int, first.SqlDbType);
        Assert.AreEqual(ParameterDirection.ReturnValue, first.Direction);
        Assert.IsNull(first.Value);
        using var command = new SqlCommand();
        Assert.IsNull(command.GetReturnValueParam());
        first.ParameterName = "@Status";
        command.Parameters.Add(first);
        Assert.AreSame(first, command.ReturnValueParam());
        Assert.AreSame(first, command.GetReturnValueParam());
        command.AddReturnValueParam();
        command.AddReturnValueParam();
        Assert.AreEqual(1, command.Parameters.Count);

        using var emptyCommand = new SqlCommand();
        emptyCommand.AddReturnValueParam();
        var created = emptyCommand.ReturnValueParam();
        emptyCommand.AddReturnValueParam();
        Assert.AreSame(created, emptyCommand.GetReturnValueParam());
        Assert.AreEqual(1, emptyCommand.Parameters.Count);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public async Task InvalidReturnConfigurationsFailBeforeOpeningAConnection(int kind)
    {
        using var command = new SqlCommand();
        command.Parameters.AddRange(InvalidReturnParameters(kind));
        var originalCount = command.Parameters.Count;
        Assert.ThrowsExactly<InvalidOperationException>(() => command.ReturnValueParam());
        Assert.ThrowsExactly<InvalidOperationException>(() => command.AddReturnValueParam());
        Assert.ThrowsExactly<InvalidOperationException>(() => command.GetReturnValueParam());
        Assert.AreEqual(originalCount, command.Parameters.Count);

        using var repository = new RepositoryBase(OfflineConnectionString);
        Assert.ThrowsExactly<InvalidOperationException>(() => repository.Execute("SELECT 1", InvalidReturnParameters(kind)));
        Assert.ThrowsExactly<InvalidOperationException>(() => repository.Execute("SELECT 1", cmd => cmd.Parameters.AddRange(InvalidReturnParameters(kind))));
        Assert.ThrowsExactly<InvalidOperationException>(() => repository.ExecuteCommand(cmd =>
        {
            cmd.UseSql("SELECT 1");
            cmd.Parameters.AddRange(InvalidReturnParameters(kind));
        }));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => repository.ExecuteAsync("SELECT 1", InvalidReturnParameters(kind)));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => repository.ExecuteAsync("SELECT 1", cmd => cmd.Parameters.AddRange(InvalidReturnParameters(kind))));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => repository.ExecuteCommandAsync(cmd =>
        {
            cmd.UseSql("SELECT 1");
            cmd.Parameters.AddRange(InvalidReturnParameters(kind));
        }));
        Assert.AreEqual(ConnectionState.Closed, repository.Connection.State);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RepositoryReadsWithFactoryParametersAndOmittedTvps(bool asynchronous)
    {
        using var repository = new RepositoryBase(new AppSettings().ConnectionStrings.DatabaseConnection);
        var parameters = new[]
        {
            SqlParam.Int("@Id", 7), SqlParam.NVarchar("@Name", 3, "  abcdef  ", trimToNull: true, truncate: true),
            SqlParam.Table("@Omitted", (IEnumerable<short>)null)
        };
        const string sql = "SELECT @Id AS Id, @Name AS Name";
        var row = asynchronous
            ? await repository.ReadAsAsync<SqlParamRow>(sql, CancellationToken.None, parameters)
            : repository.ReadAs<SqlParamRow>(sql, parameters);
        Assert.IsNotNull(row);
        Assert.AreEqual(7, row.Id);
        Assert.AreEqual("abc", row.Name);
    }

    [TestMethod]
    [DataRow(false, 0)]
    [DataRow(false, 1)]
    [DataRow(false, 2)]
    [DataRow(true, 0)]
    [DataRow(true, 1)]
    [DataRow(true, 2)]
    public async Task RepositoryExecutionPreservesOutputAndReturnParameterReferences(bool asynchronous, int entryPoint)
    {
        var connectionString = new AppSettings().ConnectionStrings.DatabaseConnection;
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        using var repository = new RepositoryBase(transaction, connectionString);
        using (var setup = new SqlCommand(null, connection, transaction))
        {
            setup.UseSql("CREATE PROCEDURE #SqlParamReturn @Input int, @Result int OUTPUT AS " +
                "BEGIN SET NOCOUNT ON; SET @Result = @Input + 1; RETURN 17; END");
            await setup.ExecuteNonQueryAsync();
        }

        var input = SqlParam.Int("@Input", 41);
        var output = SqlParam.IntOutput("@Result");
        var returned = SqlParam.ReturnValue();
        if (entryPoint != 0)
            returned.ParameterName = "@CustomStatus";
        var parameters = new[] { input, output, returned };
        void Configure(SqlCommand command)
        {
            command.UseProcedure("#SqlParamReturn");
            command.Parameters.AddRange(parameters);
        }

        var status = (asynchronous, entryPoint) switch
        {
            (false, 0) => repository.Execute("#SqlParamReturn", parameters),
            (false, 1) => repository.Execute("#SqlParamReturn", Configure),
            (false, 2) => repository.ExecuteCommand(Configure),
            (true, 0) => await repository.ExecuteAsync("#SqlParamReturn", CancellationToken.None, parameters),
            (true, 1) => await repository.ExecuteAsync("#SqlParamReturn", Configure),
            _ => await repository.ExecuteCommandAsync(Configure)
        };
        Assert.AreEqual(17, status);
        Assert.AreEqual(17, returned.Value, "The caller's original return parameter must receive the result.");
        Assert.AreEqual(42, output.Value, "The caller's original output parameter must receive the result.");
        Assert.AreEqual(ConnectionState.Open, connection.State);
        transaction.Rollback();
    }

    private static MethodInfo[] TypedHelpers() => typeof(SqlCommandExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(m => m.Name.StartsWith("Add", StringComparison.Ordinal) && m.Name.EndsWith("Param", StringComparison.Ordinal))
        .ToArray();

    private static MethodInfo FindFactory(MethodInfo helper)
    {
        var name = helper.Name.Substring(3, helper.Name.Length - 8);
        var signature = helper.GetParameters().Skip(1).Select(p => p.ParameterType.ToString()).ToArray();
        var matches = typeof(SqlParam).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == name && m.GetGenericArguments().Length == helper.GetGenericArguments().Length
                && m.GetParameters().Select(p => p.ParameterType.ToString()).SequenceEqual(signature)).ToArray();
        Assert.AreEqual(1, matches.Length, helper.ToString());
        return matches[0];
    }

    private static MethodInfo CloseGenericMethod(MethodInfo method) => method.IsGenericMethodDefinition
        ? method.MakeGenericMethod(typeof(SqlParamRow)) : method;

    private static object SampleArgument(MethodInfo factory, ParameterInfo parameter)
    {
        if (parameter.Name == "parameterName") return "@Value";
        if (parameter.Name == "tableName") return "SampleTableType";
        if (parameter.Name == "columnNames") return "Name,Id";
        if (parameter.Name == "precision") return (byte)6;
        if (parameter.Name == "scale") return (byte)2;
        if (parameter.Name == "size") return 8;
        if (parameter.IsOptional) return parameter.DefaultValue;
        var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        if (type == typeof(DataTable)) return new DataTable("SampleTableType").AddColumn<int>("Id");
        if (type == typeof(SqlParamRow)) return new SqlParamRow { Id = 7, Name = "sample" };
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = type.GetGenericArguments()[0];
            var array = Array.CreateInstance(elementType, 1);
            array.SetValue(elementType == typeof(SqlParamRow)
                ? new SqlParamRow { Id = 7, Name = "sample" }
                : Convert.ChangeType(7, elementType, CultureInfo.InvariantCulture), 0);
            return array;
        }
        if (type == typeof(string)) return factory.Name == "RowVersionFromBase64String" ? "AQIDBAUGBwg=" : "sample";
        if (type == typeof(byte[])) return new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        if (type == typeof(DateTime)) return new DateTime(2026, 11, 8);
        if (type == typeof(DateTimeOffset)) return new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
        if (type == typeof(TimeSpan)) return TimeSpan.FromHours(2);
        if (type == typeof(DateOnly)) return new DateOnly(2026, 11, 8);
        if (type == typeof(TimeOnly)) return new TimeOnly(12, 34);
        if (type == typeof(Guid)) return new Guid("11111111-2222-3333-4444-555555555555");
        if (type == typeof(char)) return 'x';
        if (type == typeof(bool)) return true;
        if (type == typeof(object)) return 7;
        return Convert.ChangeType(1, type, CultureInfo.InvariantCulture);
    }

    private static void AssertInput(SqlParameter parameter, SqlDbType expectedType, object expectedValue)
    {
        Assert.AreEqual("@Value", parameter.ParameterName);
        Assert.AreEqual(ParameterDirection.Input, parameter.Direction);
        Assert.AreEqual(expectedType, parameter.SqlDbType);
        AssertValue(expectedValue, parameter.Value, expectedType.ToString());
    }

    private static void AssertTable(SqlParameter parameter, string typeName, string[] columns, object[] row)
    {
        Assert.IsNotNull(parameter);
        Assert.AreEqual("@Rows", parameter.ParameterName);
        Assert.AreEqual(ParameterDirection.Input, parameter.Direction);
        Assert.AreEqual(SqlDbType.Structured, parameter.SqlDbType);
        Assert.AreEqual(typeName, parameter.TypeName);
        Assert.IsInstanceOfType<DataTable>(parameter.Value);
        var table = (DataTable)parameter.Value;
        Assert.AreEqual(typeName, table.TableName);
        CollectionAssert.AreEqual(columns, table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());
        Assert.AreEqual(row == null ? 0 : 1, table.Rows.Count);
        if (row != null)
            CollectionAssert.AreEqual(row, table.Rows[0].ItemArray);
    }

    private static void AssertEquivalentParameter(SqlParameter expected, SqlParameter actual, string context)
    {
        Assert.AreEqual(expected.ParameterName, actual.ParameterName, context);
        Assert.AreEqual(expected.Direction, actual.Direction, context);
        Assert.AreEqual(expected.SqlDbType, actual.SqlDbType, context);
        Assert.AreEqual(expected.Size, actual.Size, context);
        Assert.AreEqual(expected.Precision, actual.Precision, context);
        Assert.AreEqual(expected.Scale, actual.Scale, context);
        Assert.AreEqual(expected.TypeName, actual.TypeName, context);
        if (expected.Value is DataTable expectedTable && actual.Value is DataTable actualTable)
        {
            CollectionAssert.AreEqual(expectedTable.Columns.Cast<DataColumn>().Select(c => (c.ColumnName, c.DataType)).ToArray(),
                actualTable.Columns.Cast<DataColumn>().Select(c => (c.ColumnName, c.DataType)).ToArray(), context);
            Assert.AreEqual(expectedTable.Rows.Count, actualTable.Rows.Count, context);
            for (var i = 0; i < expectedTable.Rows.Count; i++)
                CollectionAssert.AreEqual(expectedTable.Rows[i].ItemArray, actualTable.Rows[i].ItemArray, context);
        }
        else
            AssertValue(expected.Value, actual.Value, context);
    }

    private static void AssertValue(object expected, object actual, string context)
    {
        if (expected is byte[] bytes)
        {
            Assert.IsInstanceOfType<byte[]>(actual, context);
            CollectionAssert.AreEqual(bytes, (byte[])actual, context);
        }
        else
            Assert.AreEqual(expected, actual, context);
    }

    private static SqlParameter[] InvalidReturnParameters(int kind) => kind switch
    {
        0 => new[] { SqlParam.Int("@rEtUrNvAlUe", 7) },
        1 => new[] { SqlParam.IntOutput("RETURNVALUE") },
        2 => new[] { new SqlParameter("@Status", SqlDbType.BigInt) { Direction = ParameterDirection.ReturnValue } },
        _ => new[] { SqlParam.ReturnValue(), new SqlParameter("@Other", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue } }
    };
}

public sealed class SqlParamRow
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[MapperFor(typeof(SqlParamRow), RequiredMethod.BothForDataTable)]
public static class SqlParamRowMapper
{
    public static DataTable CreateDataTable() => new DataTable("SqlParamRowType")
        .AddColumn<int>("Key").AddColumn<string>("Label");

    public static object[] CreateDataRow(SqlParamRow row) => new object[] { row.Id + 100, row.Name.ToUpperInvariant() };
}
