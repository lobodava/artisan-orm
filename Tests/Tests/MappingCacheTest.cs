using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using Artisan.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Tests;

[TestClass]
public class MappingCacheTest
{
    public sealed class CacheRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public sealed class ReaderRow
    {
        public int Id { get; set; }
        public int Name { get; set; }
        public int Value { get; set; }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void TableNameAndColumnSelectionHaveSeparateCacheIdentity(bool reverseOrder)
    {
        var rows = new[] { new CacheRow { Id = 7, Name = "sample" } };
        using var first = reverseOrder ? rows.AsDataTable("Id") : rows.AsDataTable(null, "Id");
        using var second = reverseOrder ? rows.AsDataTable(null, "Id") : rows.AsDataTable("Id");
        var selected = reverseOrder ? second : first;
        var complete = reverseOrder ? first : second;

        Assert.AreEqual(string.Empty, selected.TableName);
        CollectionAssert.AreEqual(new[] { "Id" }, selected.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());
        Assert.AreEqual("Id", complete.TableName);
        CollectionAssert.AreEqual(new[] { "Id", "Name" }, complete.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());
        Assert.AreEqual("sample", complete.Rows[0]["Name"]);
    }

    [TestMethod]
    public void TableNamesContainingSeparatorsDoNotConsumeColumnNames()
    {
        var rows = new[] { new CacheRow { Id = 7, Name = "sample" } };
        using var complete = rows.AsDataTable("CacheTable", "Id,Name");
        using var selected = rows.AsDataTable("CacheTable+Id", "Name");
        using var reversed = rows.AsDataTable("CacheTable", "Name,Id");

        Assert.AreEqual("CacheTable+Id", selected.TableName);
        CollectionAssert.AreEqual(new[] { "Name" }, selected.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());
        Assert.AreEqual("sample", selected.Rows[0][0]);
        CollectionAssert.AreEqual(new[] { "Name", "Id" }, reversed.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());
        CollectionAssert.AreEqual(new object[] { "sample", 7 }, reversed.Rows[0].ItemArray);
    }

    [TestMethod]
    public void SameFullNameInDifferentAssembliesUsesDifferentDataTableMappers()
    {
        var firstType = CreateRowType();
        var secondType = CreateRowType();
        Assert.AreEqual(firstType.FullName, secondType.FullName);
        Assert.AreNotEqual(firstType, secondType);

        using var first = CreateDynamicDataTable(firstType, 11);
        using var second = CreateDynamicDataTable(secondType, 22);

        Assert.AreEqual(11, first.Rows[0]["Id"]);
        Assert.AreEqual(22, second.Rows[0]["Id"]);
    }

    [TestMethod]
    public void PublicReaderMapperCacheSeparatesTargetTypesWithTheSameKey()
    {
        var key = Guid.NewGuid().ToString("N");
        Assert.IsTrue(MappingManager.AddAutoCreateObjectFunc<CacheRow>(key, _ => new CacheRow { Id = 11 }));
        Assert.IsTrue(MappingManager.AddAutoCreateObjectFunc<ReaderRow>(key, _ => new ReaderRow { Id = 22 }));

        Assert.AreEqual(11, MappingManager.GetAutoCreateObjectFunc<CacheRow>(key)(null).Id);
        Assert.AreEqual(22, MappingManager.GetAutoCreateObjectFunc<ReaderRow>(key)(null).Id);
    }

    [TestMethod]
    public void ReaderColumnAliasesCannotCollideWithMultipleFields()
    {
        using var connection = new SqlConnection(new AppSettings().ConnectionStrings.DatabaseConnection);
        connection.Open();
        using (var command = new SqlCommand("SELECT 1 AS Id, 2 AS Name, 3 AS Value", connection))
        using (var reader = command.ExecuteReader())
        {
            Assert.IsTrue(reader.Read());
            var row = reader.CreateObject<ReaderRow>();
            Assert.AreEqual(3, row.Value);
        }

        using (var command = new SqlCommand("SELECT 4 AS [Id:Int32|Name], 5 AS Value", connection))
        using (var reader = command.ExecuteReader())
        {
            Assert.IsTrue(reader.Read());
            var row = reader.CreateObject<ReaderRow>();
            Assert.AreEqual(0, row.Id);
            Assert.AreEqual(0, row.Name);
            Assert.AreEqual(5, row.Value);
        }
    }

    private static Type CreateRowType()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("CacheAudit" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("Main").DefineType("CacheIdentity.Row", TypeAttributes.Public);
        type.DefineDefaultConstructor(MethodAttributes.Public);
        var field = type.DefineField("_id", typeof(int), FieldAttributes.Private);
        var property = type.DefineProperty("Id", PropertyAttributes.None, typeof(int), null);
        var getter = type.DefineMethod("get_Id", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(int), Type.EmptyTypes);
        var getterIl = getter.GetILGenerator();
        getterIl.Emit(OpCodes.Ldarg_0);
        getterIl.Emit(OpCodes.Ldfld, field);
        getterIl.Emit(OpCodes.Ret);
        property.SetGetMethod(getter);
        var setter = type.DefineMethod("set_Id", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), new[] { typeof(int) });
        var setterIl = setter.GetILGenerator();
        setterIl.Emit(OpCodes.Ldarg_0);
        setterIl.Emit(OpCodes.Ldarg_1);
        setterIl.Emit(OpCodes.Stfld, field);
        setterIl.Emit(OpCodes.Ret);
        property.SetSetMethod(setter);
        return type.CreateType();
    }

    private static DataTable CreateDynamicDataTable(Type type, int id)
    {
        var row = Activator.CreateInstance(type);
        type.GetProperty("Id").SetValue(row, id);
        var rows = Array.CreateInstance(type, 1);
        rows.SetValue(row, 0);
        var method = typeof(Artisan.Orm.DataTableExtensions).GetMethods()
            .Single(m => m.Name == nameof(Artisan.Orm.DataTableExtensions.AsDataTable) && m.GetParameters().Last().ParameterType == typeof(string[]));
        return (DataTable)method.MakeGenericMethod(type).Invoke(null, new object[] { rows, "DynamicCacheTable", null });
    }
}
