using System;
using System.Runtime.Serialization;
using static System.String;

namespace Artisan.Orm
{
	[DataContract]
	public class DataReply
	{

		[DataMember]
		public DataReplyStatus Status { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public DataReplyMessage[] Messages { get; set; }

		public DataReply()
		{
			Status = DataReplyStatus.Ok;
			Messages = null;
		}

		public DataReply(DataReplyStatus status)
		{
			Status = status;
			Messages = null;
		}

		public DataReply(DataReplyStatus status, string code, string text)
		{
			Status = status;
			Messages = new[] { new DataReplyMessage { Code = code, Text = text } };
		}

		public DataReply(DataReplyStatus status, DataReplyMessage message)
		{
			Status = status;
			if (message != null)
				Messages = new[] { message };
		}

		public DataReply(DataReplyStatus status, DataReplyMessage[] messages)
		{
			Status = status;
			if (messages?.Length > 0)
				Messages = messages;
		}


		public DataReply(string message)
		{
			Status = DataReplyStatus.Ok;
			Messages = new[] { new DataReplyMessage { Text = message } };
		}

		public static DataReplyStatus? ParseStatus(string statusCode)
		{
			if (IsNullOrWhiteSpace(statusCode))
				return null;

			DataReplyStatus status;
			if (Enum.TryParse(statusCode, true, out status))
				return status;

			throw new InvalidCastException(
				$"Cannot cast string '{statusCode}' to DataReplyStatus Enum. " +
				$"Available values: {Join(", ", Enum.GetNames(typeof(DataReplyStatus)))}");
		}
	}


	[DataContract]
	public class DataReply<TData> : DataReply
	{

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

		public DataReply(DataReplyStatus status, string code, string text, TData data) : base(status, code, text)
		{
			Data = data;
		}

		public DataReply(DataReplyStatus status, TData data) : base(status)
		{
			Data = data;
		}

		public DataReply(DataReplyStatus status) : base(status)
		{
			Data = default(TData); // Replaced 'default' literal with explicit type
		}

		public DataReply(DataReplyStatus status, string code, string text) : base(status, code, text)
		{
			Data = default(TData); // Replaced 'default' literal with explicit type
		}

		public DataReply(DataReplyStatus status, DataReplyMessage replyMessage) : base(status, replyMessage)
		{
			Data = default(TData); // Replaced 'default' literal with explicit type
		}

		public DataReply(DataReplyStatus status, DataReplyMessage[] replyMessages) : base(status, replyMessages)
		{
			Data = default(TData); // Replaced 'default' literal with explicit type
		}
	}
}
