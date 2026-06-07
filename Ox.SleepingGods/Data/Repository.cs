using Ox.SleepingGods.Models.Data;
using TG.Blazor.IndexedDB;

namespace Ox.SleepingGods.Data
{
	/// <summary>
	/// This class provides a generic repository access to database tables for simplifid access.
	/// </summary>
	/// <typeparam name="T">The .NET model class that records will be loaded into</typeparam>
	/// <typeparam name="TKey">The type of the "Id" key (int or string)</typeparam>
	public class Repository<T, TKey>
		where T : Record<TKey>
		where TKey : IComparable
	{
		protected readonly IndexedDBManager manager;
		protected Management DbInfo { get; private init; }

		/// <summary>
		/// Used for signalling that data changes have been made
		/// </summary>
		public SyncChannel Sync { get; }

		protected readonly string storeName;

		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="dBManager">Indexed DB Adapter</param>
		/// <param name="managementInfo">Shared database metadata record</param>
		/// <param name="sync">Channel for supporting data sync notifications</param>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		public Repository(IndexedDBManager dBManager, Management managementInfo, SyncChannel sync)
		{
			this.manager = dBManager ?? throw new InvalidOperationException("Undefined store");
			this.storeName = typeof(T).Name;
			//dBManager.ActionCompleted += (s, e) =>
			//{
			//	Console.WriteLine($"{e.Outcome}: {e.Message} {e}");
			//};
			this.DbInfo = managementInfo ?? throw new ArgumentNullException(nameof(managementInfo));
			this.Sync = sync;
		}

		/// <summary>
		/// Adds a new record to the data store
		/// </summary>
		/// <param name="item">Object to save</param>
		/// <param name="import">Setting to true will skip any sync and metadata update logic</param>
		/// <exception cref="DuplicateRecordException"></exception>
		public virtual async Task Add(T item, bool import = false)
		{
			if (!import)
				item.Updated = DateTimeOffset.UtcNow;

			if (item.Id != null && (await this.manager.GetRecordById<TKey, T>(this.storeName, item.Id) != null))
				throw new DuplicateRecordException($"Record with {item.Id} already exists.");

			await this.manager.AddRecord<T>(new StoreRecord<T>() { Data = item, Storename = this.storeName });

			if (!import)
			{
				await this.DbInfo.TouchAsync();
				await this.Sync.TouchAsync();
			}
		}

		/// <summary>
		/// Returns a single record by Id
		/// </summary>
		/// <param name="key">Database primary key</param>
		/// <returns>Record or null if not found</returns>
		public virtual async Task<T?> GetById(TKey key) => await this.manager.GetRecordById<TKey, T?>(this.storeName, key);

		/// <summary>
		/// Returns all records for the repository
		/// </summary>
		/// <remarks>
		/// This can be overridden, unlike <see cref="ExportAsync"/>, and so hard-coded or cached data
		/// can be merged into the results to improve performance.
		/// </remarks>
		/// <returns>List of records</returns>
		public virtual async Task<List<T>> GetAll() => await this.ExportAsync() ?? [];

		/// <summary>
		/// Checks to see if a record exists
		/// </summary>
		/// <param name="key">Database primary key</param>
		/// <returns>True or false</returns>
		public virtual async Task<bool> ExistsAsync(TKey key)
		{
			var rec = await this.manager.GetRecordById<TKey, T?>(this.storeName, key);
			return rec != null;
		}

		/// <summary>
		/// Returns all records from the database table
		/// </summary>
		public Task<List<T>> ExportAsync() => this.manager.GetRecords<T>(this.storeName);

		/// <summary>
		/// Delete all records from the database table
		/// </summary>
		public Task EraseAsync() => this.manager.ClearStore(this.storeName);

		/// <summary>
		/// Wipes the database table and imports all supplied records
		/// </summary>
		/// <param name="records">Records to import</param>
		public async Task ImportAsync(List<T> records)
		{
			await this.EraseAsync();
			foreach (var record in records)
			{
				await this.Add(record, import: true);
			}
		}

		/// <summary>
		/// Updates an existing record with new data
		/// </summary>
		/// <param name="record">Updated record</param>
		public async virtual Task Update(T record)
		{
			record.Updated = DateTimeOffset.UtcNow;
			await this.DbInfo.TouchAsync();
			await this.manager.UpdateRecord(new StoreRecord<T>() { Data = record, Storename = this.storeName });
			await this.Sync.TouchAsync();
		}

		/// <summary>
		/// Delets an individual record from the table
		/// </summary>
		/// <param name="key">Record primary key</param>
		public virtual async ValueTask Delete(TKey key)
		{
			await this.manager.DeleteRecord(this.storeName, key);
			await this.DbInfo.TouchAsync();
			await this.Sync.TouchAsync();
		}
	}
}
