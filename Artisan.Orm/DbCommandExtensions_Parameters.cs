using System;
using System.Data;
using System.Data.SqlClient;

namespace Artisan.Orm
{
	public static partial class DbCommandExtensions
	{

		public static void UseProcedure(this SqlCommand cmd, string procedureName)
		{
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.CommandText = procedureName;
		}

		public static void UseSql(this SqlCommand cmd, string sql)
		{
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = sql;
		}


        public static void AddVarcharParam(this SqlCommand cmd, string parameterName, int size, string value)
        {
            NormalizateStringParam(ref value, size);

			cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = parameterName,
                Direction = ParameterDirection.Input,
                SqlDbType = SqlDbType.VarChar,
                Size = size,
                Value = (object)value ?? DBNull.Value,
            });
        }
		
		public static void AddNVarcharParam(this SqlCommand cmd, string parameterName, int size, string value )
		{
			NormalizateStringParam(ref value, size);
			
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.NVarChar, 
				Size = size,
				Value = (object)value ?? DBNull.Value,
			});
		}

		private static void NormalizateStringParam(ref string value,  int size)
		{
			if (value == null) return;

			if (value.Length > size)
				value = value.Substring(0, value.Length);
			else if (value.Length == 0)
				value = null;
		}

        public static void AddVarcharMaxParam(this SqlCommand cmd, string parameterName, string value)
        {
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = parameterName,
                Direction = ParameterDirection.Input,
                SqlDbType = SqlDbType.VarChar,
                Size = -1,
                Value = (object)value ?? DBNull.Value,
            });
        }

		public static void AddNVarcharMaxParam(this SqlCommand cmd, string parameterName, string value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.NVarChar, 
				Size = -1,
				Value = (object)value ?? DBNull.Value,
			});
		}

		public static void AddRowVersionFromBase64StringParam(this SqlCommand cmd, string parameterName, string value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarBinary, 
				Size = -1,
				Value = (value == null) ? DBNull.Value : (object)Convert.FromBase64String(value)
			});
		}

		public static void AddBigIntParam(this SqlCommand cmd, string parameterName, long value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.BigInt, 
				Value = value
			});
		}

		public static void AddIntParam(this SqlCommand cmd, string parameterName, int value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Int, 
				Value = value
			});
		}

		public static void AddIntParam(this SqlCommand cmd, string parameterName, int? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Int, 
				Value = value == null ? DBNull.Value : (object)value.Value
			});
		}

		public static void AddSmallIntParam(this SqlCommand cmd, string parameterName, int value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.SmallInt, 
				Value = value
			});
		}

		public static void AddSmallIntParam(this SqlCommand cmd, string parameterName,  int? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.SmallInt, 
				Value = value == null ? DBNull.Value : (object)value.Value
			});
		}


		public static void AddTinyIntParam(this SqlCommand cmd, string parameterName, int value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.TinyInt, 
				Value = value
			});
		}

		public static void AddTinyIntParam(this SqlCommand cmd, string parameterName, int? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.TinyInt, 
				Value = value == null ? DBNull.Value : (object)value.Value
			});
		}

		public static void AddDecimalParam(this SqlCommand cmd, string parameterName, byte precision,  byte scale,  decimal value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Decimal,
				Scale = scale,
 				Precision = precision,
				Value = value
			});
		}

		public static void AddDecimalParam(this SqlCommand cmd, string parameterName, byte precision, byte scale, decimal? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Decimal,
				Scale = scale,
 				Precision = precision,
				Value = value == null ? DBNull.Value : (object)value.Value
			});
		}
		
		public static void AddVarbinaryParam(this SqlCommand cmd, string parameterName, int size, byte[] value  )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.VarBinary, 
				Value = value
			});
		}

		public static void AddBitParam(this SqlCommand cmd, string parameterName, bool value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Bit, 
				Value = value ? 1 : 0
			});
		}

		public static void AddBitParam(this SqlCommand cmd, string parameterName, bool? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Bit, 
				Value = value == null ? DBNull.Value : (object)(value == true ? 1 : 0)
			});
		}

		public static void AddDateTimeParam(this SqlCommand cmd, string parameterName, DateTime value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTime, 
				Value = value
			});
		}
		
		public static void AddDateTimeParam(this SqlCommand cmd, string parameterName, DateTime? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTime, 
				Value = (object)value ?? DBNull.Value
			});
		}
		
		public static void AddDateTime2Param(this SqlCommand cmd, string parameterName, DateTime? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.DateTime2, 
				Value = (object)value ?? DBNull.Value
			});
		}

		public static void AddDateParam(this SqlCommand cmd, string parameterName, DateTime? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Date, 
				Value = (object)value ?? DBNull.Value
			});
		}
		
		public static void AddGuidParam(this SqlCommand cmd, string parameterName, Guid? value )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.UniqueIdentifier, 
				Value = (object)value ?? DBNull.Value
			});
		}

		public static void AddTableParam(this SqlCommand cmd, string parameterName, DataTable dataTable)
		{
			if (dataTable == null) return;

			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Input,
				SqlDbType = SqlDbType.Structured,
				TypeName = dataTable.TableName, 
				Value = dataTable					
			});
		}

		public static void AddVarcharOutputParam(this SqlCommand cmd, string parameterName, int size )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.VarChar, 
				Size = size,					
			});
		}

		public static void AddNVarcharOutputParam(this SqlCommand cmd, string parameterName, int size )
		{
			cmd.Parameters.Add( new SqlParameter
			{ 
				ParameterName = parameterName,
				Direction = ParameterDirection.Output,
				SqlDbType = SqlDbType.NVarChar, 
				Size = size,					
			});
		}

		public static SqlParameter ReturnValueParam(this SqlCommand cmd)
		{
			var returnValueParam = new SqlParameter
			{
				ParameterName = "ReturnValue",
				Direction = ParameterDirection.ReturnValue,
				SqlDbType = SqlDbType.Int,
			};

			cmd.Parameters.Add(returnValueParam);

			return returnValueParam;
		}
		
	}
}