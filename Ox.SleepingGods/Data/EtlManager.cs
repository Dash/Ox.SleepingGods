using System.Text.Json;
using BrowserAPI;
using Ox.SleepingGods.Models.Data;

namespace Ox.SleepingGods.Data
{
	/// <summary>
	/// Manages loading and eporting the local database
	/// </summary>
	/// <remarks>
	/// This class handles imports and exports of the local IndexedDb, which is done by serialising all data into a
	/// JSON file.  The management record tracks general common information to support this.
	/// This class will run the background process to provide synchronisation with a remote server, if one has been
	/// specified.  Minimal work is performed on validating the synchronised state - simply the latest version wins
	/// out.  This process is vulnerable to race conditions, so it is not advisible to have two people updating the
	/// same database at the same time.
	/// </remarks>
	public sealed class EtlManager
	{
		private const int SYNC_INTERVAL_SECS = 300;
		private static readonly JsonSerializerOptions serialiserOptions = new(JsonSerializerDefaults.Web);
		private LocationRepository Locations { get; }
		private KeywordRepository Keywords { get; }
		private KeywordLocationRepository KeywordMap { get; }
		private Management DBInfo { get; }
		private static DateTimeOffset? _lastSync;

		/// <summary>
		/// Last synchronised UTC
		/// </summary>
		public DateTimeOffset? LastSync
		{
			get => _lastSync;
			private set => _lastSync = value;
		}

		public bool SyncEnabled => !String.IsNullOrWhiteSpace(this.DBInfo.SyncUrl);

		private readonly ILogger<EtlManager> log;
		private readonly HttpClient http;
		private readonly MessageManager messages;
		private readonly INetworkInformation netInfo;
		private readonly IWindow appWindow;
		private readonly SemaphoreSlim syncLock = new(1, 1);
		private bool syncSuccessful = false;
		private int syncCount = 0;

		/// <param name="locations">Location repository</param>
		/// <param name="keywords">Keyword repository</param>
		/// <param name="keywordMap">Keyword to Location repository</param>
		/// <param name="dbInfo">Management info</param>
		/// <param name="http">HTTP client instance</param>
		public EtlManager(
			ILogger<EtlManager> log,
			LocationRepository locations,
			KeywordRepository keywords,
			KeywordLocationRepository keywordMap,
			Management dbInfo,
			HttpClient http,
			MessageManager messages,
			BrowserAPI.INetworkInformation netInfo,
			BrowserAPI.IWindow appWindow)
		{
			this.Locations = locations;
			this.Keywords = keywords;
			this.KeywordMap = keywordMap;
			this.DBInfo = dbInfo;
			this.log = log;
			this.http = http;
			this.messages = messages;
			this.netInfo = netInfo;
			this.appWindow = appWindow;
			this.netInfo.OnOnline += this.NetInfo_OnOnline;
			this.appWindow.OnFocus += this.AppWindow_OnFocus;
		}

		private void AppWindow_OnFocus()
		{
			// Focus has returned after n minutes
			// Especially useful in a PWA situation where the device might sleep instead of stop the app
			if(this.LastSync.HasValue && (DateTime.UtcNow - this.LastSync.Value).TotalMinutes > 15)
			{
				_ = Task.Run(async () => await this.SyncAsync());
			}
		}

		/// <summary>
		/// Fires when connectivity is restored to the browser
		/// </summary>
		private void NetInfo_OnOnline()
		{
			_ = Task.Run(async () => await this.SyncAsync());
		}

		public static string? SyncStatus { get; private set; }

		/// <summary>
		/// Export the local indexed db to a JSON file
		/// </summary>
		/// <returns></returns>
		public async Task<Stream> ExportAllAsync()
		{
			var locationTask = this.Locations.ExportAsync();
			var keywordTask = this.Keywords.ExportAsync();
			var mapTask = this.KeywordMap.ExportAsync();

			MemoryStream ms = new();
			DatabaseExport data = new()
			{
				Version = this.DBInfo.DbVersion,
				Locations = await locationTask,
				Keywords = await keywordTask,
				KeywordLocations = await mapTask,
				Management = this.DBInfo
			};

			await JsonSerializer.SerializeAsync(ms, data, serialiserOptions);

			await ms.FlushAsync();
			ms.Position = 0;

			return ms;
		}

		/// <summary>
		/// Wipe all indexed dbs and reset the db identifier
		/// </summary>
		/// <returns></returns>
		public async Task DeleteAllAsync()
		{
			await this.KeywordMap.EraseAsync();
			await this.Locations.EraseAsync();
			await this.Keywords.EraseAsync();
			await this.DBInfo.EraseAsync();
		}

		/// <summary>
		/// Completely overwrites local database with the imported file
		/// </summary>
		/// <param name="upload">JSON stream that comes from a previous <see cref="ExportAllAsync"/> output</param>
		public async Task ImportAsync(Stream upload)
		{
			var data = await JsonSerializer.DeserializeAsync<DatabaseExport>(upload, serialiserOptions);

			if (data != null)
			{
				if (data.Management != null)
				{
					this.DBInfo.Uid = data.Management.Uid;
					this.DBInfo.LastUpdate = data.Management.LastUpdate;
					this.DBInfo.Etag = data.Management.Etag;
				}

				List<Task> imports = new List<Task>(3);

				if (data.Keywords != null)
					imports.Add(this.Keywords.ImportAsync(data.Keywords));

				if (data.Locations != null)
					imports.Add(this.Locations.ImportAsync(data.Locations));

				if (data.KeywordLocations != null)
					imports.Add(this.KeywordMap.ImportAsync(data.KeywordLocations));

				await Task.WhenAll(imports);
			}
		}

		/// <summary>
		/// Pushes the current local indexed db to a remote endpoint
		/// </summary>
		/// <remaks>
		/// The remote endpoint must accept a PUT and must return an ETag based on the unix timestamp of the
		/// uploaded file.  It must (obviously) be CORS compliant.
		/// </remaks>
		private async Task PushLatestAsync()
		{
			//await this.DBInfo.Loading;
			if (this.DBInfo.SyncUrl != null)
			{
				var msg = new StreamContent(await this.ExportAllAsync());
				msg.Headers.ContentType = new("application/json");

				this.log.LogInformation("Pushing record to sync server.");

				using var response = await this.http.PutAsync(new Uri(new Uri(this.DBInfo.SyncUrl), this.DBInfo.Uid), msg);

				this.log.LogDebug("Sync status {Status}", response.StatusCode);

				if (!response.IsSuccessStatusCode)
					throw new Exception(await response.Content.ReadAsStringAsync());

				this.log.LogTrace("Sync etag set to {Etag}", response.Headers.ETag?.Tag);

				this.LastSync = DateTimeOffset.UtcNow;
				this.DBInfo.Etag = response.Headers.ETag?.Tag;
				EtlManager.SyncStatus = "Pushed latest changes.";
			}
		}

		/// <summary>
		/// Pulls a server version and if newer, overwrites the local indexed db
		/// </summary>
		/// <remarks>
		/// This relies on the ETag being a Unix timestamp indicating when the database was last updated.  Newer
		/// timestamps than the one recorded locally will result in a complete replacement of the local db with the
		/// server version.
		/// </remarks>
		public async Task SyncAsync()
		{
			await this.DBInfo.Loading;

			EtlManager.SyncStatus = null;

			if (this.DBInfo.SyncUrl != null)
			{
				if (!await this.netInfo.OnLine)
				{
					this.log.LogWarning("Browser offline, unable to sync.");
					EtlManager.SyncStatus = "Browser is offline (or saving data), not syncing.";
					return;
				}

				if (this.syncLock.CurrentCount == 0)
				{
					this.log.LogInformation("Sync already underway, skipping duplicate.");
					return;
				}

				do
				{
					try
					{
						await this.syncLock.WaitAsync();

						using HttpRequestMessage msg = new(HttpMethod.Get, new Uri(new Uri(this.DBInfo.SyncUrl), this.DBInfo.Uid));
						msg.Headers.Accept.Add(new("application/json"));

						// The browser will cache etags automatically with the cache
						// So we need to bust the cache by setting a fake value in the request if one isn't known to avoid
						// a previous etag being implicitly set
						if (this.DBInfo.Etag != null)
							msg.Headers.IfNoneMatch.Add(new(this.DBInfo.Etag));
						else
							msg.Headers.IfNoneMatch.Add(new("\"fudge\""));

						using CancellationTokenSource cts = new();

						cts.CancelAfter(10000);

						using var response = await this.http.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, cts.Token);

						switch (response.StatusCode)
						{
							case System.Net.HttpStatusCode.NotModified:
								// Nothing to do, already in-sync
								this.log.LogTrace("Server in-sync, no action taken.");
								EtlManager.SyncStatus = "Synchronised.";
								break;
							case System.Net.HttpStatusCode.NotFound:
								// Server copy missing, push it
								await this.PushLatestAsync();
								break;
							case System.Net.HttpStatusCode.OK:
								// Data!
								if (!String.IsNullOrEmpty(response.Headers.ETag?.Tag) && Int64.TryParse(response.Headers.ETag?.Tag.Substring(1, response.Headers.ETag.Tag.Length - 2), out var serverTimestamp))
								{
									// ETag is a valid number
									// Check that the server version is ahead of the client version
									if (serverTimestamp == this.DBInfo.LastUpdate.ToUnixTimeSeconds())
									{
										// Whhhaat?
										// Should have got a 304.
									}
									else if (serverTimestamp < this.DBInfo.LastUpdate.ToUnixTimeSeconds())
									{
										this.log.LogWarning("Server version is out-of-date");
										// Trigger export to server
										await this.PushLatestAsync();
									}
									else
									{
										// Import lastest to client db
										await this.ImportAsync(response.Content.ReadAsStream());
										// Overwrite the file's etag and update time to match the http request
										// to avoid confusion with the sync proces
										this.DBInfo.Etag = response.Headers.ETag?.Tag;
										this.DBInfo.LastUpdate = DateTimeOffset.FromUnixTimeSeconds(serverTimestamp);
										await this.DBInfo.SaveAsync();
										this.log.LogInformation("Imported latest from sync server.");
										EtlManager.SyncStatus = "Pulled latest changes.";
										this.LastSync = DateTimeOffset.UtcNow;
									}
								}

								break;
							default:
								EtlManager.SyncStatus = $"Unexpected response from sync server: {response.StatusCode}";
								this.log.LogError("Unexpectted response {StatusCode}", response.StatusCode);
								break;
						}

						syncSuccessful = true;
						syncCount = 0;
						await this.messages.ClearSyncError();
					}
					catch (Exception ex)
					{
						this.log.LogError(exception: ex, message: ex.Message);
						EtlManager.SyncStatus = $"Sync pull failed: {ex.Message}";

						if(++syncCount > 2)
							await this.messages.DisplaySyncError($"Unable to sync ({ex.Message})");

						// Throttle hammering
						await Task.Delay(30000);
					}
					finally
					{
						this.syncLock.Release();
					}
				} while (!syncSuccessful);
			}
		}
	}

	public class DatabaseExport
	{
		public int? Version { get; set; }
		public Management? Management { get; set; }
		public List<Location>? Locations { get; set; }
		public List<Keyword>? Keywords { get; set; }
		public List<KeywordLocation>? KeywordLocations { get; set; }
	}
}
