namespace Ox.SleepingGods.Data
{
	public delegate Task MessageRaised(string? message);

	/// <summary>
	/// Holds user feedback messages for common UI.
	/// </summary>
	public sealed class MessageManager
	{
		private const int K_SYNC = -1;
		private readonly Dictionary<int, string> keyedMessages = [];
		private int keyIndex = 0;
		private readonly List<string> msgs = [];

		public IReadOnlyDictionary<int,string> Messages => this.keyedMessages.AsReadOnly();

		/// <summary>
		/// Listen for new messages from <see cref="AddMessage(string)"/>. Don't let exceptions bubble.
		/// </summary>
		public MessageRaised? OnMesssagesChanged;

		/// <summary>
		/// Adds a message to <see cref="Messages"/> and also triggers any subscribers to <see cref="OnMesssagesChanged"/>
		/// </summary>
		/// <param name="message">Text to display</param>
		public async Task AddMessage(string message)
		{
			int key = Interlocked.Increment(ref this.keyIndex);
			this.keyedMessages[key] = message;
			await (this.OnMesssagesChanged?.Invoke(message) ?? Task.CompletedTask);
		}

		public Task ClearMessage(int key)
		{
			this.keyedMessages.Remove(key);
			return Task.CompletedTask;
		}

		public async Task DisplaySyncError(string message)
		{
			this.keyedMessages[K_SYNC] = message;
			await (this.OnMesssagesChanged?.Invoke(message) ?? Task.CompletedTask);
		}

		public async Task ClearSyncError()
		{
			this.keyedMessages.Remove(K_SYNC);
			await (this.OnMesssagesChanged?.Invoke(null) ?? Task.CompletedTask);
		}
	}
}
