namespace Ox.SleepingGods.Data
{
	/// <summary>
	/// This class continually monitors for database saves and pushes the latest to the sync endpoint - if it exists.
	/// </summary>
	public sealed class SyncMonitor : IAsyncDisposable, IDisposable
	{
		private readonly SyncChannel channel;
		private readonly EtlManager etl;
		private readonly ILogger<SyncMonitor> log;
		private readonly Task syncMonitor;
		private readonly CancellationTokenSource cancellationSource = new();

		/// <summary>
		/// Event that fires when a request for syncing has been triggered
		/// </summary>
		public static Func<Task>? OnProcessingStart;

		/// <summary>
		/// Event that fires when the syncing process has completed
		/// </summary>
		public static Func<Task>? OnProcessingEnd;

		public SyncMonitor(SyncChannel channel, EtlManager manager, ILogger<SyncMonitor> log)
		{
			this.channel = channel;
			this.etl = manager;
			this.log = log;
			this.syncMonitor = Task.Run(this.MonitorAsync);
		}

		private async Task MonitorAsync()
		{
			this.log.LogDebug("Monitoring");
			await foreach (var item in this.channel.Reader.ReadAllAsync(this.cancellationSource.Token))
			{
				if (this.etl.SyncEnabled)
				{
					this.log.LogDebug("Records to sync");
					await (SyncMonitor.OnProcessingStart?.Invoke() ?? Task.CompletedTask);

					/* The idea of waiting 10 seconds is because people tend to change multiple things in small windows
					 * and we don't want to hammer the sync endpoint.  Waiting a period of time gives the user a chance
					 * to complete all their activity before syncing.
					 * The sync logic should decide whether any sync is necessary/
					 */
					try
					{
						await Task.Delay(10000, this.cancellationSource.Token);
					}
					catch (TaskCanceledException)
					{ } // Push this waiting instance out asap

					// If after waiting there is other stuff in the queue
					// we'll just go round again as we want to avoid syncing while more
					// changes are coming through
					// Obviously, only if we haven't requested cancellation
					if (!this.cancellationSource.Token.IsCancellationRequested
						&& this.channel.Reader.Count > 0)
						continue;

					// Export 
					await this.etl.SyncAsync();

					await (SyncMonitor.OnProcessingEnd?.Invoke() ?? Task.CompletedTask);
				}
			}
		}

		public void Dispose()
		{
			this.log.LogInformation("SyncMonitor shutdown.");
			this.cancellationSource.Cancel();
			GC.SuppressFinalize(this);
		}

		public async ValueTask DisposeAsync()
		{
			this.log.LogInformation("SyncMonitor shutdown.");
			if (!this.cancellationSource.IsCancellationRequested)
				await this.cancellationSource.CancelAsync();

			await this.syncMonitor;

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Facilitates an inital sync on app start
		/// </summary>
		public Task InitialImportAsync() => this.etl.SyncAsync();
	}
}
