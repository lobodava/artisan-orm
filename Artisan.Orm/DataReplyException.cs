using System;
using System.Linq;
using System.Text;

namespace Artisan.Orm
{ 

	public class DataReplyException: Exception
	{
		public DataReplyStatus Status { get; } = DataReplyStatus.Error;

		public DataReplyMessage[] Messages { get; set; }
	
		public DataReplyException() {}

		public DataReplyException(DataReplyStatus status)
		{
			Status = status;
		}

		public DataReplyException(DataReplyStatus status, string code, long? id = null)
		{
			Status = status;
			Messages = new [] { new DataReplyMessage {Code = code, Id = id} };
		}

		public DataReplyException(DataReplyStatus status, string code, long? id, object value)
		{
			Status = status;
			Messages = new [] { new DataReplyMessage {Code = code, Id = id, Value = value} };
		}

		public DataReplyException(DataReplyStatus status, string code, string text, long? id = null)
		{
			Status = status;
			Messages = new [] { new DataReplyMessage {Code = code, Text = text, Id = id} };
		}

		public DataReplyException(DataReplyStatus status, string code, string text, object value)
		{
			Status = status;
			Messages = new [] { new DataReplyMessage {Code = code, Text = text, Value = value} };
		}

		public DataReplyException(DataReplyStatus status, string code, string text, long? id, object value)
		{
			Status = status;
			Messages = new [] { new DataReplyMessage {Code = code, Text = text, Id = id, Value = value} };
		}
	
		public DataReplyException(DataReplyStatus status, DataReplyMessage replyMessage)
		{
			Status = status;
			Messages = new [] { replyMessage };
		}

		public DataReplyException(DataReplyStatus status, DataReplyMessage[] messages)
		{
			Status = status;
			Messages = messages;
		}

		public override string Message
		{
			get
			{
				var sb = new StringBuilder();

				foreach (var message in Messages)
				{
					if (sb.Length > 0)
						sb.Append(", ");

					sb.Append('{').Append($"Code: {message.Code}");

					if (!String.IsNullOrWhiteSpace(message.Text))
						sb.Append($", Text: {message.Text}");

					if (message.Id.HasValue)
						sb.Append($", Id: {message.Id}");

					if (message.Value != null)
						sb.Append($", Value: {message.Value}");

					sb.Append('}');
				}

				return sb.ToString();
			}
		}

		public DataReplyMessage GetDataReplyMessage(string code)
		{
			return Messages?.FirstOrDefault(m => m.Code == code);
		}

	}

}
