using System;
using System.Data.SqlClient;
using System.Runtime.Serialization;

namespace Artisan.Orm
{
	[DataContract]
	public class DataReplyMessage
	{
		[DataMember]
		public String Code;

		[DataMember(EmitDefaultValue = false)]
		public String Text;

		[DataMember(EmitDefaultValue = false)]
		public Int64? Id;

		[DataMember(EmitDefaultValue = false)]
		public Object Value;
	}

	[MapperFor( typeof(DataReplyMessage), RequiredMethod.CreateObject)]
	public static class DataReplyMessageMapper 
	{
		public static DataReplyMessage CreateObject(SqlDataReader dr)
		{
			var i = 0;

			return new DataReplyMessage
			{
				Code	=	dr.GetString(i++)				,
				Text	=	dr.GetStringNullable(i++)		,
				Id		=	dr.GetValueNullable<long>(i++)	,
				Value	=	dr.GetValueNullable<object>(i)
			};
		}
	}

}
