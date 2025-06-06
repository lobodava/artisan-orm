using System.Collections.Generic;

namespace Artisan.Orm
{ 

	public class ObjectRow: List<object>
	{
		public ObjectRow() : base(new List<object>())
		{
		}

		public ObjectRow(int capacity) : base(new List<object>(capacity))
		{
		}
	}


	public class ObjectRows: List<ObjectRow>
	{
		public ObjectRows() : base(new List<ObjectRow>())
		{
		}
	}
}
