using System;

namespace Artisan.Orm
{

	public enum RequiredMethod
	{

		/// <summary>
		/// Check if all four methods exists: CreateObject, CreateObjectRow, CreateDataTable and CreateDataRow.
		/// </summary>
		All,

		/// <summary>
		/// Check if all three main methods exists: CreateObject, CreateObjectRow and CreateDataTable.
		/// </summary>
		AllMain,

		/// <summary>
		/// Check if both CreateObject and CreateObjectRow methods exist.
		/// </summary>
		BothForObject,

		/// <summary>
		/// Check if CreateDataTable and CreateDataRow methods exist.
		/// </summary>
		BothForDataTable,

		/// <summary>
		/// Check if CreateObject method exists.
		/// </summary>
		CreateObject,

		/// <summary>
		/// Check if CreateObjectRow method exists.
		/// </summary>
		CreateObjectRow,

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
