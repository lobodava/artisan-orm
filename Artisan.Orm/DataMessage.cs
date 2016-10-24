using System.Data.SqlClient;
using System.Runtime.Serialization;
//using Newtonsoft.Json;

namespace Artisan.Orm
{
	[DataContract]
	public class DataMessage
	{
		[DataMember]
		public string Code;

		//[JsonProperty( NullValueHandling = NullValueHandling.Ignore )]
		[DataMember(EmitDefaultValue = false)]
		public string Field;

		//[JsonProperty( NullValueHandling = NullValueHandling.Ignore )]
		[DataMember(EmitDefaultValue = false)]
		public string Text;

		//[JsonProperty( NullValueHandling = NullValueHandling.Ignore )]
		[DataMember(EmitDefaultValue = false)]
		public int? SourceId;
	}

	[MapperFor( typeof(DataMessage), RequiredMethod.CreateEntity)]
	public static class DataMessageMapper 
	{
		public static DataMessage CreateEntity(SqlDataReader dr)
		{
			var index = 0;

			if (dr == null || dr.IsDBNull(index)) 
				return null;

			return new DataMessage
			{
				Code		=	dr.GetString(index++)		  ,
				Field		=	dr.GetStringNullable(index++) ,
				Text		=	dr.GetStringNullable(index++) ,
				SourceId	=	dr.GetInt32Nullable(index)
			};
		}
	}

}
