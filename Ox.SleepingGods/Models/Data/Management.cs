using System.Text.Json.Serialization;
using TG.Blazor.IndexedDB;

namespace Ox.SleepingGods.Models.Data
{
	public sealed class Management()
	{
		private readonly IndexedDBManager? dbManager;

		private StoreRecord<Management>? record;

		private bool isLoaded = false;
		private readonly TaskCompletionSource loadedTask = new();
		[JsonIgnore]
		public Task Loading => this.loadedTask.Task;

		private readonly SemaphoreSlim semaphore = new(1);
		public Management(IndexedDBManager dbManager) : this()
		{
			this.dbManager = dbManager;
		}

		public int Id => 0;

		public int DbVersion
		{
			get;
			private set;
		} = Ox.SleepingGods.Data.Schema.IndexedDbSchema.DB_VERSION;

		/// <summary>
		/// Unique indicator for this database
		/// </summary>
		public string Uid { get; set; } = Guid.NewGuid().ToString();
		/// <summary>
		/// Last update timestamp for all records
		/// </summary>
		public DateTimeOffset LastUpdate { get; set; } = DateTimeOffset.MinValue;

		/// <summary>
		/// Endpoint to attempt to synchronise the database with
		/// </summary>
		/// <remarks>
		/// This field won't export to avoid situations where an import will potentially overwrite an existing save.
		/// This can only be set explicitly by each app.
		/// </remarks>
		public string? SyncUrl
		{
			get;
			set => field = String.IsNullOrEmpty(value) ? null : value;
		}

		public string? Etag { get; set; }

		public async Task LoadAsync()
		{
			if (!this.isLoaded)
			{
				await this.semaphore.WaitAsync();
				try
				{
					var r = await this.dbManager!.GetRecordById<int, Management>(nameof(Management), 0);
					// If a record exists, load those values, otherwise start with defaults
					if (r != null)
					{
						this.DbVersion = r.DbVersion;
						this.Uid = r.Uid;
						this.LastUpdate = r.LastUpdate;
						this.SyncUrl = r.SyncUrl;
						this.Etag = r.Etag;
					}
					this.isLoaded = true;
					this.loadedTask.SetResult();
				}
				catch (Exception ex)
				{
					this.loadedTask.SetException(ex);
					throw;
				}
				finally
				{
					this.semaphore.Release();
				}
			}
		}

		/// <summary>
		/// Updates the last updated state timestamp and saves the database metadata info
		/// </summary>
		/// <returns></returns>
		public async Task TouchAsync()
		{
			this.LastUpdate = DateTimeOffset.UtcNow;
			// The etag is now invalid as the state has changed and only the server should be setting the etag.
			this.Etag = null;
			await this.SaveAsync();
		}

		/// <summary>
		/// Saves database metadata info without updating the last updated state timestamp
		/// </summary>
		/// <returns></returns>
		public async Task SaveAsync()
		{
			this.record ??= new StoreRecord<Management>()
				{
					Storename = nameof(Management),
					Data = this
				};
			await this.dbManager!.UpdateRecord(this.record);
		}

		public async Task EraseAsync()
		{
			await this.dbManager!.ClearStore(nameof(Management));
			this.SyncUrl = null;
			this.Uid = Guid.NewGuid().ToString();
			this.Etag = null;
			this.LastUpdate = DateTimeOffset.MinValue;
		}
	}
}
