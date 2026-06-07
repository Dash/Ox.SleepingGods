using System.Linq.Expressions;
using Ox.SleepingGods.Models.Data;
using TG.Blazor.IndexedDB;

namespace Ox.SleepingGods.Data.Schema
{
	/// <summary>
	/// Methods required for configuring <see cref="TG.Blazor.IndexedDB.IndexedDBManager"/>
	/// </summary>
	public static class IndexedDbSchema
	{
		public const int DB_VERSION = 5;
		private const string PK_NAME = "id";

		/// <summary>
		/// Configures the browser indexed db with <see cref="IndexedDBManager"/>
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		public static IServiceCollection ConfigureWithIndexedDb(this IServiceCollection services)
		{
			return services.AddIndexedDB(store =>
			{
				store.DbName = "Ox.SleepingGods";
				store.Version = DB_VERSION;

				store.Stores.AddRange([
					new()
					{
						Name = nameof(Management),
						PrimaryKey = new IndexSpec() { Name = PK_NAME, KeyPath = "id", Auto = false, Unique = true },
					},
					new()
					{
						Name = nameof(Keyword),
						PrimaryKey = new IndexSpec() { Name = PK_NAME, KeyPath = "id", Unique = true },
					},
					new()
					{
						Name = nameof(Location),
						PrimaryKey = new IndexSpec() { Name = PK_NAME, KeyPath = "id", Unique = true },
					},
					new()
					{
						Name = nameof(KeywordLocation),
						PrimaryKey = new IndexSpec() { Name = PK_NAME, KeyPath = nameof(KeywordLocation.Id).ToLower(), Unique = true },
						Indexes = [
							IndexFor<KeywordLocation>(k => k.Location),
							IndexFor<KeywordLocation>(k => k.Keyword),
						],
					}
				]);
			});
		}

		private static IndexSpec IndexFor<T>(Expression<Func<T, object>> property)
		{
			string propertyName;

			if (property.Body is MemberExpression member)
				propertyName = member.Member.Name;

			else if (property.Body is UnaryExpression unary &&
				unary.Operand is MemberExpression unaryMember)
				propertyName = unaryMember.Member.Name;

			else
				throw new InvalidOperationException("Invalid property expression.");

			return new IndexSpec()
			{
				Name = $"{typeof(T).Name}_{propertyName}",
				KeyPath = propertyName.ToLower(),
				Auto = false,
				Unique = false
			};
		}
	}
}
