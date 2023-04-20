using Artisan.Orm;

namespace Tests.DataServices;

public class DataServiceBase: IDisposable
{
	internal IDisposable Repository;


	public static DataReply<T> Get<T>(Func<T> func) 
	{
		try 
		{
			T data = func();
			return new DataReply<T>(data);
		}
		catch (DataReplyException ex)
		{
			return new DataReply<T>(ex.Status, ex.Messages);
		}
		catch (Exception ex) 
		{
			return new DataReply<T>(DataReplyStatus.Error, GetErrorDataReplyMessages(ex));
		}
	}

	public static async Task<DataReply<T>> GetAsync<T>(Func<Task<T>> funcAsync) 
	{
		try 
		{
			T data = await funcAsync();
			return new DataReply<T>(data);
		}
		catch(DataReplyException ex)
		{
			return new DataReply<T>(ex.Status, ex.Messages);
		}
		catch(Exception ex) 
		{
			return new DataReply<T>(DataReplyStatus.Error, GetErrorDataReplyMessages(ex));
		}
	}

	public static DataReply Execute(Func<bool> func) 
	{
		try 
		{
			return func() ? new DataReply() : new DataReply(DataReplyStatus.Fail);
		}
		catch(DataReplyException ex)
		{
			return new DataReply(ex.Status, ex.Messages);
		}
		catch(Exception ex) 
		{
			return new DataReply(DataReplyStatus.Error, GetErrorDataReplyMessages(ex));
		}
	}


	public static async Task<DataReply> ExecuteAsync(Func<Task<bool>> funcAsync) 
	{
		try 
		{
			return await funcAsync() ? new DataReply() : new DataReply(DataReplyStatus.Fail);
		}
		catch(DataReplyException ex)
		{
			return new DataReply(ex.Status, ex.Messages);
		}
		catch(Exception ex) 
		{
			return new DataReply(DataReplyStatus.Error, GetErrorDataReplyMessages(ex));
		}
	}


	private static DataReplyMessage [] GetErrorDataReplyMessages (Exception ex)
	{
		return new []
		{
			new DataReplyMessage { Code = "ErrorMessage"	, Text = ex.Message },
			new DataReplyMessage { Code = "StackTrace"	, Text = ex.StackTrace[..500] }
		};
	}
	
	public void Dispose()
	{
		Repository?.Dispose();
		Repository = null;

		GC.SuppressFinalize(this);
	}
}

