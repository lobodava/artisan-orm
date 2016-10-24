using System;
using System.ComponentModel;
using System.Runtime.Serialization;
//using Newtonsoft.Json;

namespace Artisan.Orm
{
	[DataContract]
	public class DataReply {

		[DataMember]
		public DataStatus Status { get; set; }

		//[JsonProperty( NullValueHandling = NullValueHandling.Ignore )]
		[DataMember(EmitDefaultValue = false)]
		public DataMessage[] Messages { get; set; }
		
		public DataReply() {
			Status = DataStatus.Success;
			Messages = null;
		}

		public DataReply(DataStatus status) {
			Status = status;
			Messages = null;
		}

		public DataReply(DataStatus status, string messageText) {
			Status = status;
			Messages = new [] { new DataMessage { Text = messageText } };
		}

		public DataReply(DataStatus status, DataMessage message) {
			Status = status;
			Messages = new [] { message };
		}


		public DataReply(DataStatus status, DataMessage[] messages) {
			Status = status;
			if (messages.Length > 0)
				Messages = messages;
		}


		public DataReply(string message) {
			Status = DataStatus.Success;
			Messages = new [] { new DataMessage { Text = message } };
		}

		public static DataStatus GetReplyStatus (string replyStatusCode) {

			if (String.IsNullOrWhiteSpace(replyStatusCode))
				throw new InvalidEnumArgumentException("Cannot cast empty string to ReplyStatus Enum");
			
			 if (!Enum.IsDefined(typeof(DataStatus), replyStatusCode))
				throw new InvalidCastException($"Cannot cast string '{replyStatusCode}' to ReplyStatus Enum");

			return (DataStatus)Enum.Parse(typeof(DataStatus), replyStatusCode);
		}

	}

	[DataContract]
	public class DataReply<TData>: DataReply {

		//[JsonProperty( NullValueHandling = NullValueHandling.Ignore )]
		[DataMember(EmitDefaultValue = false)]
		public TData Data { get; set; }

		public DataReply(TData data)
		{
			Data = data;
		}

		public DataReply()
		{
			Data = default(TData);
		}

		public DataReply(DataStatus status, string message, TData data)  :base(status, message) 
		{
			Data = data;
		}

		public DataReply(DataStatus status, TData data) :base(status) 
		{
			Data = data;
		}

		public DataReply(DataStatus status) :base(status) 
		{
			Data = default(TData);
		}

		public DataReply(DataStatus status, string message) :base(status, message) 
		{
			Data = default(TData);
		}

		public DataReply(DataStatus status, DataMessage[] messages) :base(status, messages)  {
			Data = default(TData);
		}
	}
}
