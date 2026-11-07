using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using static System.String;

namespace Artisan.Orm
{ 
	[DataContract]
	public class DataReply {

		[DataMember]
		public DataReplyStatus Status { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public DataReplyMessage[]? Messages { get; set; }

		public DataReply()
		{
			Status = DataReplyStatus.Ok;
		}

		public DataReply(DataReplyStatus status)
		{
			Status = status;
		}

		public DataReply(DataReplyStatus status, string code, string text)
		{
			Status = status;
			Messages = new [] { new DataReplyMessage { Code = code, Text = text } };
		}
	
		public DataReply(DataReplyStatus status, DataReplyMessage message)
		{
			Status = status;
			if (message != null)
				Messages = new [] { message };
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
			Messages = new [] { new DataReplyMessage { Text = message } };
		}

		/// <summary>
		/// Parses one defined status name (case-insensitive) or its numeric value.
		/// Blank input returns null. Unknown values and comma-separated combinations are rejected.
		/// </summary>
		public static DataReplyStatus? ParseStatus (string? statusCode)
		{
			if (IsNullOrWhiteSpace(statusCode))
				return null;

			if (statusCode!.IndexOf(',') < 0 &&
				Enum.TryParse(statusCode, true, out DataReplyStatus status) &&
				Enum.IsDefined(typeof(DataReplyStatus), status))
				return status;

			throw new InvalidCastException(
					$"Cannot cast string '{statusCode}' to DataReplyStatus Enum. " +
					$"Available values: {Join(", ", Enum.GetNames(typeof(DataReplyStatus)))}");
		}
	}


	[DataContract]
	public class DataReply<TData>: DataReply {

		/// <summary>The payload. May be <see langword="null"/> for status-only replies.</summary>
		[DataMember(EmitDefaultValue = false)]
		[MaybeNull]
		public TData Data { get; set; }

		public DataReply(TData data)
		{
			Data = data;
		}

		public DataReply()
		{
			Data = default!;
		}

		public DataReply(DataReplyStatus status, string code, string text, TData data)  :base(status, code, text)
		{
			Data = data;
		}

		public DataReply(DataReplyStatus status, TData data) :base(status)
		{
			Data = data;
		}

		public DataReply(DataReplyStatus status) :base(status)
		{
			Data = default!;
		}

		public DataReply(DataReplyStatus status, string code, string text) :base(status, code, text)
		{
			Data = default!;
		}

		public DataReply(DataReplyStatus status, DataReplyMessage replyMessage) :base(status, replyMessage)
		{
			Data = default!;
		}

		public DataReply(DataReplyStatus status, DataReplyMessage[] replyMessages) :base(status, replyMessages)
		{
			Data = default!;
		}
	}
}

