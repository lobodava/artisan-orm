using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Artisan.Orm
{

	public static partial class SqlCommandExtensions
	{
		/// <summary>Configures the command to invoke a stored procedure with the given <paramref name="procedureName"/>.</summary>
		public static void UseProcedure(this SqlCommand cmd, string procedureName)
		{
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.CommandText = procedureName;
		}

		/// <summary>Configures the command to execute the given <paramref name="sql"/> as a text batch.</summary>
		public static void UseSql(this SqlCommand cmd, string sql)
		{
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = sql;
		}

		// http://blogs.msmvps.com/jcoehoorn/blog/2014/05/12/can-we-stop-using-addwithvalue-already/


		/// <summary>Adds an input <c>bit</c> parameter named <paramref name="parameterName"/> with <paramref name="value"/>.</summary>
		public static void AddBitParam(this SqlCommand cmd, string parameterName, bool value)
		{
			cmd.Parameters.Add(SqlParam.Bit(parameterName, value));
		}

		/// <inheritdoc cref="AddBitParam(SqlCommand, string, bool)"/>
		public static void AddBitParam(this SqlCommand cmd, string parameterName, bool? value)
		{
			cmd.Parameters.Add(SqlParam.Bit(parameterName, value));
		}


		/// <summary>Adds an input <c>tinyint</c> parameter.</summary>
		public static void AddTinyIntParam(this SqlCommand cmd, string parameterName, byte value)
		{
			cmd.Parameters.Add(SqlParam.TinyInt(parameterName, value));
		}

		/// <inheritdoc cref="AddTinyIntParam(SqlCommand, string, byte)"/>
		public static void AddTinyIntParam(this SqlCommand cmd, string parameterName, byte? value)
		{
			cmd.Parameters.Add(SqlParam.TinyInt(parameterName, value));
		}


		/// <summary>Adds an input <c>smallint</c> parameter.</summary>
		public static void AddSmallIntParam(this SqlCommand cmd, string parameterName, short value)
		{
			cmd.Parameters.Add(SqlParam.SmallInt(parameterName, value));
		}

		/// <inheritdoc cref="AddSmallIntParam(SqlCommand, string, short)"/>
		public static void AddSmallIntParam(this SqlCommand cmd, string parameterName, short? value)
		{
			cmd.Parameters.Add(SqlParam.SmallInt(parameterName, value));
		}


		/// <summary>Adds an input <c>int</c> parameter.</summary>
		public static void AddIntParam(this SqlCommand cmd, string parameterName, int value)
		{
			cmd.Parameters.Add(SqlParam.Int(parameterName, value));
		}

		/// <inheritdoc cref="AddIntParam(SqlCommand, string, int)"/>
		public static void AddIntParam(this SqlCommand cmd, string parameterName, int? value)
		{
			cmd.Parameters.Add(SqlParam.Int(parameterName, value));
		}


		/// <summary>Adds an input <c>bigint</c> parameter.</summary>
		public static void AddBigIntParam(this SqlCommand cmd, string parameterName, long value)
		{
			cmd.Parameters.Add(SqlParam.BigInt(parameterName, value));
		}

		/// <inheritdoc cref="AddBigIntParam(SqlCommand, string, long)"/>
		public static void AddBigIntParam(this SqlCommand cmd, string parameterName, long? value)
		{
			cmd.Parameters.Add(SqlParam.BigInt(parameterName, value));
		}


		/// <summary>Adds an input <c>decimal(<paramref name="precision"/>, <paramref name="scale"/>)</c> parameter.
		/// When <paramref name="truncateFraction"/> is <c>true</c>, fractional digits beyond <paramref name="scale"/>
		/// are discarded toward zero; when <c>false</c>, a value requiring fractional rounding is rejected.
		/// Insignificant trailing zeroes do not require rounding. Precision must be 1 through 38,
		/// and scale must not exceed precision.</summary>
		public static void AddDecimalParam(this SqlCommand cmd, string parameterName, byte precision, byte scale, decimal value, bool truncateFraction = false)
		{
			cmd.Parameters.Add(SqlParam.CreateDecimal(parameterName, precision, scale, value, truncateFraction, cmd));
		}

		/// <inheritdoc cref="AddDecimalParam(SqlCommand, string, byte, byte, decimal, bool)"/>
		public static void AddDecimalParam(this SqlCommand cmd, string parameterName, byte precision, byte scale, decimal? value, bool truncateFraction = false)
		{
			cmd.Parameters.Add(SqlParam.CreateDecimal(parameterName, precision, scale, value, truncateFraction, cmd));
		}




		/// <summary>Adds an input <c>smallmoney</c> parameter.</summary>
		public static void AddSmallMoneyParam(this SqlCommand cmd, string parameterName, decimal value)
		{
			cmd.Parameters.Add(SqlParam.CreateSmallMoney(parameterName, value, cmd));
		}

		/// <inheritdoc cref="AddSmallMoneyParam(SqlCommand, string, decimal)"/>
		public static void AddSmallMoneyParam(this SqlCommand cmd, string parameterName, decimal? value)
		{
			cmd.Parameters.Add(SqlParam.CreateSmallMoney(parameterName, value, cmd));
		}


		/// <summary>Adds an input <c>money</c> parameter.</summary>
		public static void AddMoneyParam(this SqlCommand cmd, string parameterName, decimal value)
		{
			cmd.Parameters.Add(SqlParam.CreateMoney(parameterName, value, cmd));
		}

		/// <inheritdoc cref="AddMoneyParam(SqlCommand, string, decimal)"/>
		public static void AddMoneyParam(this SqlCommand cmd, string parameterName, decimal? value)
		{
			cmd.Parameters.Add(SqlParam.CreateMoney(parameterName, value, cmd));
		}


		/// <summary>Adds an input <c>real</c> (single-precision float) parameter.</summary>
		public static void AddRealParam(this SqlCommand cmd, string parameterName, float value)
		{
			cmd.Parameters.Add(SqlParam.CreateReal(parameterName, value, cmd));
		}

		/// <inheritdoc cref="AddRealParam(SqlCommand, string, float)"/>
		public static void AddRealParam(this SqlCommand cmd, string parameterName, float? value)
		{
			cmd.Parameters.Add(SqlParam.CreateReal(parameterName, value, cmd));
		}


		/// <summary>Adds an input <c>float</c> (double-precision) parameter.</summary>
		public static void AddFloatParam(this SqlCommand cmd, string parameterName, double value)
		{
			cmd.Parameters.Add(SqlParam.CreateFloat(parameterName, value, cmd));
		}

		/// <inheritdoc cref="AddFloatParam(SqlCommand, string, double)"/>
		public static void AddFloatParam(this SqlCommand cmd, string parameterName, double? value)
		{
			cmd.Parameters.Add(SqlParam.CreateFloat(parameterName, value, cmd));
		}


		/// <summary>Adds an input <c>char(1)</c> parameter.</summary>
		public static void AddCharParam(this SqlCommand cmd, string parameterName, char value)
		{
			cmd.Parameters.Add(SqlParam.Char(parameterName, value));
		}

		/// <inheritdoc cref="AddCharParam(SqlCommand, string, char)"/>
		public static void AddCharParam(this SqlCommand cmd, string parameterName, char? value)
		{
			cmd.Parameters.Add(SqlParam.Char(parameterName, value));
		}


		/// <summary>Adds an input <c>nchar(1)</c> parameter.</summary>
		public static void AddNCharParam(this SqlCommand cmd, string parameterName, char value)
		{
			cmd.Parameters.Add(SqlParam.NChar(parameterName, value));
		}

		/// <inheritdoc cref="AddNCharParam(SqlCommand, string, char)"/>
		public static void AddNCharParam(this SqlCommand cmd, string parameterName, char? value)
		{
			cmd.Parameters.Add(SqlParam.NChar(parameterName, value));
		}


		/// <summary>Adds an input <c>varchar(<paramref name="size"/>)</c> parameter.</summary>
		/// <remarks>When <paramref name="trimToNull"/>=<c>true</c>, the value is trimmed and an empty result is sent as <c>NULL</c>.
		/// After trimming, strings longer than <paramref name="size"/> are truncated when <paramref name="truncate"/>=<c>true</c>;
		/// otherwise an <see cref="ArgumentException"/> is thrown before adding the parameter.</remarks>
		public static void AddVarcharParam(this SqlCommand cmd, string parameterName, int size, string? value, bool trimToNull = false, bool truncate = false)
		{
			cmd.Parameters.Add(SqlParam.CreateVarchar(parameterName, size, value, trimToNull, truncate, cmd));
		}

		/// <summary>Adds an input <c>nvarchar(<paramref name="size"/>)</c> parameter.</summary>
		/// <remarks>When <paramref name="trimToNull"/>=<c>true</c>, the value is trimmed and an empty result is sent as <c>NULL</c>.
		/// After trimming, strings longer than <paramref name="size"/> are truncated when <paramref name="truncate"/>=<c>true</c>;
		/// otherwise an <see cref="ArgumentException"/> is thrown before adding the parameter.</remarks>
		public static void AddNVarcharParam(this SqlCommand cmd, string parameterName, int size, string? value, bool trimToNull = false, bool truncate = false)
		{
			cmd.Parameters.Add(SqlParam.CreateNVarchar(parameterName, size, value, trimToNull, truncate, cmd));
		}


		/// <summary>Adds an input <c>varchar(max)</c> parameter.</summary>
		/// <remarks>When <paramref name="trimToNull"/>=<c>true</c>, the value is trimmed and an empty result is sent as <c>NULL</c>.</remarks>
		public static void AddVarcharMaxParam(this SqlCommand cmd, string parameterName, string? value, bool trimToNull = false)
		{
			cmd.Parameters.Add(SqlParam.VarcharMax(parameterName, value, trimToNull));
		}

		/// <summary>Adds an input <c>nvarchar(max)</c> parameter.</summary>
		/// <remarks>When <paramref name="trimToNull"/>=<c>true</c>, the value is trimmed and an empty result is sent as <c>NULL</c>.</remarks>
		public static void AddNVarcharMaxParam(this SqlCommand cmd, string parameterName, string? value, bool trimToNull = false)
		{
			cmd.Parameters.Add(SqlParam.NVarcharMax(parameterName, value, trimToNull));
		}


		/// <summary>Adds an input <c>binary(<paramref name="size"/>)</c> parameter (fixed-length).</summary>
		public static void AddBinaryParam(this SqlCommand cmd, string parameterName, int size, byte[]? value)
		{
			cmd.Parameters.Add(SqlParam.Binary(parameterName, size, value));
		}

		/// <summary>Adds an input <c>varbinary(<paramref name="size"/>)</c> parameter (variable-length, capped at <paramref name="size"/>).</summary>
		public static void AddVarbinaryParam(this SqlCommand cmd, string parameterName, int size, byte[]? value)
		{
			cmd.Parameters.Add(SqlParam.Varbinary(parameterName, size, value));
		}

		/// <summary>Adds an input <c>varbinary(max)</c> parameter -- for arbitrary-length blobs.</summary>
		public static void AddVarbinaryMaxParam(this SqlCommand cmd, string parameterName, byte[]? value)
		{
			cmd.Parameters.Add(SqlParam.VarbinaryMax(parameterName, value));
		}


		/// <summary>Adds an input <c>date</c> parameter (time component is dropped on the SQL side).</summary>
		public static void AddDateParam(this SqlCommand cmd, string parameterName, DateTime value)
		{
			cmd.Parameters.Add(SqlParam.Date(parameterName, value));
		}

		/// <inheritdoc cref="AddDateParam(SqlCommand, string, DateTime)"/>
		public static void AddDateParam(this SqlCommand cmd, string parameterName, DateTime? value)
		{
			cmd.Parameters.Add(SqlParam.Date(parameterName, value));
		}


		/// <summary>Adds an input <c>time</c> parameter.</summary>
		public static void AddTimeParam(this SqlCommand cmd, string parameterName, TimeSpan value)
		{
			cmd.Parameters.Add(SqlParam.Time(parameterName, value));
		}

		/// <inheritdoc cref="AddTimeParam(SqlCommand, string, TimeSpan)"/>
		public static void AddTimeParam(this SqlCommand cmd, string parameterName, TimeSpan? value)
		{
			cmd.Parameters.Add(SqlParam.Time(parameterName, value));
		}


		/// <summary>Adds an input <c>smalldatetime</c> parameter (1-minute precision, 1900-2079 range).</summary>
		public static void AddSmallDateTimeParam(this SqlCommand cmd, string parameterName, DateTime value)
		{
			cmd.Parameters.Add(SqlParam.SmallDateTime(parameterName, value));
		}

		/// <inheritdoc cref="AddSmallDateTimeParam(SqlCommand, string, DateTime)"/>
		public static void AddSmallDateTimeParam(this SqlCommand cmd, string parameterName, DateTime? value)
		{
			cmd.Parameters.Add(SqlParam.SmallDateTime(parameterName, value));
		}


		/// <summary>Adds an input <c>datetime</c> parameter (~3.33ms precision, 1753-9999 range).
		/// Prefer <see cref="AddDateTime2Param(SqlCommand, string, DateTime)"/> for new schemas.</summary>
		public static void AddDateTimeParam(this SqlCommand cmd, string parameterName, DateTime value)
		{
			cmd.Parameters.Add(SqlParam.DateTime(parameterName, value));
		}

		/// <inheritdoc cref="AddDateTimeParam(SqlCommand, string, DateTime)"/>
		public static void AddDateTimeParam(this SqlCommand cmd, string parameterName, DateTime? value)
		{
			cmd.Parameters.Add(SqlParam.DateTime(parameterName, value));
		}


		/// <summary>Adds an input <c>datetime2(7)</c> parameter (100ns precision, 0001-9999 range).</summary>
		public static void AddDateTime2Param(this SqlCommand cmd, string parameterName, DateTime value)
		{
			cmd.Parameters.Add(SqlParam.DateTime2(parameterName, value));
		}

		/// <inheritdoc cref="AddDateTime2Param(SqlCommand, string, DateTime)"/>
		public static void AddDateTime2Param(this SqlCommand cmd, string parameterName, DateTime? value)
		{
			cmd.Parameters.Add(SqlParam.DateTime2(parameterName, value));
		}


		/// <summary>Adds an input <c>datetimeoffset(7)</c> parameter -- preserves the time-zone offset.</summary>
		public static void AddDateTimeOffsetParam(this SqlCommand cmd, string parameterName, DateTimeOffset value)
		{
			cmd.Parameters.Add(SqlParam.DateTimeOffset(parameterName, value));
		}

		/// <inheritdoc cref="AddDateTimeOffsetParam(SqlCommand, string, DateTimeOffset)"/>
		public static void AddDateTimeOffsetParam(this SqlCommand cmd, string parameterName, DateTimeOffset? value)
		{
			cmd.Parameters.Add(SqlParam.DateTimeOffset(parameterName, value));
		}


		/// <summary>Adds an input <c>uniqueidentifier</c> parameter.</summary>
		public static void AddGuidParam(this SqlCommand cmd, string parameterName, Guid value)
		{
			cmd.Parameters.Add(SqlParam.Guid(parameterName, value));
		}

		/// <inheritdoc cref="AddGuidParam(SqlCommand, string, Guid)"/>
		public static void AddGuidParam(this SqlCommand cmd, string parameterName, Guid? value)
		{
			cmd.Parameters.Add(SqlParam.Guid(parameterName, value));
		}


		/// <summary>Adds an input <c>binary(8)</c> parameter holding a SQL Server <c>rowversion</c>/<c>timestamp</c> value (raw bytes).</summary>
		public static void AddRowVersionParam(this SqlCommand cmd, string parameterName, byte[] value)
		{
			cmd.Parameters.Add(SqlParam.RowVersion(parameterName, value));
		}

		/// <summary>Decodes a base64 string and adds it as a <c>binary(8)</c> rowversion parameter.</summary>
		public static void AddRowVersionFromBase64StringParam(this SqlCommand cmd, string parameterName, string value)
		{
			cmd.Parameters.Add(SqlParam.RowVersionFromBase64String(parameterName, value));
		}

		/// <summary>Encodes the <see cref="long"/> as 8 bytes and adds it as a <c>binary(8)</c> rowversion parameter.</summary>
		public static void AddRowVersionFromInt64Param(this SqlCommand cmd, string parameterName, long value)
		{
			cmd.Parameters.Add(SqlParam.RowVersionFromInt64(parameterName, value));
		}

		/// <inheritdoc cref="AddRowVersionFromInt64Param(SqlCommand, string, long)"/>
		public static void AddRowVersionFromInt64Param(this SqlCommand cmd, string parameterName, long? value)
		{
			cmd.Parameters.Add(SqlParam.RowVersionFromInt64(parameterName, value));
		}


		/// <summary>Adds an input <c>sql_variant</c> parameter -- accepts any boxed value of a SQL-compatible type.</summary>
		public static void AddSqlVariantParam(this SqlCommand cmd, string parameterName, object value)
		{
			cmd.Parameters.Add(SqlParam.SqlVariant(parameterName, value));
		}

		/// <summary>Adds an input <c>xml</c> parameter from a serialised XML string.</summary>
		public static void AddXmlParam(this SqlCommand cmd, string parameterName, string value)
		{
			cmd.Parameters.Add(SqlParam.Xml(parameterName, value));
		}


		/// <summary>Adds a structured (table-valued) parameter built from <paramref name="dataTable"/>.
		/// The <see cref="DataTable.TableName"/> must match the user-defined SQL table type expected by the procedure.
		/// No-op when <paramref name="dataTable"/> is <c>null</c>.</summary>
		public static void AddTableParam(this SqlCommand cmd, string parameterName, DataTable? dataTable)
		{
			var parameter = SqlParam.Table(parameterName, dataTable);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}

		/// <summary>Adds a TVP populated with <paramref name="ids"/> against the canonical
		/// <c>TinyIntIdTableType</c> (single-column <c>Id tinyint not null</c>).</summary>
		public static void AddTableParam(this SqlCommand cmd, string parameterName, IEnumerable<byte>? ids)
		{
			var parameter = SqlParam.Table(parameterName, ids);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}

		/// <summary>Adds a TVP populated with <paramref name="ids"/> against the canonical <c>SmallIntIdTableType</c>.</summary>
		public static void AddTableParam(this SqlCommand cmd, string parameterName, IEnumerable<short>? ids)
		{
			var parameter = SqlParam.Table(parameterName, ids);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}

		/// <summary>
		/// <para>Convert <paramref name="ids"/> param to DataTable with name <c>ToIntIdDataTable</c> and <c>Id</c> column</para>
		/// <para>and add <paramref name="parameterName"/> SqlParameter to the <paramref name="cmd"/> SqlCommand</para>
		/// <para>Database must have the following user-defined table type:</para>
		/// <para><c>create type IntIdTableType as table (Id int not null primary key clustered)</c></para>
		/// </summary>
		/// <param name="cmd">SqlCommand</param>
		/// <param name="parameterName">The name of parameter in stored procedure or in SQL text</param>
		/// <param name="ids">Collection of int Ids</param>
		public static void AddTableParam(this SqlCommand cmd, string parameterName, IEnumerable<int>? ids)
		{
			var parameter = SqlParam.Table(parameterName, ids);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}

		/// <summary>Adds a TVP populated with <paramref name="ids"/> against the canonical <c>BigIntIdTableType</c>.</summary>
		public static void AddTableParam(this SqlCommand cmd, string parameterName, IEnumerable<long>? ids)
		{
			var parameter = SqlParam.Table(parameterName, ids);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}

		/// <summary>Adds a TVP built via the registered mapper for <typeparamref name="T"/>
		/// (<c>CreateDataTable</c>/<c>CreateDataRow</c>).</summary>
		/// <remarks>
		/// Requires three pieces wired together:
		/// <list type="number">
		/// <item>A user-defined SQL table type (e.g. <c>create type RecordTableType as table (Id int, Name nvarchar(30))</c>).</item>
		/// <item>A <c>[MapperFor(typeof(T))]</c> static class with <c>CreateDataTable</c> (its <see cref="DataTable.TableName"/>
		/// must equal the SQL type name) and <c>CreateDataRow(T)</c> producing an <c>object[]</c> in column order.</item>
		/// <item>A stored procedure parameter typed as <c>readonly</c> of that table type.</item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// // 1) C# mapper:
		/// [MapperFor(typeof(Record), RequiredMethod.BothForDataTable)]
		/// public static class RecordMapper
		/// {
		///     public static DataTable CreateDataTable() => new DataTable("RecordTableType")
		///         .AddColumn<int>   ("Id")
		///         .AddColumn<string>("Name");
		///
		///     public static object[] CreateDataRow(Record r) => new object[] { r.Id, r.Name };
		/// }
		///
		/// // 2) Call site:
		/// using var cmd = repo.CreateCommand();
		/// cmd.UseProcedure("dbo.SaveRecords");
		/// cmd.AddTableParam("@Records", records);
		///
		/// // 3) T-SQL side:
		/// // create procedure dbo.SaveRecords @Records dbo.RecordTableType readonly as ...
		/// ]]></code>
		/// </example>
		public static void AddTableParam<T>(this SqlCommand cmd, string parameterName, IEnumerable<T>? list)
		{
			var parameter = SqlParam.Table<T>(parameterName, list);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}

		/// <summary>Adds a TVP via auto-mapping (reflection-based). <paramref name="tableName"/> sets the
		/// SQL user-defined-table-type name, and <paramref name="columnNames"/> (comma- or semicolon-separated)
		/// selects which properties of <typeparamref name="T"/> become columns.</summary>
		public static void AddTableParam<T>(this SqlCommand cmd, string parameterName, IEnumerable<T>? list, string tableName, string columnNames)
		{
			var parameter = SqlParam.Table<T>(parameterName, list, tableName, columnNames);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}

		/// <summary>Adds a TVP built via the registered mapper, but overrides its <see cref="DataTable.TableName"/>
		/// with <paramref name="tableName"/> -- useful when the same C# type maps to multiple SQL TVP types.</summary>
		public static void AddTableParam<T>(this SqlCommand cmd, string parameterName, IEnumerable<T>? list, string tableName)
		{
			var parameter = SqlParam.Table<T>(parameterName, list, tableName);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}

		/// <summary>Adds a single-row TVP holding <paramref name="id"/> -- convenience for procedures that take
		/// a one-element <c>TinyIntIdTableType</c> instead of a scalar.</summary>
		public static void AddTableRowParam(this SqlCommand cmd, string parameterName, byte id)
		{
			var parameter = SqlParam.TableRow(parameterName, id);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}

		/// <inheritdoc cref="AddTableRowParam(SqlCommand, string, byte)"/>
		public static void AddTableRowParam(this SqlCommand cmd, string parameterName, short id)
		{
			var parameter = SqlParam.TableRow(parameterName, id);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}

		/// <inheritdoc cref="AddTableRowParam(SqlCommand, string, byte)"/>
		public static void AddTableRowParam(this SqlCommand cmd, string parameterName, int id)
		{
			var parameter = SqlParam.TableRow(parameterName, id);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}

		/// <inheritdoc cref="AddTableRowParam(SqlCommand, string, byte)"/>
		public static void AddTableRowParam(this SqlCommand cmd, string parameterName, long id)
		{
			var parameter = SqlParam.TableRow(parameterName, id);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}


		/// <summary>Adds a single-row TVP from <paramref name="obj"/> using the registered mapper for <typeparamref name="T"/>.</summary>
		public static void AddTableRowParam<T>(this SqlCommand cmd, string parameterName, T obj)
		{
			var parameter = SqlParam.TableRow<T>(parameterName, obj);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}

		/// <summary>Adds a single-row TVP via auto-mapping. <typeparamref name="T"/> must not implement <see cref="IEnumerable"/>.</summary>
		/// <exception cref="ArgumentException">Thrown when <typeparamref name="T"/> implements <see cref="IEnumerable"/>.</exception>
		public static void AddTableRowParam<T>(this SqlCommand cmd, string parameterName, T obj, string tableName, string columnNames)
		{
			var parameter = SqlParam.TableRow<T>(parameterName, obj, tableName, columnNames);
			if (parameter != null)
				cmd.Parameters.Add(parameter);
		}


		/// <summary>Adds an output <c>bit</c> parameter; read its value via <c>cmd.Parameters[name].Value</c> after execute.</summary>
		public static void AddBitOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(SqlParam.BitOutput(parameterName));
		}

		/// <summary>Adds an output <c>tinyint</c> parameter.</summary>
		public static void AddTinyIntOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(SqlParam.TinyIntOutput(parameterName));
		}

		/// <summary>Adds an output <c>smallint</c> parameter.</summary>
		public static void AddSmallIntOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(SqlParam.SmallIntOutput(parameterName));
		}

		/// <summary>Adds an output <c>int</c> parameter.</summary>
		public static void AddIntOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(SqlParam.IntOutput(parameterName));
		}

		/// <summary>Adds an output <c>bigint</c> parameter.</summary>
		public static void AddBigIntOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(SqlParam.BigIntOutput(parameterName));
		}

		/// <summary>Adds an output <c>decimal(<paramref name="precision"/>, <paramref name="scale"/>)</c> parameter.</summary>
		public static void AddDecimalOutputParam(this SqlCommand cmd, string parameterName, byte precision, byte scale)
		{
			cmd.Parameters.Add(SqlParam.DecimalOutput(parameterName, precision, scale));
		}

		/// <summary>Adds an output <c>uniqueidentifier</c> parameter.</summary>
		public static void AddGuidOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(SqlParam.GuidOutput(parameterName));
		}

		/// <summary>Adds an output <c>datetime2(7)</c> parameter.</summary>
		public static void AddDateTime2OutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(SqlParam.DateTime2Output(parameterName));
		}

		/// <summary>Adds an output <c>datetimeoffset(7)</c> parameter.</summary>
		public static void AddDateTimeOffsetOutputParam(this SqlCommand cmd, string parameterName)
		{
			cmd.Parameters.Add(SqlParam.DateTimeOffsetOutput(parameterName));
		}

		/// <summary>Adds an output <c>varchar(<paramref name="size"/>)</c> parameter.</summary>
		public static void AddVarcharOutputParam(this SqlCommand cmd, string parameterName, int size)
		{
			cmd.Parameters.Add(SqlParam.VarcharOutput(parameterName, size));
		}

		/// <summary>Adds an output <c>nvarchar(<paramref name="size"/>)</c> parameter.</summary>
		public static void AddNVarcharOutputParam(this SqlCommand cmd, string parameterName, int size)
		{
			cmd.Parameters.Add(SqlParam.NVarcharOutput(parameterName, size));
		}

#if NET6_0_OR_GREATER

		/// <summary>Adds an input <c>date</c> parameter from a <see cref="DateOnly"/> (NET6+).</summary>
		public static void AddDateParam(this SqlCommand cmd, string parameterName, DateOnly value)
		{
			cmd.Parameters.Add(SqlParam.Date(parameterName, value));
		}

		/// <inheritdoc cref="AddDateParam(SqlCommand, string, DateOnly)"/>
		public static void AddDateParam(this SqlCommand cmd, string parameterName, DateOnly? value)
		{
			cmd.Parameters.Add(SqlParam.Date(parameterName, value));
		}

		/// <summary>Adds an input <c>time</c> parameter from a <see cref="TimeOnly"/> (NET6+).</summary>
		public static void AddTimeParam(this SqlCommand cmd, string parameterName, TimeOnly value)
		{
			cmd.Parameters.Add(SqlParam.Time(parameterName, value));
		}

		/// <inheritdoc cref="AddTimeParam(SqlCommand, string, TimeOnly)"/>
		public static void AddTimeParam(this SqlCommand cmd, string parameterName, TimeOnly? value)
		{
			cmd.Parameters.Add(SqlParam.Time(parameterName, value));
		}

#endif


#if NET6_0_OR_GREATER

		/// <summary>
		/// Serializes <paramref name="value"/> to JSON (via <see cref="System.Text.Json.JsonSerializer"/>)
		/// and adds it as an <c>nvarchar(max)</c> parameter.
		/// Passes <c>NULL</c> when <paramref name="value"/> is <c>null</c>.
		/// </summary>
		/// <remarks>
		/// Useful for sending complex object graphs into a stored procedure without defining a TVP for each
		/// shape. The procedure parses the JSON server-side via <c>OPENJSON</c>. For <c>IEnumerable</c>-typed
		/// payloads consider <see cref="AddTableParam{T}(SqlCommand, string, IEnumerable{T})"/> instead -- TVPs
		/// avoid the JSON parse cost and let SQL Server check column types at the boundary.
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// // C# side
		/// using var cmd = repo.CreateCommand();
		/// cmd.UseProcedure("dbo.UpsertUserPreferences");
		/// cmd.AddIntParam("@UserId", userId);
		/// cmd.AddJsonParam("@Preferences", new { Theme = "dark", FontSize = 14, Locale = "en-US" });
		/// cmd.ExecuteNonQuery();
		///
		/// // T-SQL side
		/// // create procedure dbo.UpsertUserPreferences
		/// //     @UserId      int,
		/// //     @Preferences nvarchar(max)
		/// // as begin
		/// //     declare @Theme nvarchar(50), @FontSize int, @Locale nvarchar(10);
		/// //     select @Theme = Theme, @FontSize = FontSize, @Locale = Locale
		/// //     from openjson(@Preferences)
		/// //     with (Theme nvarchar(50), FontSize int, Locale nvarchar(10));
		/// //     -- ...upsert...
		/// // end;
		/// ]]></code>
		/// </example>
		public static void AddJsonParam<T>(this SqlCommand cmd, string parameterName, T? value)
		{
			cmd.Parameters.Add(SqlParam.Json<T>(parameterName, value));
		}

#endif


		/// <summary>Returns the existing integer return-value parameter, or adds <c>@ReturnValue</c> when absent.
		/// Used internally by <c>Execute*</c> methods to expose the TSQL <c>RETURN</c> value.</summary>
		/// <exception cref="InvalidOperationException">Return parameters are ambiguous, have an incompatible SQL type,
		/// or another parameter uses the reserved <c>ReturnValue</c> name.</exception>
		public static SqlParameter ReturnValueParam(this SqlCommand cmd)
		{
			var parameter = cmd.GetReturnValueParam();
			if (parameter == null)
			{
				parameter = SqlParam.ReturnValue();
				cmd.Parameters.Add(parameter);
			}
			return parameter;
		}

		/// <summary>Ensures one integer return-value parameter exists, reusing one supplied by the caller.
		/// When absent, adds <c>@ReturnValue</c>. After execute, its <c>Value</c> holds the value supplied by
		/// TSQL <c>RETURN N;</c>, or <c>0</c> if no <c>RETURN</c> was hit.</summary>
		public static void AddReturnValueParam(this SqlCommand cmd)
		{
			cmd.ReturnValueParam();
		}

		/// <summary>Returns the sole integer return-value parameter, regardless of its name, or <c>null</c> when absent.
		/// Rejects ambiguous return parameters, incompatible SQL types, and reserved-name collisions.</summary>
		public static SqlParameter? GetReturnValueParam(this SqlCommand cmd)
		{
			SqlParameter? returnParameter = null;
			foreach (SqlParameter parameter in cmd.Parameters)
			{
				var name = parameter.ParameterName.StartsWith("@", StringComparison.Ordinal)
					? parameter.ParameterName.Substring(1) : parameter.ParameterName;
				if (parameter.Direction != ParameterDirection.ReturnValue)
				{
					if (string.Equals(name, "ReturnValue", StringComparison.OrdinalIgnoreCase))
						throw new InvalidOperationException("The parameter name 'ReturnValue' is reserved for an integer parameter with Direction.ReturnValue.");
					continue;
				}

				if (parameter.SqlDbType != SqlDbType.Int)
					throw new InvalidOperationException($"Return-value parameter '{parameter.ParameterName}' must have SqlDbType.Int.");
				if (returnParameter != null)
					throw new InvalidOperationException("A command must not contain multiple return-value parameters.");
				returnParameter = parameter;
			}
			return returnParameter;
		}

		/// <summary>Auto-discovers the procedure's parameter set via <see cref="SqlCommandBuilder.DeriveParameters(SqlCommand)"/>
		/// (cached for schema-qualified procedures), then fills each parameter from <paramref name="paramDictionary"/> by case-insensitive
		/// name match. Property keys may include or omit the leading <c>@</c>.</summary>
		/// <remarks>Useful when you don't want to hand-write a sequence of <c>Add*Param</c> calls -- pass an anonymous-typed
		/// dictionary or one built from a model. Metadata is shared only for the same server, database, procedure,
		/// connection settings and SQL principal. Integrated and token-based authentication reuse metadata only
		/// on the same connection instance. Default-schema-dependent names and unknown databases are derived on each call.
		/// Passwords and tokens are not cached.</remarks>
		public static void AddParams(this SqlCommand cmd, Dictionary<string, object> paramDictionary)
		{
			var sqlParameters = MappingManager.GetProcedureParameters(cmd);

			if (sqlParameters == null)
			{
				var isConnectionClosed = true;

				try
				{
					isConnectionClosed = cmd.Connection.State == ConnectionState.Closed;

					if (isConnectionClosed)
						cmd.Connection.Open();

					SqlCommandBuilder.DeriveParameters(cmd);
				}
				finally
				{
					if (isConnectionClosed)
						cmd.Connection.Close();
				}

				cmd.Parameters.Remove(cmd.Parameters["@RETURN_VALUE"]);

				sqlParameters = new SqlParameter[cmd.Parameters.Count];
				cmd.Parameters.CopyTo(sqlParameters, 0);

				MappingManager.CacheProcedureParameters(cmd, sqlParameters);
			}
			else
			{
				cmd.Parameters.AddRange(sqlParameters);
			}

			foreach (SqlParameter sqlParameter in cmd.Parameters)
			{
				var key = paramDictionary.Keys.FirstOrDefault(k =>
					string.Equals(k.StartsWith("@", StringComparison.Ordinal) ? k.Substring(1) : k,
						sqlParameter.ParameterName.StartsWith("@", StringComparison.Ordinal) ? sqlParameter.ParameterName.Substring(1) : sqlParameter.ParameterName,
						StringComparison.OrdinalIgnoreCase)
				);

				if (key != null)
					sqlParameter.Value = paramDictionary[key] ?? DBNull.Value;
			}
		}

		internal static bool IsSqlText(string sql)
		{
			return (Regex.IsMatch(sql, @"\bselect\b|\binsert\b|\bupdate\b|\bdelete\b|\bmerge\b", RegexOptions.IgnoreCase));
		}

		internal static void ConfigureCommand(this SqlCommand cmd, string sql, params SqlParameter?[] sqlParameters)
		{
			if(IsSqlText(sql))
				cmd.UseSql(sql);
			else
				cmd.UseProcedure(sql);

			foreach (var param in sqlParameters)
				if (param != null)
					cmd.Parameters.Add(param);
		}

		internal static void ConfigureCommand(this SqlCommand cmd, string sql, Action<SqlCommand> action)
		{
			if(IsSqlText(sql))
				cmd.UseSql(sql);
			else
				cmd.UseProcedure(sql);

			action(cmd);
		}

	}

}
