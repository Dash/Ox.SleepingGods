using System.Linq.Expressions;
using Microsoft.JSInterop;
using Ox.SleepingGods.Models.Data;
using TG.Blazor.IndexedDB;

namespace Ox.SleepingGods.Data.Schema
{
	/// <summary>
	/// Methods required for configuring <see cref="TG.Blazor.IndexedDB.IndexedDBManager"/>
	/// </summary>
	public static class IndexedDbSchema
	{
		public const int DB_VERSION = 6;
		private const string PK_NAME = "id";
		private const string DB_NAME = "Ox.SleepingGods";

		public static void ConfigureIndexedDbStore(DbStore store)
		{
			store.DbName = DB_NAME;
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
						Indexes = [
							IndexFor<Keyword>(k => k.Index)
						],
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
		}

		/// <summary>
		/// Configures the browser indexed db with <see cref="IndexedDBManager"/>
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		public static IServiceCollection ConfigureWithIndexedDb(this IServiceCollection services)
		{
			return services.AddIndexedDB(ConfigureIndexedDbStore);
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

		public static async Task UpgradeSchema(IServiceProvider sp)
		{
			var js = sp.GetRequiredService<IJSRuntime>();

			DbStore store = new();
			ConfigureIndexedDbStore(store);
			IndexedDBManager dbManager = new IndexedDBManager(store, js);
			var mgmt = await dbManager.GetRecordById<int, Management>(nameof(Management), 0);

			async Task DoUpgrade()
			{
				// Without registering all the infrastructure, save out all tables
				var keywords = await dbManager.GetRecords<Keyword>(nameof(Keyword));
				var locations = await dbManager.GetRecords<Location>(nameof(Location));
				var keywordMap = await dbManager.GetRecords<KeywordLocation>(nameof(KeywordLocation));

				// Blow away the db
				await dbManager.DeleteDb(DB_NAME);

				// Create a new instance
				dbManager = new IndexedDBManager(store, js);

				async Task Import<T>(IList<T>? entities) where T : class
				{
					if(entities != null)
					{
						var storename = typeof(T).Name;
						foreach(var r in entities)
						{
							await dbManager.AddRecord<T>(new StoreRecord<T>()
							{
								Data = r,
								Storename = storename,
							});
						}
					}
				}

				// Import all the previous records
				await Import(keywords);
				await Import(locations);
				await Import(keywordMap);
				var newMgmt = new Management(dbManager)
				{
					Uid = mgmt.Uid,
					SyncUrl = mgmt.SyncUrl,
					Etag = mgmt.Etag,
					LastUpdate = mgmt.LastUpdate,
				};
				await newMgmt.SaveAsync();
			}


			if(mgmt?.DbVersion < DB_VERSION)
			{
				switch(mgmt.DbVersion)
				{
					case 1:
					case 2:
					case 3:
					case 4:
					case 5:
						// Upgrade Keyword table
						await DoUpgrade();
						break;
				}
			}

		}
	}
}
