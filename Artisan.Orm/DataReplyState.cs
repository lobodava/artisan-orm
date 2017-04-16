using System.Runtime.Serialization;

namespace Artisan.Orm
{
	public enum DataReplyStatus
	{
		Ok			,
		Fail		,
		Missing		,
		Validation	,
		Concurrency	,
		Denial		,
		Error
	}

}
