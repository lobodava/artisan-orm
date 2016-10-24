using System;

namespace Artisan.Orm
{

	public enum RequiredMethod
	{

		/// <summary>
		/// Check if all four methods exists: CreateEntity, CreateEntityRow, CreateDataTable and CreateDataRow.
		/// </summary>
		All,

		/// <summary>
		/// Check if all three main methods exists: CreateEntity, CreateEntityRow and CreateDataTable.
		/// </summary>
		AllMain,

		/// <summary>
		/// Check if both CreateEntity and CreateEntityRow methods exist.
		/// </summary>
		BothForEntity,

		/// <summary>
		/// Check if CreateDataTable and CreateDataRow methods exist.
		/// </summary>
		BothForDataTable,

		/// <summary>
		/// Check if CreateEntity method exists.
		/// </summary>
		CreateEntity,

		/// <summary>
		/// Check if CreateEntityRow method exists.
		/// </summary>
		CreateEntityRow,

		//CreateDataTable,
		//CreateDataRow,
	}

	
	[AttributeUsage(AttributeTargets.Class)] // , AllowMultiple = true
	public class MapperForAttribute: Attribute
	{
		public Type MapperForType { get; }

		public RequiredMethod[] RequiredMethods { get; }


		public MapperForAttribute(Type mapperForType) {
			MapperForType = mapperForType;
			RequiredMethods = new RequiredMethod[]{ };
		}

		public MapperForAttribute(Type mapperForType, params RequiredMethod[] requiredMethods) {
			MapperForType = mapperForType;
			RequiredMethods = requiredMethods;
		}

	}
}
