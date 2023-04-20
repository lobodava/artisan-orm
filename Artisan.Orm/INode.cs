namespace Artisan.Orm;

public interface INode<T> where T: class
{
	int Id { get; set; }

	int? ParentId { get; set; }

	IList<T> Children { get; set; }
}
