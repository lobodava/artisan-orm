using System.Collections.Generic;

namespace Artisan.Orm
{
	public class Rows: List<object[]>
	{
		public Rows() : base(new List<object[]>())
		{
		}
	}
}
