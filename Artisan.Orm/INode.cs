using System;
using System.Collections.Generic;

namespace Artisan.Orm
{

	public interface INode<T> where T: class
	{
		Int32 Id { get; set; }

		Int32? ParentId { get; set; }

		IList<T> Children { get; set; }
	}

}
