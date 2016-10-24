using System;

namespace Artisan.Orm
{
	public class DataNotFoundException: Exception
	{
		public DataNotFoundException() {}

		public DataNotFoundException(string message): base(message) {}
	}

	public class DataConcurrencyException: Exception
	{
		public DataConcurrencyException() {}

		public DataConcurrencyException(string message): base(message) {}
	}

	public class DataWarningException: Exception
	{
		public DataMessage[] DataMessages { get; set; }

		public DataWarningException(string code)
		{
			DataMessages = new [] { new DataMessage {Code = code} };
		}

		public DataWarningException(string code, string text)
		{
			DataMessages = new [] { new DataMessage {Code = code, Text = text} };
		}

		public DataWarningException(string code, string text, string field, int? sourceId)
		{
			DataMessages = new [] { new DataMessage {Code = code, Text = text, Field = field, SourceId = sourceId} };
		}

		public DataWarningException(DataMessage dataMessage)
		{
			DataMessages = new [] { dataMessage };
		}

		public DataWarningException(DataMessage[] dataMessages)
		{
			DataMessages = dataMessages;
		}
	}

	public class ConnectionStringNotFoundException: Exception
	{
		public ConnectionStringNotFoundException(string message): base(message) {}
	}
}
